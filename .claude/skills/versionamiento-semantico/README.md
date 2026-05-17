# Skill: Versionamiento Semántico

Herramienta para gestionar versionamiento semántico (SemVer) en el proyecto OBSArrastre2026, integrada con Conventional Commits y Git Flow.

## Propósito

Automatizar y estandarizar el proceso de:
- **Determinar** el siguiente número de versión basado en commits
- **Crear** tags de versión en Git
- **Generar** changelogs automáticos
- **Validar** versiones semánticas
- **Actualizar** versiones en archivos del proyecto

## Cuándo usar

- Antes de hacer release: `semver next` → obtener versión siguiente
- Para generar changelog: `semver changelog`
- Para crear un tag de versión: `semver tag v1.2.3`
- Para validar formato SemVer: `semver validate v1.2.3`

## Conceptos

### Versionamiento Semántico (SemVer)

Formato: `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`

- **MAJOR**: Cambios incompatibles (breaking changes)
- **MINOR**: Nuevas características hacia atrás compatibles (features)
- **PATCH**: Correcciones de bugs

Ejemplo: `1.2.3`, `2.0.0-rc.1`, `1.0.0-beta+001`

### Mapeo de Conventional Commits

```
feat:  → MINOR bump (nueva característica)
fix:   → PATCH bump (corrección de bug)
feat!: → MAJOR bump (breaking change)
fix!:  → MAJOR bump (breaking change)
```

## Estructura

```
.codex/skills/versionamiento-semantico/
├── README.md                          (esta guía)
├── references/
│   ├── semver-spec.md                (especificación SemVer 2.0.0)
│   ├── conventional-commits.md       (mapeo a Conventional Commits)
│   └── changelog-format.md           (formato de changelog)
└── scripts/
    ├── semver.ps1                     (script PowerShell principal)
    └── semver-functions.ps1           (funciones auxiliares)
```

## Uso rápido

### Obtener siguiente versión

```powershell
.\scripts\semver.ps1 next
# Salida: v1.3.0
```

### Listar commits desde última versión

```powershell
.\scripts\semver.ps1 commits
# Salida: lista de commits sin mergear desde último tag
```

### Generar changelog

```powershell
.\scripts\semver.ps1 changelog --output CHANGELOG.md
# Crea/actualiza CHANGELOG.md
```

### Crear tag de versión

```powershell
.\scripts\semver.ps1 tag v1.3.0 --message "Release v1.3.0"
# Crea git tag v1.3.0
```

### Validar versión

```powershell
.\scripts\semver.ps1 validate v1.3.0
# Valida que cumple SemVer
```

## Integración con el proyecto

### Versión actual

La versión se lee desde:
1. Última `git tag` que comienza con `v` (ej: `v1.2.3`)
2. Si no existe tag, se asume `v0.0.0`

### Actualización en archivos

Se pueden actualizar automáticamente:
- `OBSArrastre2026.App.csproj` (`<Version>` y `<FileVersion>`)
- Archivos de configuración
- CHANGELOG.md

### Workflow Git Flow

En rama `develop`:
1. `semver next` → determinar próxima versión
2. `semver changelog` → generar cambios
3. Revisar, validar, mergear
4. `semver tag vX.Y.Z` → crear tag en main
5. Mergear tag a develop

## Variables de entorno

- `SEMVER_SKIP_PRERELEASE`: Si es `true`, omite versiones pre-release
- `SEMVER_INCLUDE_BREAKING_CHANGES`: Si es `true`, enfatiza breaking changes en changelog

## Validación de cambios

La skill automáticamente:
- ✅ Valida formato de commits
- ✅ Detecta breaking changes (`feat!`, `fix!`)
- ✅ Agrupa commits por tipo
- ✅ Valida versiones semánticas

## Ejemplo: Release workflow

```powershell
# En rama develop
cd OBSArrastre2026

# 1. Determinar versión siguiente
$nextVersion = .\scripts\semver.ps1 next
# → v1.3.0

# 2. Generar changelog
.\scripts\semver.ps1 changelog

# 3. Revisar cambios, actualizar si es necesario
git log --oneline v1.2.3..HEAD

# 4. Crear tag (si todo OK)
.\scripts\semver.ps1 tag v1.3.0 --message "Release: Mejoras en importación y reportes"

# 5. Mergear a main (Git Flow)
git checkout main
git pull origin main
git merge --no-ff develop
git tag v1.3.0
git push origin main
git push origin --tags

# 6. Volver a develop
git checkout develop
git merge main
```

## Referencias

- [Semantic Versioning 2.0.0](https://semver.org/)
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/)
- [Keep a Changelog](https://keepachangelog.com/)

## Relacionado

- `commits-convencionales`: Cómo escribir buenos commits
- CLAUDE.md `Workflow Git`
