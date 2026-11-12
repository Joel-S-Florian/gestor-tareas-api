# ADR 003 — El proyecto no tiene producción ni despliegue

**Estado:** aceptada (2026-11-11)

## Contexto
El plan original contemplaba un despliegue en la nube (Render y Vercel). El autor eliminó su cuenta de Render y no quiere una versión en producción.

## Decisión
No hay ni habrá producción. Se retiró todo lo que servía para desplegar:
- `render.yaml`, `vercel.json` y el `Dockerfile`.
- La conversión de URL `postgres://` (formato de Render), el manejo de cabeceras de proxy y el interruptor `RUN_MIGRATIONS`.
- En GitHub: los registros de despliegue, los entornos y el enlace de producción del repositorio del SPA.

Swagger, las migraciones automáticas y los datos de demostración solo existen en el entorno Development.

## Consecuencias
- Lo que se afirma del proyecto es lo que se puede ejecutar y probar en local: 50 pruebas de integración en la API y 11 en el SPA.
- Fuera de Development, la API arranca sin Swagger y sin aplicar migraciones: no está pensada para ese uso.
- Si algún día se quisiera desplegar, habría que decidir de nuevo el alojamiento, la base gestionada, los secretos y dónde guardar los adjuntos (hoy van a disco local), y servir API y SPA bajo el mismo dominio por la cookie de sesión (ver [ADR 002](002-sesion-con-cookie-httponly.md)).
