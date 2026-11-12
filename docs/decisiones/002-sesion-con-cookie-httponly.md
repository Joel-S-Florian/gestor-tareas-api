# ADR 002 — Sesión: access token en memoria y refresh token en cookie HttpOnly

**Estado:** aceptada (2026-11-11)

## Contexto
El SPA guardaba el access token y el refresh token en `localStorage`: cualquier fallo de XSS los expone, y el refresh token dura 7 días. Además, la API guardaba el refresh token en texto plano y no detectaba que alguien reutilizara uno ya rotado.

## Decisión
- **Access token (JWT, 15 min):** solo en memoria del SPA. Al recargar la página se recupera con la cookie.
- **Refresh token:** cookie `gt_rt` `HttpOnly`, `SameSite=Lax`, `Path=/api/v1/auth`, `Secure` si la petición es HTTPS. El JavaScript nunca lo ve ni viaja en el JSON.
- **En la base:** solo el hash SHA-256 del refresh token.
- **Rotación con detección de reuso:** cada refresh revoca el token usado y emite uno nuevo. Si llega un token ya revocado, se asume robo y se cierran **todas** las sesiones del usuario.
- **Mismo sitio:** en desarrollo el SPA llama a `/api/v1` en su mismo origen y Vite redirige `/api` a la API; con otro despliegue habría que hacer lo mismo con un proxy, o configurar `Auth:CookieSameSite=None`.
- Los intentos de login, registro y refresh están limitados a 10 por minuto y por IP.

## Consecuencias
- Un XSS ya no puede robar la sesión de larga duración, solo usar el access token mientras la pestaña vive.
- **Límite conocido:** dos pestañas que renuevan en el mismo instante con la misma cookie pueden disparar la detección de reuso y cerrar la sesión.
- Las cookies exigen que el SPA y la API compartan sitio; no sirve con dominios totalmente distintos sin `SameSite=None` (y los navegadores pueden bloquear cookies de terceros).
- No hay token CSRF: `SameSite=Lax` y el hecho de que la respuesta de `/auth/refresh` no es legible entre orígenes limitan el riesgo, pero no lo eliminan del todo.

## Alternativas descartadas
- **Dejar `localStorage` y documentar el riesgo.**
- **Access token también en cookie:** obliga a proteger todas las rutas contra CSRF.
