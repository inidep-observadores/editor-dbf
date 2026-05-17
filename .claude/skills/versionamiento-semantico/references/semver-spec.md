# Semantic Versioning 2.0.0

Resumen de la especificación oficial de Versionamiento Semántico.

## Resumen

Una versión DEBE tener el formato `MAJOR.MINOR.PATCH` donde cada uno es un entero no-negativo, y NO DEBEN incluir ceros iniciales.

Dado un número de versión MAJOR.MINOR.PATCH:

1. **MAJOR** se incrementa cuando haces cambios incompatibles (breaking change)
2. **MINOR** se incrementa cuando añades funcionalidad compatible hacia atrás
3. **PATCH** se incrementa cuando haces correcciones de bugs compatibles hacia atrás

Ejemplos: `1.0.0`, `2.1.0`, `1.0.0-alpha`, `1.0.0-alpha.1`, `1.0.0-0.3.7`

## Especificación detallada

### 1. Cuando estés en versión 0.y.z

Todo puede cambiar en cualquier momento. No consideres la API estable.

- Ejemplo: `0.1.0` → `0.2.0` es un cambio MINOR (compatible)
- Pero conceptualmente sigue siendo inestable

### 2. Versión 1.0.0 define la API pública

A partir de aquí, el versionamiento es según SemVer estricto.

### 3. PATCH para arreglos de bugs

Una versión PATCH DEBE ser incrementada si se introducen uno o más arreglos de bugs compatibles hacia atrás. Ejemplo: `1.0.1`, `1.0.2`.

### 4. MINOR para nuevas características

Una versión MINOR DEBE ser incrementada si se introduce nueva funcionalidad pública compatible hacia atrás. PUEDE ser incrementada si se depreca funcionalidad en el código privado.

Ejemplo: `1.1.0`, `1.2.0`.

### 5. MAJOR para cambios incompatibles

Una versión MAJOR DEBE ser incrementada si cualquier cambio incompatible hacia atrás se introduce en la API pública. PUEDE incluir cambios a MINOR y PATCH.

Ejemplo: `2.0.0`, `3.0.0`.

### 6. Versiones pre-release

Pueden ser denotadas añadiendo un guión seguido de una serie de identificadores separados por puntos: `1.0.0-alpha`, `1.0.0-alpha.1`, `1.0.0-0.3.7`.

```
1.0.0-alpha       < 1.0.0-alpha.1    < 1.0.0-beta      < 1.0.0
1.0.0-rc.1        < 1.0.0
```

### 7. Metadata de build

Pueden ser denotadas añadiendo un signo más seguido de identificadores: `1.0.0+20130313144700`, `1.0.0-beta+exp.sha.5114f85`.

La metadata de build DEBE ser ignorada al determinar precedencia de versiones.

```
1.0.0+build.1 == 1.0.0+build.2  (en términos de precedencia)
```

## Precedencia

Ejemplo de orden creciente:

```
1.0.0-alpha
1.0.0-alpha.1
1.0.0-alpha.beta
1.0.0-beta
1.0.0-beta.2
1.0.0-beta.11
1.0.0-rc.1
1.0.0
1.1.0
```

## Changelog template

```markdown
## [1.0.0] - 2026-05-17

### Added
- Nueva característica X
- Nueva característica Y

### Changed
- Comportamiento modificado de Z

### Deprecated
- Método antiguo (usar nuevoMétodo en su lugar)

### Removed
- Método obsoleto X

### Fixed
- Corrección de bug en componente Y

### Security
- Parchado CVE-XXXX

### Breaking Changes
- La clase A ha sido removida; usar B en su lugar
```

## Convención en este proyecto

En OBSArrastre2026 se combinan:

1. **Conventional Commits** para normalizar mensajes
2. **SemVer** para versionamiento
3. **Git tags** con formato `v1.2.3`

Mapeo:

| Tipo Commit | MAJOR | MINOR | PATCH |
|---|---|---|---|
| `feat` | - | ✓ | - |
| `fix` | - | - | ✓ |
| `feat!` | ✓ | - | - |
| `fix!` | ✓ | - | - |
| `docs` | - | - | - |
| `refactor` | - | - | - |
| `test` | - | - | - |
| `perf` | - | - | - |

**Nota**: `docs`, `refactor`, `test`, `perf` no afectan versionamiento (sin cambios de API pública).

## Referencias

- https://semver.org/
- https://semver.org/lang/es/ (versión en español)
