# Guía de Integración: Commits Convencionales y Versionamiento Automático

Este documento explica cómo los skills, CLAUDE.md, AGENTS.md y el sistema de versionamiento trabajan juntos en EditorDbf.

---

## Arquitectura de Integración

\\\
┌─────────────────────────────────────────────────────────────────┐
│                     Desarrollo Diario                            │
│  (código, tests, refactors en feature branches)                 │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│               Commit con Conventional Commits                    │
│  (/commits-convencionales verifica formato)                    │
│  Formato: tipo(ámbito): descripción                             │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│              Tests y Validación (/principios-arquitectura)      │
│  - xUnit tests pasan                                            │
│  - Arquitectura respeta SOLID                                   │
│  - ViewModels desacoplados de WPF                               │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│                 Pull Request a \master\                         │
│  (descripción incluye tipo y ámbito de cambios)                │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│                        Code Review                              │
│  (revisa AGENTS.md checklist)                                  │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Merge a \master\                             │
│  (preserva commits Conventional)                               │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│              Análisis Automático de Commits                     │
│  (analiza desde último tag)                                    │
│  - Cuenta: feat → MINOR | fix/docs/etc → PATCH | BREAKING → MAJOR
│  - Actualiza docs/CHANGELOG.md                                 │
│  - Incrementa EditorDbf.App.csproj::Version                   │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                                  ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Tag y Release (Manual)                       │
│  git tag -a vX.Y.Z -m "Release vX.Y.Z"                         │
│  git push origin vX.Y.Z                                        │
└─────────────────────────────────────────────────────────────────┘
\\\

---

## Documentos del Sistema

Resumen final: Se han integrado exitosamente Conventional Commits y Semantic Versioning en EditorDbf.

