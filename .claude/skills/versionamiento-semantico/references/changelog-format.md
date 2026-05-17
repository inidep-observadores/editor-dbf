# Formato de Changelog

Estándar de changelog para OBSArrastre2026, basado en "Keep a Changelog".

## Estructura general

```markdown
# Changelog

Todos los cambios notables en este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/)
y este proyecto adhiere a [Versionamiento Semántico](https://semver.org/).

## [Unreleased]

### Added
- Nueva característica X

### Changed
- Comportamiento modificado de Y

### Fixed
- Corrección de bug Z

## [1.2.0] - 2026-05-10

### Added
- Soporte para exportación a Excel
- Nuevo tipo de reporte: análisis de capturas

### Fixed
- Corrección en validación de especies con código duplicado
- Error al importar archivos DBF con caracteres especiales

## [1.1.0] - 2026-04-15

### Added
- Mejora en UI de importación de mareas

### Changed
- Optimización en consultas de base de datos

### Fixed
- Crash al generar reportes PDF sin datos

## [1.0.0] - 2026-03-01

### Added
- Versión inicial de OBSArrastre2026
- Gestión de mareas y lances
- Exportación DBF
- Generación de reportes PDF

[Unreleased]: https://github.com/usuario/OBSArrastre2026/compare/v1.2.0...develop
[1.2.0]: https://github.com/usuario/OBSArrastre2026/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/usuario/OBSArrastre2026/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/usuario/OBSArrastre2026/releases/tag/v1.0.0
```

## Secciones estándar

Usa estas secciones en orden (si aplican):

### Added (Agregado)
Nuevas características añadidas.

```markdown
### Added
- Soporte para exportación a Excel con formato personalizado
- Nueva API REST para consultas de captura
- Caché en memoria para especies frecuentes
```

### Changed (Modificado)
Cambios en funcionalidad existente (compatible hacia atrás).

```markdown
### Changed
- Optimización en queries de base de datos (50% más rápido)
- Interfaz de importación mejorada
- Estructura de directorios del proyecto reorganizada
```

### Deprecated (Deprecado)
Funcionalidad que será removida en futuras versiones.

```markdown
### Deprecated
- Método `OldImportService.Import()` — usar `JsonImportService` en su lugar
- API antigua `/api/v1/reports` — migrar a `/api/v2/reports`
```

### Removed (Removido)
Funcionalidad que ha sido removida.

```markdown
### Removed
- Método deprecado `OldValidationService`
- Soporte para archivos CSV (usar JSON en su lugar)
- Compatibilidad con .NET 8 (ahora requiere .NET 10)
```

### Fixed (Corregido)
Correcciones de bugs.

```markdown
### Fixed
- Crash al generar reportes PDF sin datos de capturas
- Error en validación de especies con caracteres especiales
- Inconsistencia en cálculo de totales en reportes Excel
```

### Security (Seguridad)
Correcciones de vulnerabilidades de seguridad.

```markdown
### Security
- Parchado CVE-2026-12345 en librería ClosedXML
- Refuerzo en validación de entrada de usuario
```

### Performance (Rendimiento)
Cambios de rendimiento o optimizaciones.

```markdown
### Performance
- Optimización de consultas en importación (60% más rápido)
- Reducción de uso de memoria en generación de reportes
```

## Breaking Changes (Cambios incompatibles)

Pueden aparecer en cualquier sección, pero deben ser **destacados**:

```markdown
## [2.0.0] - 2026-06-01

### ⚠️ BREAKING CHANGES
- Renombrado campo `id_producto` a `product_id` en BD
- Estructura de respuesta JSON de reportes ha cambiado
- Eliminado soporte para archivos DBF legacy

### Changed
- Nuevo sistema de autenticación OAuth2
- Reorganización de servicios de base de datos

### Added
- API REST v2 completamente refactorizada
```

## Ejemplo completo: Release notes

```markdown
## [1.3.0] - 2026-05-17

### Added
- Soporte para exportación a Excel con gráficos
- Nueva página de estadísticas en el dashboard
- Validación mejorada en importación de mareas

### Changed
- Optimización del motor de reportes PDF (2x más rápido)
- Refactorización de servicios de base de datos
- Mejora en mensajes de error de validación

### Deprecated
- Método `LegacyImportService.ImportFromCsv()` — usar `JsonImportService` en su lugar

### Fixed
- Corrección de crash al exportar reportes sin datos
- Error en cálculo de totales en reportes por especie
- Problema de caracteres especiales en nombres de buques

### Security
- Actualización de librerías para parchear CVE-2026-5544
```

## Generación automática

El script `semver.ps1` puede generar automáticamente secciones basadas en commits:

```powershell
# Generar changelog desde v1.2.0 hasta HEAD
.\scripts\semver.ps1 changelog --from v1.2.0 --output CHANGELOG.md

# Esto agrupa commits automáticamente por tipo:
# feat → Added
# fix → Fixed
# perf → Performance
# breaking changes → ⚠️ BREAKING CHANGES
```

## Proceso de actualización

Antes de hacer release:

1. Revisar commits desde última versión:
   ```powershell
   git log v1.2.0..develop --oneline
   ```

2. Generar borrador de changelog:
   ```powershell
   .\scripts\semver.ps1 changelog --draft
   ```

3. Editar manualmente para claridad y completitud

4. Agregar fecha de release

5. Añadir sección "Unreleased" para futuras versiones

6. Comitear:
   ```powershell
   git commit -m "docs(changelog): actualizar para v1.3.0"
   ```

## Referencias

- [Keep a Changelog](https://keepachangelog.com/)
- [Keep a Changelog - Español](https://keepachangelog.com/es-ES/)
- [Semantic Versioning](https://semver.org/)
