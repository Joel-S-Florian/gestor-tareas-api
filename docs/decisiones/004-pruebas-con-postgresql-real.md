# ADR 004 — Pruebas de integración con la API real y PostgreSQL real

**Estado:** aceptada (2026-11-11)

## Contexto
El proyecto no tenía pruebas. Las reglas importantes (roles, rotación de tokens, adjuntos) viven en servicios que dependen de EF Core y de la base.

## Decisión
`GestorTareas.Tests` levanta la API completa con `WebApplicationFactory` contra una base PostgreSQL propia por ejecución (`gt_test_<id>`, se crea y se borra). El servidor se configura con `GT_TEST_PG` (por defecto, el PostgreSQL local). En CI se usa un servicio `postgres:16` de GitHub Actions.

Las reglas se prueban como matriz: el usuario demo tiene un rol distinto en cada proyecto sembrado (Owner, Editor, Viewer), así que una sola prueba parametrizada recorre rol × acción.

## Consecuencias
- Se prueba la ruta completa: autenticación, validación, middleware de errores, EF y SQL.
- Cada prueba comparte la base de la ejecución, por lo que los datos que crea deben usar títulos propios.
- Una prueba mal aislada contaminó la base de desarrollo al principio (el `Program` volvía a agregar `appsettings` y pisaba la configuración de prueba); se corrigió quitando esas fuentes redundantes.
- Comprobado por mutación: al desactivar la regla de borrado de proyecto, la matriz falla.

## Alternativas descartadas
- **Proveedor en memoria de EF Core:** no valida SQL, índices ni `ExecuteUpdate`.
- **Pruebas unitarias por servicio con mocks:** no detectarían errores de configuración ni de pipeline.
