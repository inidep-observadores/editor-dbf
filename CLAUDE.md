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

No hay suite de tests automatizados en este proyecto.

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

## Control de versiones

- **Branch principal**: `master` — código estable, listo para producción
- **Feature branches**: `feature/*` — desarrollo de nuevas características
- **Convención de commits**: Mensajes descriptivos en español o inglés. Ejemplos:
  - `fix: corregir pérdida de separador decimal en filtros`
  - `feat: conversión automática de punto a coma en campos numéricos`

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
