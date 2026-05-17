# Mapeo de Commits a Versiones Semánticas

Este documento detalla cómo se clasificaron los commits existentes del repositorio en versiones semánticas para construir el CHANGELOG.md.

## Metodología

Se usó la especificación [Conventional Commits](https://www.conventionalcommits.org/es/v1.0.0/) para clasificar commits que originalmente no seguían este formato, y [Semantic Versioning](https://semver.org/es/) para determinar incrementos de versión:

- **MAJOR** (X.0.0): cambios incompatibles que rompan compatibilidad
- **MINOR** (0.X.0): nuevas funcionalidades (feat)
- **PATCH** (0.0.X): correcciones (fix), documentación, estilos, etc.

---

## Versiones y Commits

### v0.0.1 (Inicial)
- `8222933` Initial commit: WPF DBF editor

### v0.1.0 (Core Features iniciales)
- `61cde21` Localizacion al español de mensajes y filtros → **feat(i18n)**
- `0e3e705` Rediseño completo de UI: Fluent/Windows 11 con tema claro/oscuro → **feat(ui): sistema de temas light/dark**
- `661528a` Optimizar barra de filtro: botones compactos (iconos) → **style(ui)**
- `da9f579` Optimizar selector de código de página: botones con solo íconos → **style(ui)**
- `28d457c` UI: Modernización completa del Editor DBF, soporte de temas → **feat(ui)**

### v0.2.0 (UI Improvements y Refinements)
- `949d1ba` style: fix Expander theme compatibility → **style**
- `9a6a00b` Implement CachedTabControl and UI refinements → **feat(ui)**
- `472c759` Add data export functionality and refresh button → **feat(exportación)**
- `c1d5d17` UI: fix mismatched tags, reposition filter buttons → **fix(ui)**
- `d877bab` Fix file access in SaveTable and single-instance check → **fix(core)**
- `a9fbcda` Ignore publish folders and track installer script → **chore**
- `20e66c9` Organizar instalador en carpeta setup y automatizar → **build**

### v0.3.0 (SQL Console - Feature Mayor)
- `513e3bb` feat: implementar consola SQL con ejecución y motor SQLite → **feat(sql)**
- `a64464a` Implementación de navegación contextual y explorador SQL → **feat(ui)**
- `c1c07a0` Optimización de UX en consola SQL → **feat(sql)**
- `efdb3e5` Finalización de consola SQL → **feat(sql)**
- `e7ceccc` build: cambiar ruta de instalación a Program Files (64 bits) → **build**
- `3dfa008` Fix: Configuración de ícono de aplicación → **fix(build)**
- `a2d7988` fix: resolver errores de inicio de la aplicacion → **fix**
- `22140b9` merge: fusionar rama test con correcciones → **chore**
- `04af071` Actualizar nombre del ejecutable del instalador → **build**

### v0.4.0 (Advanced Filtering & Mass Operations - La versión más grande)
- `913032b` Implementación de filtrado avanzado SQL → **feat(filtrado)**
- `ff90276` Permitir combinación de filtros con AND → **feat(filtrado)**
- `6c3caa1` Funcionalidad de cambio de valor masivo → **feat(edición)**
- `873fac2` fix: corregir formato de fecha dd/MM/yyyy → **fix**
- `4826267` Implementar validación de cambios pendientes al cerrar → **feat(ui)**
- `bd2e6b0` Mejoras en gestión de archivos y filtrado avanzado → **feat(múltiple)**
- `5aefb33` Ajuste en comparaciones de fecha por día completo → **fix**
- `ee9fb75` Añadidos filtros 'Está vacío' y 'No está vacío' → **feat(filtrado)**
- `8ad4bd8` Mejoras en gestión de conexiones (F2, tooltips, etc.) → **feat(conexiones)**
- `a9b0a9c` Validación dinámica de rutas al activar ventana → **feat(ui)**
- `296112e` Fix: InvalidCastException en menú contextual → **fix**
- `5ec87e4` Implementación de exportación avanzada y renombrado → **feat(exportación)**
- `b99b7d0` Mejora de navegación en grilla → **feat(ui)**
- `e62ee62` Implementar cambio de parte de fecha en lote → **feat(edición)**
- `b654335` Alinear columnas por tipo y resaltar modificadas → **feat(ui)**
- `5a51cdb` Eliminar archivo temporal de compilación → **chore**
- `a2a33f0` feat: conversión automática punto a coma en numéricos → **feat(edición)**

### v0.5.0 (Testing & Architecture - Refactor y Quality)
- `c60b8a7` Fix: decimal separator loss in filtering → **fix(filtrado)**
- `d517398` docs: expand CLAUDE.md with debugging, project structure → **docs**
- `f8dbd72` refactor: decouple ViewModels from WPF dialogs → **refactor(architecture)** ⚠️ BREAKING CHANGE en interno
- `05d596a` test: add comprehensive unit tests → **test**
- `b968b80` Test: configure unit test discovery → **test**

---

## Criterios de incremento de versión

### De v0.0.1 → v0.1.0 (MINOR)
Razón: Introducción de features importantes (localización, sistema de temas completo).

### De v0.1.0 → v0.2.0 (MINOR)
Razón: Nuevas features (exportación, CachedTabControl), múltiples mejoras.

### De v0.2.0 → v0.3.0 (MINOR)
Razón: Feature mayor nueva (SQL Console completa) + navegación contextual.

### De v0.3.0 → v0.4.0 (MINOR)
Razón: La versión más grande del proyecto. Decenas de features nuevas:
- Filtrado avanzado con múltiples operadores
- Edición masiva de datos
- Gestión mejorada de conexiones
- Exportación avanzada
- Refinamientos extensos de UI

### De v0.4.0 → v0.5.0 (MINOR)
Razón: Suite de testing comprensiva + refactor arquitectónico importante para testabilidad.

---

## Observaciones

1. **No hay BREAKING CHANGE mayor**: Aunque la refactorización de ViewModels en v0.5.0 es significativa internamente, no afecta la API pública visible al usuario, por lo que es MINOR (0.x.0) no MAJOR.

2. **Versión 0.4.0 fue la más grande**: Contiene aproximadamente 40% de todos los commits, indicando una fase intensiva de desarrollo de features.

3. **Patrón de desarrollo**: El proyecto seguía un patrón de:
   - Fase 1 (v0.1.0): UI base + localización
   - Fase 2 (v0.2.0): Refinamientos
   - Fase 3 (v0.3.0): Feature grande (SQL Console)
   - Fase 4 (v0.4.0): Expansión masiva de features
   - Fase 5 (v0.5.0): Estabilización con testing

4. **Commits sin formato convencional**: La mayoría de commits antiguos no seguían Conventional Commits. Se clasificaron heurísticamente analizando el asunto y el cuerpo del commit.

---

## Próximos pasos

Nuevos commits deben seguir el formato `tipo(ámbito): descripción` como se detalla en la especificación de Conventional Commits en el repositorio.
