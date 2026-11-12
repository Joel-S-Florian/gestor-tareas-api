# ADR 003 — El proyecto no se despliega

**Estado:** aceptada (2026-11-11)

## Contexto
El plan original contemplaba un despliegue en la nube (Render y Vercel). El autor eliminó su cuenta de Render y prefiere no tener una versión en producción.

## Decisión
No habrá despliegue. Se quitaron `render.yaml` y `vercel.json`, y en GitHub se borraron los registros de despliegue y el enlace de producción. El proyecto se ejecuta en local y se valida con integración continua.

## Consecuencias
- Lo que se afirma del proyecto es lo que se puede ejecutar y probar: 50 pruebas de integración en la API y 11 en el SPA.
- El `Dockerfile` se conserva porque es genérico, pero no hay configuración de ningún proveedor.
- Para desplegarlo habría que: definir la base de datos gestionada, las variables `ConnectionStrings__DefaultConnection` y `Jwt__Secret`, `RUN_MIGRATIONS=true`, y servir API y SPA bajo el mismo dominio (ver [ADR 002](002-sesion-con-cookie-httponly.md)).
- El adjunto se guarda en disco local (`UploadsPath`): en un entorno con disco efímero se perderían al reiniciar.
