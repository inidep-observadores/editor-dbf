# Índice: Skill Versionamiento Semántico

Documentación completa de la skill para gestión de versionamiento semántico en OBSArrastre2026.

## 📚 Estructura de archivos

```
.codex/skills/versionamiento-semantico/
│
├── README.md                          ← Documentación principal
├── QUICK-START.md                     ← Guía de uso rápido
├── INDEX.md                           ← Este archivo
│
├── references/                        ← Referencias y especificaciones
│   ├── semver-spec.md                ← Especificación SemVer 2.0.0
│   ├── conventional-commits.md       ← Mapeo de Conventional Commits
│   └── changelog-format.md           ← Formato estándar de changelog
│
└── scripts/                           ← Implementación
    ├── semver.ps1                    ← Script principal
    └── semver-functions.ps1          ← Funciones auxiliares
```

## 🎯 Propósito

Automatizar gestión de versiones semánticas en el proyecto OBSArrastre2026 basada en:
- **Semantic Versioning 2.0.0** (SemVer): versionamiento MAJOR.MINOR.PATCH
- **Conventional Commits**: commits estructurados para determinar tipo de cambio
- **Git Flow**: integración con ramas develop/main y Git tags

## 📖 Documentación

### Para empezar rápido
👉 [QUICK-START.md](./QUICK-START.md) - Guía de uso inmediato

### Para entender el framework
- 📋 [README.md](./README.md) - Propósito y conceptos
- 📐 [references/semver-spec.md](./references/semver-spec.md) - Especificación SemVer oficial
- ✍️ [references/conventional-commits.md](./references/conventional-commits.md) - Formato de commits
- 📝 [references/changelog-format.md](./references/changelog-format.md) - Formato de changelog

### Para desarrolladores
- 🔧 [scripts/semver.ps1](./scripts/semver.ps1) - Script principal con comandos
- 🛠️ [scripts/semver-functions.ps1](./scripts/semver-functions.ps1) - Funciones reutilizables

## 🚀 Comandos disponibles

| Comando | Uso | Ejemplo |
|---|---|---|
| `next` | Determinar siguiente versión | `semver.ps1 next` |
| `commits` | Listar commits desde última versión | `semver.ps1 commits` |
| `changelog` | Generar changelog automático | `semver.ps1 changelog --output CHANGELOG.md` |
| `tag` | Crear tag de versión en Git | `semver.ps1 tag v1.1.0` |
| `validate` | Validar formato de versión | `semver.ps1 validate v1.1.0` |
| `validate-commits` | Validar commits en Conventional Commits | `semver.ps1 validate-commits develop..HEAD` |

## 📊 Mapeo de commits a versiones

```
                    MAJOR   MINOR   PATCH
feat                  -      ✓        -       (nueva característica)
fix                   -      -        ✓       (corrección de bug)
perf                  -      -        ✓       (optimización)
feat!                 ✓      -        -       (breaking change en feature)
fix!                  ✓      -        -       (breaking change en fix)
docs/refactor/test    -      -        -       (sin impacto en versión)
```

## 🔄 Workflow típico

### En desarrollo
```powershell
# Hacer commits con Conventional Commits
git commit -m "feat(reportes): agregar soporte Excel"
git commit -m "fix(importacion): corregir validación"
```

### Antes de release
```powershell
# 1. Determinar versión siguiente
.\semver.ps1 next                           # → v1.1.0

# 2. Generar changelog
.\semver.ps1 changelog --output CHANGELOG.md

# 3. Crear tag
.\semver.ps1 tag v1.1.0 --message "Release v1.1.0"

# 4. Mergear en main y empujar
git push origin main --tags
```

## 🔐 Restricciones y convenciones

### Commits obligatorios
- Formato: `tipo(scope): descripción`
- Tipos válidos: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
- Ejemplos en [references/conventional-commits.md](./references/conventional-commits.md)

### Versiones válidas
- Formato: `vMAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`
- Ejemplos: `v1.0.0`, `v1.2.3-rc.1`, `v2.0.0-beta+build.1`
- Especificación en [references/semver-spec.md](./references/semver-spec.md)

### Tags en Git
- Siempre con prefijo `v`: `v1.2.3` (no `1.2.3`)
- Anotados (no lightweight): `git tag -a v1.2.3 -m "..."`
- Mensajes descriptivos: "Release v1.2.3: descripción de cambios"

## 💾 Archivos generados

### CHANGELOG.md
Se genera automáticamente agrupando commits por tipo:
- ⚠️ BREAKING CHANGES
- ✨ Added (nuevas características)
- 🐛 Fixed (correcciones)
- ⚡ Performance (optimizaciones)

Ejemplo en [references/changelog-format.md](./references/changelog-format.md)

## 🤝 Integración con el proyecto

### Ubicación en proyecto
- Script principal: `.codex/skills/versionamiento-semantico/scripts/semver.ps1`
- Funciones auxiliares: `.codex/skills/versionamiento-semantico/scripts/semver-functions.ps1`

### Prerequisitos
- PowerShell 7+ (o Windows PowerShell 5.1+)
- Git instalado y configurado
- Repositorio Git inicializado

### Variables de entorno (opcionales)
- `SEMVER_SKIP_PRERELEASE`: Omitir versiones pre-release
- `SEMVER_INCLUDE_BREAKING_CHANGES`: Enfatizar breaking changes

## 📚 Referencias externas

- [Semantic Versioning 2.0.0](https://semver.org/) - Especificación oficial
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) - Formato de commits
- [Keep a Changelog](https://keepachangelog.com/) - Formato de changelog

## 🆘 Troubleshooting

### "No hay commits nuevos"
→ No hay commits desde último tag. Verifica: `git log --oneline v1.0.0..HEAD`

### "Versión inválida"
→ Usa formato `vMAJOR.MINOR.PATCH` (ej: `v1.2.3`)

### "Commit inválido"
→ Usa Conventional Commits: `tipo(scope): descripción` (ej: `feat(reportes): `)

### "Error de validación en changelog"
→ Revisa que todos los commits desde último tag sigan Conventional Commits

## 📝 Cambios recientes

| Fecha | Cambio |
|---|---|
| 2026-05-17 | Creación inicial de la skill |

## 📞 Soporte

Para problemas, consulta:
1. [QUICK-START.md](./QUICK-START.md) - Troubleshooting section
2. [README.md](./README.md) - Conceptos y workflow
3. `references/` - Especificaciones detalladas
