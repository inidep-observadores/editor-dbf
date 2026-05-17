# Instalación y configuración

Instrucciones para instalar y configurar la skill de versionamiento semántico.

## ✅ Instalación automática

La skill ya está instalada en:

```
d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026\.codex\skills\versionamiento-semantico\
```

## 🚀 Uso inmediato

Abre PowerShell en la carpeta del proyecto y prueba:

```powershell
cd d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026

# Ver ayuda
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 help

# Determinar siguiente versión
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 next

# Listar commits nuevos
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 commits

# Validar una versión
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 validate v1.2.3
```

## 🎯 Configuración recomendada

### Opción 1: Crear alias en PowerShell (Recomendado)

Edita tu perfil de PowerShell:

```powershell
# Abrir editor de perfil
notepad $PROFILE
```

Agrega estas líneas:

```powershell
# Alias para versionamiento semantico
$semverPath = "d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026\.codex\skills\versionamiento-semantico\scripts\semver.ps1"
if (Test-Path $semverPath) {
    New-Alias -Name semver -Value $semverPath -Force -Scope Global
}
```

Luego usa:

```powershell
semver help
semver next
semver changelog --output CHANGELOG.md
semver tag v1.2.3 --message "Release v1.2.3"
semver validate v1.2.3
```

### Opción 2: Variable de entorno

```powershell
[Environment]::SetEnvironmentVariable("SEMVER_SCRIPT", "d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026\.codex\skills\versionamiento-semantico\scripts\semver.ps1", [EnvironmentVariableTarget]::User)
```

Luego:

```powershell
& $env:SEMVER_SCRIPT help
```

## 📋 Requisitos previos

- ✅ PowerShell 5.1+ (incluido en Windows 10+)
- ✅ Git instalado (`git --version` para verificar)
- ✅ Repositorio Git inicializado (ya lo tienes)

## 🔍 Verificación de instalación

Ejecuta esta prueba:

```powershell
cd d:\Desarrollo\_INIDEP\OBS\OBS-Arrastre-2026

# 1. Verificar que el script existe
Test-Path ".\.codex\skills\versionamiento-semantico\scripts\semver.ps1"
# Debe retornar: True

# 2. Verificar que Git funciona
git --version

# 3. Ejecutar comando
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 help
# Debe mostrar la ayuda
```

## 📚 Documentación

Después de instalar, lee:

1. **Para empezar rápido**: [QUICK-START.md](./QUICK-START.md)
2. **Para entender el framework**: [README.md](./README.md)
3. **Para especificaciones técnicas**: `references/` folder

## 🆘 Troubleshooting

### "El script no se ejecuta"

Verifica la política de ejecución de PowerShell:

```powershell
Get-ExecutionPolicy
```

Si retorna "Restricted", cambia a:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### "Git no se encuentra"

Instala Git desde: https://git-scm.com/download/win

### "No hay commits nuevos"

Esto significa que:
1. No hay tag de versión aún (normal para primer uso), O
2. No hay commits desde el último tag

Crea un tag inicial:

```powershell
.\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag v0.1.0 --message "Versión inicial"
```

## 📝 Próximos pasos

1. **Entender el workflow**:
   - Lee [QUICK-START.md](./QUICK-START.md)
   - Entiende cómo escribir commits en Conventional Commits

2. **Crear CHANGELOG.md inicial** (opcional):
   ```powershell
   .\.codex\skills\versionamiento-semantico\scripts\semver.ps1 changelog --output CHANGELOG.md --draft
   ```

3. **Crear primera etiqueta de versión**:
   ```powershell
   .\.codex\skills\versionamiento-semantico\scripts\semver.ps1 tag v1.0.0 --message "Release: Versión inicial del producto"
   ```

4. **Integrar en CLAUDE.md** (opcional):
   - Ver [INTEGRACION-CLAUDE.md](./INTEGRACION-CLAUDE.md)

## 📞 Soporte adicional

- 📖 Lee la documentación en `references/`
- 🔧 Customiza el script según necesites
- 📝 Contribuye mejoras al proyecto

## Versión

Skill de Versionamiento Semántico v1.0.0
Creado: 2026-05-17
Requiere: PowerShell 5.1+, Git 2.0+
