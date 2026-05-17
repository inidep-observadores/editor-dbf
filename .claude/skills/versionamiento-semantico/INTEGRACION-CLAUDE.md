# Integración de skill en CLAUDE.md

Esta skill de versionamiento semántico puede ser referenciada en el proyecto CLAUDE.md. A continuación se muestran opciones de integración.

## Opción 1: Adicionar a tabla de skills locales (Recomendado)

En el archivo `CLAUDE.md`, actualizar la sección de skills locales:

```markdown
## Skills locales (`.codex/skills/`)

| Skill | Cuándo usarlo |
|---|---|
| `obs-arrastre-wpf` | Arquitectura, naming, mapeo de dominio, tareas de implementación en este repo |
| `commits-convencionales` | Redactar mensajes de commit, merge, squash o revert |
| `principios-arquitectura` | Evaluar o aplicar SOLID, Clean Architecture, Repository Pattern |
| `versionamiento-semantico` | Determinar versiones, generar changelogs, crear tags de release |
```

## Opción 2: Crear sección dedicada a versionamiento

Dentro de CLAUDE.md, agregar:

```markdown
## Versionamiento

El proyecto sigue [Versionamiento Semántico 2.0.0](https://semver.org/) basado en [Conventional Commits 1.0.0](https://www.conventionalcommits.org/).

### Versión actual

Ver versión actual: `git describe --tags --match "v*"`

### Workflow de versiones

Antes de hacer release:

```powershell
# En rama develop
cd OBSArrastre2026

# 1. Determinar versión siguiente
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 next

# 2. Generar changelog automático
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md

# 3. Crear tag en Git
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag vX.Y.Z --message "Release vX.Y.Z"

# 4. Mergear a main siguiendo Git Flow
```

Para más detalles: [Ver skill de versionamiento](./.codex/skills/versionamiento-semantico/QUICK-START.md)
```

## Opción 3: Actualizar convenciones de commits

Mejorar la sección de Convenciones de código:

```markdown
## Convenciones de código

- Documentación del proyecto en **español**.
- Mensajes de Git en **español** con formato Conventional Commits: `tipo(ámbito): descripción`.
- Tipos válidos: `feat`, `fix`, `docs`, `refactor`, `style`, `test`, `build`, `ci`, `chore`, `perf`, `revert`.
  - `feat`: Nueva característica (incrementa MINOR en versionamiento)
  - `fix`: Corrección de bug (incrementa PATCH)
  - `feat!` o `fix!`: Breaking change (incrementa MAJOR)
  - Otros tipos: sin impacto en versión
- Archivos en **UTF-8** (crítico para XAML y markdown con tildes).
- Nullable reference types e implicit usings habilitados; no desactivarlos sin razón clara.
- Nombres de entidades C# alineados con los nombres de tabla del esquema SQL; documentar cualquier divergencia en código.

### Versionamiento Semántico

El proyecto usa [Semantic Versioning 2.0.0](https://semver.org/) automáticamente derivado de los Conventional Commits. 

Ver skill de versionamiento: `.codex/skills/versionamiento-semantico/`
```

## Opción 4: Crear CHANGELOG.md inicial (opcional)

Para proyectos sin CHANGELOG.md aún:

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md
```

Esto genera un changelog con todos los commits históricos agrupados por versión.

## Integración en Pipeline de CI/CD

Si el proyecto tiene GitHub Actions u otro CI:

```yaml
# Ejemplo: .github/workflows/release.yml
name: Release

on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to release (e.g., v1.2.3)'
        required: true

jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Validate version
        run: |
          .\.codex\skills\versionamiento-semantico\scripts\semver.ps1 validate ${{ github.event.inputs.version }}
      
      - name: Update CHANGELOG
        run: |
          .\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md
      
      - name: Commit and tag
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add CHANGELOG.md
          git commit -m "docs(changelog): actualizar para ${{ github.event.inputs.version }}"
          .\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag ${{ github.event.inputs.version }}
          git push origin main
          git push origin --tags
```

## Alias de PowerShell (Recomendado)

Para usar los comandos más fácilmente, agregar a `$PROFILE`:

```powershell
# Alias para skill de versionamiento
New-Alias -Name semver -Value "$PSScriptRoot\..\..\.codex\skills\versionamiento-semantico\scripts\semver.ps1" -Force

# Luego usar como:
# semver next
# semver changelog --output CHANGELOG.md
# semver tag v1.2.3
```

## Validación de commits pre-commit (Opcional)

Crear hook pre-commit para validar Conventional Commits:

```powershell
# .git/hooks/pre-commit (PowerShell)
$commitMsg = git diff --cached --name-status | Get-Content -Head 1
if (-not ($commitMsg -match "^[a-z]+(\([^)]*\))?!?:\s*.+")) {
    Write-Host "❌ Commit inválido. Usa Conventional Commits: tipo(scope): descripción" -ForegroundColor Red
    exit 1
}
exit 0
```

## Checklist de integración

- [ ] Actualizar CLAUDE.md con referencia a skill
- [ ] Crear CHANGELOG.md inicial si no existe
- [ ] Crear alias en PowerShell profile (opcional pero recomendado)
- [ ] Documentar en README.md cómo hacer releases
- [ ] Configurar hooks pre-commit (opcional)
- [ ] Integrar en CI/CD si existe (opcional)

## Referencias

- [Skill de versionamiento](./README.md)
- [Guía rápida](./QUICK-START.md)
- [Índice completo](./INDEX.md)
- [Especificación SemVer](./references/semver-spec.md)
- [Conventional Commits](./references/conventional-commits.md)
