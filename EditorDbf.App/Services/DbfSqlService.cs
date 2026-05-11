using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetDBF;
using Microsoft.Data.Sqlite;

namespace EditorDbf.App.Services;

public sealed class SqlExecutionResult
{
    public DataTable? Results { get; init; }
    public int RowsAffected { get; init; }
    public string? Message { get; init; }
    public bool IsSuccess { get; init; }
    public IReadOnlyList<string> ModifiedTables { get; init; } = [];
}

public sealed class DbfSqlService
{
    private readonly DbfTableService _dbfTableService;

    public DbfSqlService(DbfTableService dbfTableService)
    {
        _dbfTableService = dbfTableService;
    }

    public async Task<SqlExecutionResult> ExecuteAsync(string folderPath, string sql)
    {
        try
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var dbfFiles = _dbfTableService.ListDbfFiles(folderPath);
            var tableMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Cargar todas las tablas en SQLite
            foreach (var fileName in dbfFiles)
            {
                var tableName = Path.GetFileNameWithoutExtension(fileName);
                var filePath = Path.Combine(folderPath, fileName);
                
                // Cargar documento DBF
                var doc = _dbfTableService.LoadTable(filePath);
                tableMapping[tableName] = filePath;

                // Crear tabla en SQLite
                await CreateSqliteTableAsync(connection, tableName, doc.Fields);

                // Insertar datos
                await InsertDataAsync(connection, tableName, doc.DataTable, doc.Fields);
            }

            // 2. Ejecutar el SQL del usuario
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var cleanSql = StripComments(sql).Trim();
            var isQuery = cleanSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                          cleanSql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase) ||
                          cleanSql.StartsWith("VALUES", StringComparison.OrdinalIgnoreCase);

            if (isQuery)
            {
                using var reader = await command.ExecuteReaderAsync();
                var dt = new DataTable();
                dt.Load(reader);

                return new SqlExecutionResult
                {
                    Results = dt,
                    IsSuccess = true,
                    Message = $"{dt.Rows.Count} filas encontradas."
                };
            }
            else
            {
                var affected = await command.ExecuteNonQueryAsync();
                
                // 3. Si fue una modificación, sincronizar cambios de vuelta a los DBF
                var modifiedTables = new List<string>();
                foreach (var kvp in tableMapping)
                {
                    if (await WasTableModifiedAsync(connection, kvp.Key))
                    {
                        await SyncBackToDbfAsync(connection, kvp.Key, kvp.Value);
                        modifiedTables.Add(kvp.Key);
                    }
                }

                return new SqlExecutionResult
                {
                    RowsAffected = affected,
                    IsSuccess = true,
                    Message = $"{affected} filas afectadas.",
                    ModifiedTables = modifiedTables
                };
            }
        }
        catch (Exception ex)
        {
            return new SqlExecutionResult
            {
                IsSuccess = false,
                Message = $"Error SQL: {ex.Message}"
            };
        }
    }

    private async Task CreateSqliteTableAsync(SqliteConnection conn, string tableName, DBFField[] fields)
    {
        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE [{tableName}] (");
        
        var columnDefs = fields.Select(f => $"[{f.Name}] {MapToSqliteType(f.DataType)}");
        sb.Append(string.Join(", ", columnDefs));
        
        sb.Append(")");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sb.ToString();
        await cmd.ExecuteNonQueryAsync();

        // Crear una tabla espejo para detectar cambios si fuera necesario, 
        // pero por ahora simplemente asumiremos que si no es SELECT, todo pudo cambiar 
        // o leeremos los datos de vuelta.
    }

    private async Task InsertDataAsync(SqliteConnection conn, string tableName, DataTable data, DBFField[] fields)
    {
        if (data.Rows.Count == 0) return;

        var columnNames = string.Join(", ", fields.Select(f => $"[{f.Name}]"));
        var paramNames = string.Join(", ", fields.Select(f => $"@{f.Name}"));
        var insertSql = $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({paramNames})";

        using var transaction = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = insertSql;
        cmd.Transaction = transaction;

        foreach (var field in fields)
        {
            cmd.Parameters.Add(new SqliteParameter($"@{field.Name}", null));
        }

        foreach (DataRow row in data.Rows)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                var val = row[i];
                if (val == DBNull.Value)
                {
                    cmd.Parameters[i].Value = DBNull.Value;
                }
                else if (fields[i].DataType == NativeDbType.Date || fields[i].DataType == NativeDbType.Timestamp)
                {
                    cmd.Parameters[i].Value = ((DateTime)val).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else if (fields[i].DataType == NativeDbType.Logical)
                {
                    cmd.Parameters[i].Value = (bool)val ? 1 : 0;
                }
                else
                {
                    cmd.Parameters[i].Value = val;
                }
            }
            await cmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<bool> WasTableModifiedAsync(SqliteConnection conn, string tableName)
    {
        // SQLite tiene una tabla interna llamada sqlite_sequence o podemos usar un checksum.
        // Pero lo más sencillo y preciso para DBF es comparar si el contenido cambió.
        // Para esta versión, utilizaremos una técnica más eficiente: comprobar si hubo escrituras
        // en la base de datos que afectaron a esta tabla.
        
        // Una opción es consultar los cambios totales en la conexión:
        // Pero como cargamos todo desde cero en cada ejecución de consola, 
        // simplemente verificaremos si la query original contiene el nombre de la tabla 
        // y NO es un SELECT. El método ExecuteAsync ya separa SELECTs.
        
        // Si llegamos aquí, es un UPDATE/INSERT/DELETE/CREATE.
        // Verificamos si la tabla existe y si el comando SQL parece afectarla.
        // Por ahora, para garantizar integridad, sincronizaremos las tablas que fueron cargadas
        // si el comando no fue un SELECT.
        return true; 
    }

    private string StripComments(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return string.Empty;

        // Eliminar comentarios multilínea /* ... */
        var noMultiLine = System.Text.RegularExpressions.Regex.Replace(sql, @"/\*.*?\*/", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        // Eliminar comentarios de una sola línea -- o #
        var lines = noMultiLine.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cleanLines = lines.Select(line => 
        {
            int dashIndex = line.IndexOf("--");
            if (dashIndex >= 0) return line.Substring(0, dashIndex);
            
            int hashIndex = line.IndexOf("#");
            if (hashIndex >= 0) return line.Substring(0, hashIndex);
            
            return line;
        });

        return string.Join(Environment.NewLine, cleanLines);
    }

    private async Task SyncBackToDbfAsync(SqliteConnection conn, string tableName, string dbfFilePath)
    {
        // 1. Leer datos de SQLite
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM [{tableName}]";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var dt = new DataTable();
        dt.Load(reader);

        // 2. Cargar documento original para obtener estructura y encoding
        var doc = _dbfTableService.LoadTable(dbfFilePath);

        // 3. Actualizar el DataTable del documento con los nuevos datos
        // Necesitamos mapear los tipos de vuelta
        var targetDt = doc.DataTable;
        targetDt.Rows.Clear();

        foreach (DataRow sourceRow in dt.Rows)
        {
            var newRow = targetDt.NewRow();
            for (int i = 0; i < doc.Fields.Length; i++)
            {
                var val = sourceRow[i];
                newRow[i] = MapFromSqliteValue(val, doc.Fields[i].DataType);
            }
            targetDt.Rows.Add(newRow);
        }

        // 4. Guardar
        _dbfTableService.SaveTable(doc);
    }

    private string MapToSqliteType(NativeDbType type)
    {
        return type switch
        {
            NativeDbType.Logical => "INTEGER",
            NativeDbType.Numeric or NativeDbType.Long or NativeDbType.Autoincrement => "NUMERIC",
            NativeDbType.Float or NativeDbType.Double => "REAL",
            NativeDbType.Date or NativeDbType.Timestamp => "TEXT",
            NativeDbType.Binary => "BLOB",
            _ => "TEXT"
        };
    }

    private object MapFromSqliteValue(object val, NativeDbType targetType)
    {
        if (val == DBNull.Value) return DBNull.Value;

        return targetType switch
        {
            NativeDbType.Logical => Convert.ToInt32(val) != 0,
            NativeDbType.Date or NativeDbType.Timestamp => DateTime.Parse(Convert.ToString(val)!),
            NativeDbType.Numeric => Convert.ToDecimal(val),
            NativeDbType.Float or NativeDbType.Double => Convert.ToDouble(val),
            NativeDbType.Long or NativeDbType.Autoincrement => Convert.ToInt32(val),
            _ => Convert.ToString(val) ?? string.Empty
        };
    }

    public async Task<IEnumerable<DBFField>> GetTableSchemaAsync(string dbfFilePath)
    {
        return await Task.Run(() => 
        {
            // Usamos el servicio de tablas para cargar el documento y extraer sus campos
            var doc = _dbfTableService.LoadTable(dbfFilePath);
            return doc.Fields.AsEnumerable();
        });
    }
}
