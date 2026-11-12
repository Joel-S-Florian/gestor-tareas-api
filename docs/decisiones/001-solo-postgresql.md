# ADR 001 — PostgreSQL como única base de datos

**Estado:** aceptada (2026-11-11)

## Contexto
El proyecto arrancó con SQL Server LocalDB en desarrollo y PostgreSQL previsto para la nube, con un interruptor `DB_PROVIDER`. La única migración era de SQL Server, así que el camino PostgreSQL nunca estuvo probado, y el README y el CV decían cosas distintas.

## Decisión
Un solo proveedor: **PostgreSQL** (Npgsql). Se eliminó `DB_PROVIDER` y el paquete de SQL Server, y se regeneró la migración inicial. Las migraciones se aplican en un solo sitio (`Program.cs`): en Development, o con `RUN_MIGRATIONS=true`.

## Consecuencias
- Lo que se prueba en local y en CI es lo mismo que correría en cualquier entorno.
- Las pruebas de integración usan PostgreSQL real, no un proveedor en memoria (ver [ADR 004](004-pruebas-con-postgresql-real.md)).
- Quien solo tenga SQL Server no puede ejecutar el proyecto sin cambiar el proveedor.

## Alternativas descartadas
- **Mantener los dos proveedores:** duplica migraciones y pruebas para un proyecto sin ese requisito.
- **SQLite para desarrollo:** comportamiento distinto en tipos, concurrencia y `ExecuteUpdate`.
