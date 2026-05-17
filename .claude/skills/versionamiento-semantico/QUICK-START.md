# Guía rápida: Versionamiento Semántico

## Instalación

Los scripts ya están en `.codex/skills/versionamiento-semantico/scripts/`.

### Agregar al PATH (opcional)

Para usarlos desde cualquier lugar:

```powershell
# En $PROFILE de PowerShell
$env:PATH += ";$(Split-Path -Parent $PSCommandPath)\.codex\skills\versionamiento-semantico\scripts"
```

## Uso rápido

### 1. Ver siguiente versión

```powershell
cd d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 next
```

**Resultado esperado:**
```
📦 Determinando siguiente versión...
Versión actual: v1.0.0
Commits nuevos: 5

Siguiente versión: v1.1.0

Resumen de cambios:
  ✨ Features: 3
  🐛 Fixes: 2
```

### 2. Ver commits desde última versión

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 commits
```

### 3. Generar changelog

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md
```

Esto crea o actualiza `CHANGELOG.md` con:
- Resumen de cambios desde última versión
- Agrupados por tipo (Added, Fixed, Breaking Changes)
- Fechas y links a commits

### 4. Ver borrador de changelog

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --draft
```

Muestra el changelog sin guardar a archivo.

### 5. Crear tag de versión

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag v1.1.0 --message "Release: Nuevas características de reportes"
```

### 6. Validar versión

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 validate v1.1.0
```

### 7. Validar commits

```powershell
# Validar commits en feature branch
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 validate-commits "develop..HEAD"
```

## Workflow típico: Release

```powershell
# 1. En rama develop
git checkout develop
git pull origin develop

# 2. Determinar próxima versión
$nextVersion = .\.codex\skills\versionamiento-semantico\scripts\semver.ps1 next
# → v1.1.0

# 3. Generar changelog
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md

# 4. Revisar changelog generado
notepad CHANGELOG.md

# 5. Comitear changelog
git add CHANGELOG.md
git commit -m "docs(changelog): actualizar para $nextVersion"

# 6. Crear tag (volver a main si usas Git Flow)
git checkout main
git pull origin main
git merge --no-ff develop
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag $nextVersion --message "Release: $nextVersion"

# 7. Empujar cambios
git push origin main
git push origin --tags
git checkout develop
git merge main
git push origin develop
```

## Cómo escribir commits para SemVer

### Nueva característica → Incrementa MINOR

```powershell
git commit -m "feat(reportes): agregar soporte para gráficos en PDF"
git commit -m "feat(importacion): permitir importación desde JSON"
```

### Corrección de bug → Incrementa PATCH

```powershell
git commit -m "fix(validacion): corregir validación de especies"
git commit -m "fix(reportes): error al generar PDF sin datos"
```

### Breaking change → Incrementa MAJOR

```powershell
git commit -m "feat(api)!: cambiar estructura de respuesta JSON"

# O con descripción adicional:
git commit -m "feat(database)!: renombrar tablas de base de datos

BREAKING CHANGE: Tabla 'registros_produccion' cambió estructura.
Ejecutar migración: scripts/v2.0.0-migration.sql"
```

### Sin impacto en versión (internal changes)

```powershell
git commit -m "refactor(services): simplificar inyección de dependencias"
git commit -m "test(validacion): agregar tests de validación"
git commit -m "docs: actualizar README"
```

## Troubleshooting

### Error: "No hay commits nuevos"

Significa que desde el último tag no hay commits. Esto es normal si ya hiciste release.

```powershell
git log --oneline v1.0.0..HEAD
```

Si hay commits nuevos pero el script no los ve, verifica que estés en la rama correcta.

### Error: "Versión inválida"

La versión no sigue SemVer. Usa formato: `vMAJOR.MINOR.PATCH`

✅ Correcto:
- `v1.0.0`
- `v1.2.3-rc.1`
- `v2.0.0-beta+build.123`

❌ Incorrecto:
- `1.0.0` (falta la `v`)
- `v1.2` (falta PATCH)
- `v1.2.3.4` (demasiados números)

### Error: "Commit inválido"

Tus commits no siguen Conventional Commits. Revisa el formato:

```
✅ feat(scope): descripción
✅ fix: descripción
❌ Feature X
❌ fixed a bug
❌ test fix
```

## Más información

- 📖 [README de la skill](./README.md)
- 📋 [Especificación SemVer](./references/semver-spec.md)
- ✍️ [Conventional Commits](./references/conventional-commits.md)
- 📝 [Formato de Changelog](./references/changelog-format.md)

## Integración con Claude Code

Puedes pedirle a Claude Code que te ayude con versionamiento:

```
/skills semver
```

O usar directamente los comandos:

```powershell
claude-code ./.codex/skills/versionamiento-semantico/scripts/semver.ps1 next
```

## Soporte

Si los scripts no funcionan:

1. Verifica que estés en directorio con `.git/`
2. Verifica que Git está instalado: `git --version`
3. Verifica que tienes commits: `git log --oneline -1`
4. Lee la documentación en `references/`
