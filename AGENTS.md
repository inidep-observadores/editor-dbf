# AGENTS.md

Instrucciones específicas para agentes de Claude Code que trabajen en este repositorio.

## Objetivo del proyecto

EditorDbf es un editor WPF de alto rendimiento para archivos DBF (dBASE) que proporciona:
- Visualización y edición de datos en grillas
- Filtrado avanzado con operadores SQL
- Consola SQL integrada
- Gestión de múltiples conexiones
- Sistema de temas light/dark
- Exportación de datos

Versión actual: **0.5.0**

---

## Reglas de Oro para Agentes

### 1. Convenciones de Commit (OBLIGATORIO)

**Todos los commits DEBEN seguir Conventional Commits**. Antes de crear un commit:

1. Invoca `/commits-convencionales` si hay dudas
2. Usa el formato: `tipo(ámbito): descripción`
3. Escribe en español
4. Sé específico y orientado al resultado
5. NO uses punto final en la descripción

**Tipos válidos**: `feat`, `fix`, `refactor`, `docs`, `test`, `build`, `style`, `perf`, `chore`, `ci`, `revert`

**Ejemplos correctos**:
- ✅ `feat(edición): agregar validación de entrada en campos numéricos`
- ✅ `fix(filtrado): corregir comparación de fechas con timezone`
- ✅ `refactor(services): extraer lógica de validación a clase separada`
- ✅ `test(viewmodels): ampliar tests para operadores de filtrado`

**Ejemplos incorrectos**:
- ❌ `Update stuff` — muy vago
- ❌ `feat: corregir bug.` — punto final
- ❌ `feat(all): cambio general` — ámbito demasiado genérico

### 2. Versionamiento Automático

Los commits se agrupan automáticamente en versiones:

| Tipo de commit | Incremento | Versión nueva |
|---|---|---|
| `feat` | MINOR | 0.x.0 |
| `fix`, `style`, `perf`, `docs`, `chore` | PATCH | 0.0.x |
| `BREAKING CHANGE` | MAJOR | x.0.0 |

**Ejemplo**:
- v0.5.0 actual
- Haces `feat(ui): nuevo panel`
- Siguiente: v0.6.0
- Haces `fix(filtrado): corregir bug`
- Siguiente: v0.6.1

NO actualices manualmente `EditorDbf.App.csproj::Version` — se hace automáticamente al tagear release.

### 3. Arquitectura y MVVM Estricto

Respeta la separación de capas:

```
Infrastructure/       ← ObservableObject, RelayCommand (base)
Models/              ← Datos puros, sin lógica
Services/            ← I/O, persistencia, acceso a archivos
ViewModels/          ← Lógica de presentación, comandos, estado
Views/               ← XAML puro, binding
```

Antes de refactorizar, invoca `/principios-arquitectura` si:
- Vas a introducir una clase nueva
- Vas a mover código entre capas
- Vas a crear una interfaz o abstracción

**Regla**: ViewModels DEBEN ser testables sin WPF. Si necesitas `MessageBox` o `SaveFileDialog`, injéctalo vía `IDialogService`.

### 4. Testing Obligatorio

- **NO haya código sin tests** en ViewModels o Services
- Tests viven en `EditorDbf.Tests/`
- Usa `xUnit` + `NSubstitute` para mocks + `FluentAssertions`
- Antes de PR, ejecuta: `dotnet test`

**Áreas de cobertura crítica**:
- Filtrado SQL (todos los operadores: `=`, `<>`, `>`, `<`, `>=`, `<=`, `ENTRE`, `LIKE`, `VACIO`, `NO VACIO`)
- Tipos de datos (string, numeric, date, bool)
- Validaciones de entrada
- Manejo de errors

### 5. Documentación de Cambios

Si tu PR toca:
- **Nuevas features**: Actualiza el ejemplo en CLAUDE.md si aplica
- **Cambios de API**: Documenta en docstrings (una línea máximo)
- **Arquitectura**: Actualiza las secciones correspondientes en CLAUDE.md
- **Breaking changes**: Usa `BREAKING CHANGE:` en el footer del commit

### 6. Interfaz de Usuario

- **Idioma**: TODO en español (labels, tooltips, mensajes de error)
- **Fecha**: Siempre formato `dd/MM/yyyy`
- **Temas**: Nuevos colores van en AMBOS `LightTheme.xaml` y `DarkTheme.xaml` con la misma clave
- **Binding**: Usa `{Binding Propiedad, Mode=TwoWay}` para data-aware controls
- **Performance**: DataGrid con 100k+ filas debe ser responsive

### 7. DBF y Persistencia

- El `DbfTableService` maneja conversión de tipos automáticamente
- Preserva el byte de firma DBF original
- El code page se detecta al cargar (fallback CP1252)
- Guardado usa archivo temporal + move atómico (respeta `.fpt`)

**NO hagas**:
- ❌ Guardar directo al archivo original
- ❌ Ignorar code page
- ❌ Confiar en los tipos del DataTable sin conversión

---

## Checklist antes de Hacer Commit

- [ ] Código compilado sin warnings: `dotnet build`
- [ ] Tests pasan: `dotnet test`
- [ ] Mensaje de commit sigue Conventional Commits
- [ ] Cambios son atómicos (un objetivo por commit)
- [ ] Documentación actualizada si aplica
- [ ] Colores nuevos en AMBOS temas
- [ ] Binding XAML correcto
- [ ] ViewModels testables (sin WPF directo)

## Checklist antes de PR

- [ ] Branch basado en `master`
- [ ] Todos los commits siguen Conventional Commits
- [ ] Descripción de PR incluye ámbito del cambio
- [ ] Tests nuevos para features nuevas
- [ ] No hay breaking changes no documentados
- [ ] CLAUDE.md actualizado si corresponde

---

## Flujo Típico

```powershell
# 1. Crear rama
git checkout -b feature/nueva-funcionalidad

# 2. Implementar (con tests)
# ... editar archivos ...
dotnet test

# 3. Commit con Conventional Commits
git add .
git commit -m "feat(ámbito): descripción"
# (O invoke /commits-convencionales si hay dudas)

# 4. Push
git push origin feature/nueva-funcionalidad

# 5. PR a master con descripción detallada
# La description debe listar el tipo y ámbito de cambios

# 6. Merge (squash si hay muchos commits exploratorios)
```

---

## Debugging

### En Visual Studio
```powershell
# Abre EditorDbf.sln y presiona F5
# Los breakpoints funcionan normalmente
```

### En terminal
```powershell
dotnet run --project EditorDbf.App
# Los logs aparecen en stdout
```

### Tests
```powershell
# Ejecutar todos
dotnet test

# Un test específico
dotnet test --filter "ClassName"

# Con verbosity
dotnet test --verbosity detailed
```

---

## Referencias Rápidas

| Recurso | Ubicación | Propósito |
|---|---|---|
| CLAUDE.md | `/CLAUDE.md` | Guía principal del proyecto |
| CHANGELOG | `docs/CHANGELOG.md` | Historial de versiones |
| Mapping commits | `docs/CHANGELOG-MAPPING.md` | Cómo se clasifican commits |
| Skill Commits | `.claude/skills/commits-convencionales/` | Guía Conventional Commits |
| Skill Arquitectura | `.claude/skills/principios-arquitectura/` | Principios SOLID, etc. |
| Versión actual | `EditorDbf.App/EditorDbf.App.csproj` | Tag `<Version>` |

---

## Dudas Frecuentes

**P: Mi commit es mitad feature, mitad fix. ¿Qué tipo uso?**
A: Separa en dos commits: uno para la feature, otro para el fix. Los cambios deben ser atómicos.

**P: ¿Puedo hacer breaking changes?**
A: Solo si es absolutamente necesario. Usa `feat!` o `BREAKING CHANGE:` en el footer. Incrementará MAJOR version.

**P: ¿Qué pasa con los commits que no siguen el formato?**
A: Los agentes los rechazarán en PR. Usa `git commit --amend` para reescribir antes de mergear.

**P: ¿Necesito actualizar CHANGELOG manualmente?**
A: NO. Se genera automáticamente del historial de commits al tagear releases.

**P: ¿Cómo hago un release nuevo?**
A: Tagging:
```powershell
git tag -a v0.6.0 -m "Release 0.6.0"
git push origin v0.6.0
```

---

## Alertas de Riesgo

🚨 **NUNCA**:
- Hacer commit sin tests (si es en ViewModel/Service)
- Ignorar warnings de build
- Pushear a `master` directo (siempre vía PR)
- Usar tipos Windows específicos en Services (hace testing imposible)
- Olvidar color en AMBOS temas
- Hacer breaking changes sin documentar

⚠️ **TEN CUIDADO**:
- Performance: La UI debe ser responsive con 100k+ filas
- Code pages: DBF soporta múltiples encodings
- Filtrado: Los operadores deben ser coherentes en SQL
- Threads: WPF es single-threaded; usa Dispatcher si necesitas async

---

Última actualización: 2026-05-17
Versión del proyecto: 0.5.0
