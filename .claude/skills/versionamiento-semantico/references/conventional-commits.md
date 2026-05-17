# Mapeo: Conventional Commits → Versionamiento Semántico

Cómo los tipos de Conventional Commits se traducen a cambios de versión.

## Tipos de Conventional Commits

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Tipos reconocidos

| Tipo | Descripción | SemVer | Incluir en changelog |
|---|---|---|---|
| `feat` | Nueva característica | MINOR | ✅ Added |
| `fix` | Corrección de bug | PATCH | ✅ Fixed |
| `docs` | Documentación | - | ⚠️ Opcional |
| `style` | Formato (sin cambios de lógica) | - | ❌ |
| `refactor` | Refactorización (sin cambios de comportamiento) | - | ❌ |
| `perf` | Optimización de rendimiento | PATCH | ✅ Performance |
| `test` | Agregar o actualizar tests | - | ❌ |
| `build` | Cambios en build/dependencies | - | ✅ Build |
| `ci` | Cambios en CI/CD | - | ❌ |
| `chore` | Cambios rutinarios (actualizar deps, etc) | - | ❌ |
| `revert` | Revertir commit anterior | Según reversión | ✅ |

## Breaking Changes (Cambios incompatibles)

Se indican con `!` después del tipo/scope:

```
feat!: cambiar estructura de API
feat(api)!: cambiar endpoint /users
fix!: remover método deprecado
```

**Efecto**: Incrementa MAJOR version (no MINOR/PATCH).

### En el body

Incluir:

```
BREAKING CHANGE: descripción del cambio incompatible
```

Ejemplo:

```
feat(auth): implementar OAuth2

BREAKING CHANGE: El sistema de tokens JWT ha sido reemplazado.
Los clientes deben actualizar para usar OAuth2.
```

## Ejemplos en contexto del proyecto

### Ejemplo 1: Nueva característica

```
feat(reportes): agregar soporte para exportación a Excel

- Implementa ExcelReportService
- Añade nuevo tipo de reporte en ReportType enum
- Compatible con reportes PDF existentes
```

**Resultado**: `1.2.0` → `1.3.0` (MINOR bump)

**En changelog**: ✅ Added - Soporte para exportación a Excel

### Ejemplo 2: Corrección de bug

```
fix(importacion): corregir validación de especie en DBF

Soluciona el caso donde especies con código duplicado no eran detectadas.

Fixes #123
```

**Resultado**: `1.3.0` → `1.3.1` (PATCH bump)

**En changelog**: ✅ Fixed - Validación de especie en importación DBF

### Ejemplo 3: Breaking change

```
feat(database)!: cambiar estructura de tabla registros_produccion

BREAKING CHANGE: Se ha renombrado la columna `id_producto` a `product_id`.
Se requiere migración manual de base de datos.

Refs #456
```

**Resultado**: `1.3.1` → `2.0.0` (MAJOR bump)

**En changelog**: ❗ Breaking Changes - Estructura de registros_produccion

### Ejemplo 4: Sin impacto en versión

```
refactor(services): simplificar inyección de dependencias

- Eliminar parámetros redundantes
- Sin cambios en comportamiento público
```

**Resultado**: No hay bump de versión (cambio interno)

**En changelog**: ❌ No incluir

## Commits sin tipo

Si un commit **no tiene tipo** (ej: "fix typo"), se asume como:
- `fix` si menciona bug/corrección
- `chore` si es cambio rutinario
- Se puede rechazar en validación pre-commit si es estricto

En este proyecto, se **requiere tipo explícito** en todos los commits.

## Scope (opcional)

Especifica el área afectada:

```
feat(mareas): agregar filtro por fecha
fix(reportes-pdf): ajustar ancho de columna
refactor(database): cambiar nombres de tablas
```

**Scopes comunes en OBSArrastre2026**:
- `mareas`, `lances`, `etapas`, `productos`
- `importacion`, `exportacion`, `dbf`
- `reportes`, `reportes-pdf`, `reportes-excel`
- `validacion`, `database`, `auth`
- `ui`, `viewmodels`, `services`

## Validación de commits

Para validar commits antes de mergear:

```powershell
# Ver commits en rama actual no en main
git log main..HEAD --oneline

# Validar formato de commit
.\scripts\semver.ps1 validate-commits main..HEAD
```

## Referencia rápida

```powershell
# ✅ Buen commit
git commit -m "feat(importacion): agregar soporte para archivos DBF"

# ✅ Buen commit con breaking change
git commit -m "feat(api)!: cambiar estructura de respuesta

BREAKING CHANGE: El campo 'cantidad' es ahora 'cantidad_kg'"

# ❌ Mal commit (sin tipo)
git commit -m "fix validation bug"

# ❌ Mal commit (tipo inválido)
git commit -m "feature: agregar soporte para Excel"
```

## Referencias

- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/)
- [Conventional Commits español](https://www.conventionalcommits.org/es/)
