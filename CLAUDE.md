# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Comandos esenciales

```powershell
# Restaurar dependencias NuGet
dotnet restore

# Compilar (Debug, por defecto)
dotnet build

# Ejecutar
dotnet run --project EditorDbf.App

# Compilar en Release
dotnet build -c Release

# Limpiar artefactos de build
dotnet clean

# Publicar
dotnet publish -c Release
```

**NOTA**: Hay suite de tests unitarios con 42 tests que cubren core functionality. Ver `EditorDbf.Tests/` y ejecutar con `dotnet test`.

### Debugging

- **Visual Studio / IDE**: Abre el proyecto `.sln` y presiona F5 para ejecutar con debugger attached. Los breakpoints funcionan normalmente.
- **Consola**: Usa `dotnet run` desde PowerShell; los logs de la aplicación aparecerán en la salida estándar.

## Stack tecnológico

- **Framework**: .NET 10 (`net10.0-windows`), C# 13
- **UI**: WPF (Windows Presentation Foundation) — estilos propios, sin librerías UI externas
- **Dependencia clave**: `DotNetDBF v7.0.1` — lectura/escritura de archivos DBF

## Estructura del proyecto

```
EditorDbf/
├── EditorDbf.App/
│   ├── Infrastructure/           # Base MVVM (ObservableObject, RelayCommand)
│   ├── Models/                   # Modelos de datos (ConnectionProfile, DbfTableDocument, etc.)
│   ├── Services/                 # Lógica de I/O (DbfTableService, ConnectionRepository)
│   ├── ViewModels/               # Lógica de presentación (MainViewModel, TableTabViewModel)
│   ├── Views/                    # UI declarativa (MainWindow.xaml, App.xaml)
│   ├── Themes/                   # Temas light/dark (LightTheme.xaml, DarkTheme.xaml, ControlStyles.xaml)
│   └── App.xaml.cs               # Punto de entrada, composición de servicios
├── EditorDbf.sln                 # Solución .NET
└── *.csproj                      # Proyectos
```

## Arquitectura

La aplicación sigue el patrón **MVVM** estricto.

### Flujo principal

```
App.xaml.cs
  └─ crea ConnectionRepository + DbfTableService
  └─ instancia MainViewModel → se enlaza a MainWindow
```

**MainWindow** tiene tres paneles:
1. Lista de conexiones (perfiles de carpeta)
2. Lista de archivos `.dbf` de la conexión activa + estructura de campos
3. Workspace con tabs, una por tabla abierta

### Capas

| Capa | Archivos | Responsabilidad |
|------|----------|-----------------|
| Infrastructure | `ObservableObject`, `RelayCommand` | Base MVVM: `INotifyPropertyChanged`, `ICommand` |
| Models | `ConnectionProfile`, `AppState`, `DbfTableDocument`, `DbfFieldDescriptor` | Datos en memoria y configuración |
| Services | `DbfTableService`, `ConnectionRepository` | I/O de archivos DBF y persistencia de perfiles |
| ViewModels | `MainViewModel`, `TableTabViewModel` | Lógica de presentación, comandos, estado de UI |
| Views | `MainWindow.xaml`, `App.xaml` | UI declarativa en XAML |

### Operaciones sobre tablas (`DbfTableService`)

- **Carga**: Lee DBF con DotNetDBF, mapea tipos DBF → CLR (`Logical→bool`, `Date→DateTime`, `Numeric→decimal`), detecta code page ANSI (fallback CP1252).
- **Guardado**: Escribe a archivo temporal y luego hace un move atómico para preservar archivos `.fpt` de memo.
- **Import (APPEND FROM)**: Valida compatibilidad de estructura antes de agregar filas.

### Estado por tabla (`TableTabViewModel`)

Cada tab tiene su propio `TableTabViewModel` que gestiona:
- Filtrado con `DataView.RowFilter` y operadores `=`, `<>`, `>`, `<`, `CONTIENE` (LIKE), `VACIO`, `NO VACIO`
- Flag de cambios pendientes (suscripto a `RowChanged`, `RowDeleted`, `TableNewRow`)
- Selección múltiple de filas (modo Extended)

### Persistencia

Los perfiles de conexión se guardan en:
```
%LOCALAPPDATA%\EditorDbf\connections.json
```

## Control de versiones y Commits

Este proyecto usa **Conventional Commits** y **Semantic Versioning** para automatizar versionamiento y changelog.

### Rama y flujo de trabajo

- **Branch principal**: `master` — código estable, listo para producción
- **Feature branches**: `feature/*` — desarrollo de nuevas características
- **Versionamiento**: [Semantic Versioning 2.0.0](https://semver.org/es/) (MAJOR.MINOR.PATCH)

### Formato de commits (Conventional Commits)

Todos los commits deben seguir el formato:

```
tipo(ámbito): descripción

[cuerpo opcional]

[footer opcional]
```

**Tipos permitidos**:
- `feat`: Nueva funcionalidad → incrementa MINOR
- `fix`: Corrección de bug → incrementa PATCH
- `refactor`: Reorganización sin cambio observable
- `perf`: Mejora de rendimiento
- `style`: Formato, sin cambio funcional
- `docs`: Cambios de documentación
- `test`: Adición o ajuste de pruebas
- `build`: Cambios en build, dependencias, SDK
- `ci`: Cambios en CI/CD
- `chore`: Mantenimiento (no code)
- `revert`: Revierte un commit anterior

**Ejemplos válidos**:
```
feat(filtrado): agregar operador ENTRE en filtrado SQL
fix(edición): corregir pérdida de separador decimal en campos numéricos
refactor(viewmodels): desacoplar de diálogos WPF
docs: expandir CLAUDE.md con debugging y estructura
test(filtering): agregar tests para operadores LIKE e IS NULL
build(installer): cambiar ruta a Program Files (64-bit)
```

**BREAKING CHANGE**:
Si un cambio rompe compatibilidad, usa `!` o agrega `BREAKING CHANGE:` en el pie:
```
feat(api)!: cambiar estructura de respuesta JSON

BREAKING CHANGE: el campo 'data' ahora es 'results'
```

### Archivo de referencia

El skill **commits-convencionales** está disponible en `.claude/skills/commits-convencionales/` para consulta rápida y redacción de mensajes.

### Changelog y versiones

- **Changelog**: Ver `docs/CHANGELOG.md` con historial completo desde v0.0.1
- **Versión actual**: v0.5.0 (definida en `EditorDbf.App/EditorDbf.App.csproj`)
- **Mapeo de commits**: Ver `docs/CHANGELOG-MAPPING.md` para entender cómo se clasifican commits en versiones

Los commits se agrupan automáticamente en versiones basadas en su tipo:
- Cambios `feat` = nueva versión MINOR (0.x.0)
- Cambios `fix`, `style`, `perf`, etc. = nueva versión PATCH (0.0.x)
- `BREAKING CHANGE` = nueva versión MAJOR (x.0.0)

## Temas (Light / Dark)

El sistema de temas usa tres archivos en `EditorDbf.App/Themes/`:
- `LightTheme.xaml` / `DarkTheme.xaml` — diccionarios de colores (brushes con claves como `AppBackgroundBrush`, `AccentBrush`, etc.)
- `ControlStyles.xaml` — templates de controles que referencian esas claves via `DynamicResource`

El intercambio en runtime reemplaza `Application.Current.Resources.MergedDictionaries[0]` en `MainWindow.xaml.cs::ApplyTheme()`. El estado se trackea en `MainViewModel.IsDarkTheme`. Para agregar colores nuevos, definirlos en **ambos** archivos de tema con la misma clave.

## Convenciones importantes

### UI e Internacionalización
- Toda la UI está en **español** (labels, mensajes, diálogos de error).
- El formato de fecha está hardcodeado como `dd/MM/yyyy` en `MainWindow.xaml.cs`.
- Los colores nuevos se definen en **ambos** archivos de tema (`LightTheme.xaml` y `DarkTheme.xaml`) con la misma clave para mantener coherencia.

### Binding y MVVM
- Las propiedades del ViewModel deben heredar de `ObservableObject` e invocar `OnPropertyChanged()` para notificar cambios.
- Los comandos usan `RelayCommand` y exponen `CanExecute` con predicados; deben llamar a `RaiseCanExecuteChanged()` cuando cambien las condiciones.
- El binding en XAML usa `{Binding NombrePropiedad, Mode=TwoWay}` (por defecto OneWay en controles read-only).

### DBF y Persistencia
- El byte de firma DBF original se preserva al guardar (por defecto DBase3 si falta).
- El `DbfTableService` maneja automáticamente la conversión de tipos: `Logical→bool`, `Date→DateTime`, `Numeric→decimal`.
- El code page se detecta al cargar (fallback CP1252 si no es detectado).
- Las operaciones de guardado usan un archivo temporal y luego un move atómico para preservar archivos `.fpt` (memo fields).

### Tracking de cambios
- Los cambios en tablas se detectan automáticamente vía `DataTable` events: `RowChanged`, `RowDeleted`, `TableNewRow`.
- El ViewModel expone un flag `HasPendingChanges` que se sincroniza con estos eventos.
- La selección múltiple de filas usa el modo `Extended` del DataGrid.

## Skills disponibles

Se han definido dos skills locales para mantener coherencia arquitectónica y de versionamiento:

### 1. `/commits-convencionales`
Guía para redactar mensajes de commit usando Conventional Commits en español. Usar cuando:
- Propongas un mensaje de commit
- Revises commits antes de pusharlos
- Squashes o merges requieran redacción de mensaje
- Necesites ejemplos de formato válido

Ubicación: `.claude/skills/commits-convencionales/`

**Ejemplo de uso**:
```
/commits-convencionales ¿Es este un mensaje válido? "refactor: mejora de performance"
```

### 2. `/principios-arquitectura`
Guía para aplicar SOLID, Clean Code, Clean Architecture, separación por capas y Repository Pattern. Usar cuando:
- Diseñes nueva arquitectura
- Refactorices código existente
- Definas límites entre capas (UI/Dominio/Datos)
- Evalúes si una abstracción realmente mejora el código

Ubicación: `.claude/skills/principios-arquitectura/`

**Ejemplo de uso**:
```
/principios-arquitectura ¿Esta clase viola SRP? [codigo...]
```

## Flujo recomendado para desarrollo

1. **Crear rama feature**: `git checkout -b feature/descripcion-breve`
2. **Implementar cambios**: Aplicar principios de arquitectura según corresponda
3. **Redactar commits**: Usar `/commits-convencionales` si hay dudas
4. **Push**: `git push origin feature/descripcion-breve`
5. **PR/Review**: Descripción debe referenciar cambios convencionales
6. **Merge a master**: Los commits quedan documentados en CHANGELOG automáticamente

## Testing

Antes de hacer PR, asegurate que:
- Los tests existentes pasen: `dotnet test`
- Nuevas features tengan tests correspondientes
- Coverage no disminuya significativamente

Ver `EditorDbf.Tests/` para estructura de tests.
