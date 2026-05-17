# Changelog

Todos los cambios importantes de este proyecto están documentados en este archivo.

El formato sigue [Conventional Commits](https://www.conventionalcommits.org/es/v1.0.0/) y el versionamiento sigue [Semantic Versioning](https://semver.org/es/).

---

## [0.5.0] - 2026-05-16

### Added
- **test**: Suite de pruebas unitarias comprehensive que cubre:
  - ObservableObject: notificación de cambios de propiedades (5 tests)
  - ConnectionProfile: nombres efectivos/de visualización y notificaciones (8 tests)
  - TableTabViewModel: filtrado, gestión de documentos, operaciones de datos (13 tests)
  - DbfTableService: operaciones de archivo, validación de estructura, manejo de codepage (13 tests)
  - Cobertura de operadores de filtrado: LIKE, IS NULL, comparadores
  - Manejo de eventos de DataTable
  - Operaciones del sistema de archivos
- **test**: Configuración de descubrimiento automático de pruebas unitarias

### Changed
- **refactor**: Desacoplamiento de ViewModels respecto a diálogos WPF y servicios del sistema
  - Introducción de capa de abstracción con IDialogService, IClipboardService, IProcessService
  - Implementaciones concretas: WpfDialogService, WpfClipboardService, WpfProcessService
  - MainViewModel y TableTabViewModel ahora aceptan dependencias inyectadas en constructor
  - Visibilidad de métodos auxiliares cambiada a `internal` para permitir testing

### Fixed
- **fix**: Pérdida de separador decimal en filtrado de columnas numéricas
- **fix**: Mejora en visualización localizada en menús contextuales

### Documentation
- **docs**: Expansión de CLAUDE.md con secciones adicionales:
  - Instrucciones de debugging (IDE y consola)
  - Estructura de carpetas del proyecto detallada
  - Responsabilidades por capa
  - Pautas de control de versiones y convenciones de commit
  - Documentación ampliada de convenciones MVVM, persistencia DBF y tracking de cambios

---

## [0.4.0] - 2026-05-12

### Added
- **feat(filtrado)**: Filtrado avanzado SQL con soporte de:
  - Operadores de comparación: `=`, `<>`, `>`, `<`, `>=`, `<=`, `ENTRE`
  - Operadores de texto: `CONTIENE` (LIKE), `VACIO`, `NO VACIO`
  - Combinación de filtros múltiples con operador AND
  - Lenguaje natural y etiquetas dinámicas para mejor UX
  - Diálogo de valores personalizados
- **feat(edición-masiva)**: Cambio de valores en lote con confirmación
  - Menú contextual con opción 'Cambiar valor'
  - Soporte para edición de registros individuales o múltiples
  - Diálogo de confirmación antes de aplicar cambios
- **feat(gestión-conexiones)**: Mejoras en la administración de conexiones
  - Renombrado de conexiones con F2
  - Tooltips informativos
  - Verificación de rutas inexistentes
  - Atajos de teclado (Ctrl+E para explorar)
  - Validación dinámica de rutas al activar ventana
  - Soporte para multiselección en selector de carpetas
- **feat(exportación)**: Exportación avanzada de datos DBF
  - Renombrado de archivos
  - Diálogo de opciones de exportación
- **feat(interfaz)**: 
  - Mejora de navegación en grilla con feedback visual de celda activa
  - Alineación de columnas por tipo de dato
  - Resaltado de celdas modificadas en naranja
  - Cambio de partes de fecha (día, mes, año) en lote con validaciones
  - Conversión automática de punto a coma en campos numéricos (teclado numérico)

### Fixed
- **fix**: Correccion de formato de fecha `dd/MM/yyyy` en edición de grilla
- **fix**: Ajuste en comparaciones de fecha (por día completo)
- **fix**: InvalidCastException en menú contextual de lista de archivos
- **fix**: Validación de cambios pendientes al cerrar documento
- **fix**: Sincronización robusta de navegación en grilla

### Changed
- Refinamiento de temas light/dark
- Ajustes de espaciado y densidad en la interfaz

### Chore
- Eliminación de archivo temporal de compilación accidentalmente incluido

---

## [0.3.0] - 2026-05-10

### Added
- **feat(sql-console)**: Implementación de consola SQL con ejecución de selecciones
  - Motor SQLite integrado
  - Sintaxis SQL avanzada
  - Auto-enfoque después de inserción
  - Soporte para comentarios (`--`, `/* */`)
  - Selección múltiple de campos con checkboxes
  - Selección inteligente y doble clic para inserción
  - Sincronización de cursor robusta
  - Pulido visual de temas y espaciado
- **feat(navegación)**: Navegación contextual y explorador de esquemas SQL
  - Refactorización de MainWindow.xaml con DataTemplates dinámicos
  - Soporte para RelayCommand genérico
  - Inserción por doble clic en esquema

### Fixed
- **fix**: Configuración de ícono de aplicación
  - Regeneración de app_icon.ico con multi-resolución
- **fix**: Resolución de errores de inicio de la aplicación
- **fix**: Reparación de MainWindow.xaml

### Build
- **build**: Cambio de ruta de instalación a Program Files (64 bits)
- **build**: Validación de arquitectura x64

### Chore
- Merge de rama test con correcciones de inicio y recursos
- Actualización de nombre del ejecutable del instalador a `InstalarEditorDbf.exe`
- Organización del instalador en carpeta `setup`
- Automatización del proceso con script `.bat`
- Ignorar carpetas de publicación

---

## [0.2.0] - 2026-05-09

### Added
- **feat(ui)**: Implementación de CachedTabControl y refinamientos de interfaz
- **feat(exportación)**: Funcionalidad de exportación de datos con botón de refresco en panel de archivos

### Fixed
- **fix**: Reparación de tags desemparejados en XAML
- **fix**: Reposicionamiento de botones de filtro con soporte de Enter
- **fix**: Acceso a archivos en SaveTable
- **fix**: Implementación de verificación de instancia única
- **fix**: Compatibilidad de tema en Expander

### Style
- **style**: Correcciones y modernización de diseño
- Refinamiento de temas light/dark
- Tooltips faltantes añadidos

### Chore
- Ignorar carpetas de publicación y registrar script de instalador

---

## [0.1.0] - 2026-05-08

### Added
- **feat(core)**: Implementación inicial de editor WPF para archivos DBF
  - Interfaz de usuario básica con soporte de múltiples pestañas
  - Gestión de conexiones (perfiles de carpeta)
  - Explorador de archivos con visualización de estructura DBF
  - Visualización de datos con DataGrid
  
- **feat(localización)**: Localización completa al español
  - Mensajes y filtros en español
  - Interfaz de usuario en español

- **feat(temas)**: Sistema moderno de temas Light/Dark
  - Temas intercambiables en tiempo de ejecución
  - Diccionarios XAML: LightTheme.xaml, DarkTheme.xaml, ControlStyles.xaml
  - Templates customizados para Button, ListBoxItem, DataGrid, TabControl, TextBox, ScrollBar, GridSplitter
  - Toolbar modernizada con iconos Segoe MDL2 y grupos lógicos de acciones
  - Paneles redimensionables con GridSplitters
  - Estado vacío con placeholder cuando no hay pestañas abiertas
  - Indicador de filtro activo y punto de estado en barra de estado
  - Convertores: BoolToVisibilityConverter
  - Sistema de tracking de estado: IsDarkTheme y ToggleThemeCommand

- **feat(ui-optimization)**: Optimizaciones iniciales de interfaz
  - Barra de filtro con botones compactos (iconos)
  - Selector de código de página con botones icon-only (28x28px)
  - MinWidth ajustados para evitar contracciones excesivas
  - Padding optimizado para mayor densidad

### Documentation
- Documentación inicial del sistema de temas en CLAUDE.md

---

## [0.0.1] - 2026-05-08

### Added
- **Initial commit**: Commit inicial con estructura base del proyecto WPF DBF Editor
  - Solución .NET 10 con C# 13
  - Estructura MVVM básica
  - Dependencia DotNetDBF para lectura/escritura de archivos DBF
  - Configuración de proyecto y recursos iniciales

---

## Formato de versioning

Este proyecto utiliza [Semantic Versioning](https://semver.org/es/):

- **MAJOR** (x.0.0): cambios incompatibles (BREAKING CHANGE)
- **MINOR** (0.x.0): nuevas funcionalidades retrocompatibles (feat)
- **PATCH** (0.0.x): correcciones de errores retrocompatibles (fix)

Los números de versión se incrementan según el cambio más significativo en una versión.
