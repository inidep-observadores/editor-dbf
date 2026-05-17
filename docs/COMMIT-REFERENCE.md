# Referencia Rápida de Conventional Commits

Guía de bolsillo para escribir commits en EditorDbf.

## Formato

```
tipo(ámbito): descripción

[cuerpo opcional - líneas de 72 caracteres máximo]

[footer opcional]
```

## Tipos

| Tipo | Cuándo | SEMVER |
|------|--------|--------|
| `feat` | Nueva funcionalidad | MINOR (0.x.0) |
| `fix` | Corrección de bug | PATCH (0.0.x) |
| `refactor` | Reorganización sin cambio | PATCH |
| `perf` | Mejora de rendimiento | PATCH |
| `style` | Formato, linting | PATCH |
| `docs` | Documentación | PATCH |
| `test` | Tests nuevos/modificados | PATCH |
| `build` | Build, dependencias | PATCH |
| `ci` | CI/CD changes | PATCH |
| `chore` | Mantenimiento | PATCH |
| `revert` | Revierte un commit | varía |

## Ámbitos válidos

- `ui` — Interfaz de usuario, XAML, controles
- `filtrado` — Lógica de filtrado SQL
- `edición` — Edición de datos, validación
- `exportación` — Export de datos
- `conexiones` — Gestión de conexiones
- `temas` — Sistema de temas light/dark
- `consola` — Consola SQL
- `services` — Services (DbfTableService, etc.)
- `viewmodels` — ViewModels
- `models` — Modelos de datos
- `architecture` — Cambios arquitectónicos
- `testing` — Tests
- `installer` — Instalador
- `core` — Core / no encaja en otro

## Ejemplos

### Feature

```
feat(filtrado): agregar operador CONTIENE para búsqueda de texto
```

### Bug fix

```
fix(edición): corregir pérdida de separador decimal en campos numéricos
```

### Refactor

```
refactor(services): extraer validación de DBF a clase separada
```

### Test

```
test(filtering): agregar tests para operador ENTRE
```

### Documentación

```
docs: actualizar CLAUDE.md con instrucciones de debugging
```

### Build

```
build(installer): cambiar ruta de instalación a Program Files
```

## Con BREAKING CHANGE

```
feat(api)!: cambiar estructura de respuesta del filtrado

BREAKING CHANGE: campo 'conditions' renombrado a 'filters'
```

O alternativamente:

```
feat(api): cambiar estructura de respuesta del filtrado

BREAKING CHANGE: campo 'conditions' renombrado a 'filters'
```

## Reglas Clave

✅ **DO**:
- Usa presente imperativo: "agregar", no "agrega" ni "agregado"
- Sé específico: "corregir pérdida de decimal en filtros" no "bug fix"
- Español siempre
- Una línea de descripción < 50 caracteres
- Cuerpo líneas < 72 caracteres

❌ **DON'T**:
- Punto final en descripción
- Múltiples features en un commit
- Commits vagos ("Update", "Fix stuff")
- Inglés (salvo en nombrado de código)

## Incremento de versión

```
v0.5.0 (actual)

+ feat(x) = v0.6.0 (MINOR)
+ fix(x)  = v0.5.1 (PATCH)
+ feat! / BREAKING CHANGE = v1.0.0 (MAJOR)
```

## Comando git

```powershell
# Commit simple
git commit -m "feat(filtrado): agregar operador ENTRE"

# Con cuerpo multiline
git commit -m "feat(filtrado): agregar operador ENTRE" -m "Permite filtrar rangos de valores numéricos en columnas."

# Amend si cometiste error
git commit --amend --no-edit
# (Luego edita el mensaje con git commit --amend)
```

## Duda rápida

📍 **"¿Es feat o fix?"**
- Agregaste capacidad nueva → `feat`
- Arreglaste comportamiento incorrecto → `fix`

📍 **"¿Debo cambiar MAJOR version?"**
- Solo si rompe compatibilidad con usuarios
- Si es interno (refactor) → no necesariamente MAJOR

📍 **"¿Puedo hacer un commit sin tests?"**
- En Views/XAML: sí
- En ViewModels/Services: NO (requiere tests)

---

**Referencia oficial**: https://www.conventionalcommits.org/es/v1.0.0/
