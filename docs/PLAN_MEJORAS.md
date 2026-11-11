# Plan de reconstrucción — Gestor de Tareas (API + SPA)

Cubre las dos repos: `gestor-tareas-api` (.NET 8) y `gestor-tareas-spa` (React 18 + Vite).
Objetivo: que lo que dice el CV (ASP.NET Core 8 + React + JWT con refresh + PostgreSQL, roles, adjuntos, nube) sea **verdad y demostrable**.

**Estado (2026-11-11):** fases 1 a 6 hechas. Decisión del autor: **no habrá despliegue ni producción**; se quitaron `render.yaml` y `vercel.json`, y se borraron los despliegues y el enlace de producción en GitHub. La fase 7 queda reducida a documentación (README único, diagrama, ADR, capturas) y la 8 al CV y el STAR.

## Diagnóstico (2026-11-11, servidores levantados en local)

| # | Hallazgo | Dónde | Gravedad |
|---|---|---|---|
| 1 | Login "demo" fijo `joel@demo.com`/`Demo1234!` que funciona en cualquier entorno; si la BD falla emite token de un usuario falso (id 9999) | `AuthService.cs:44-92` | Crítica |
| 2 | La única migración es de SQL Server; producción (`render.yaml`) usa PostgreSQL | `Migrations/`, `Program.cs:35` | Alta |
| 3 | Sin pruebas ni CI en ninguna repo; despliegue (Hito 5) sin terminar y sin URL pública | ambas | Alta |
| 4 | Documentación contradictoria: 3 juegos de credenciales, SQL Server vs PostgreSQL | READMEs, `SeedData.cs` | Media |
| 5 | Tokens (access y refresh) en `localStorage` | `api.js` | Media |
| 6 | Adjuntos en disco local: en Render (plan gratis) se pierden al reiniciar; el tipo se valida con el `Content-Type` que envía el cliente | `AdjuntoService.cs` | Media |
| 7 | `PageSize` sin tope; `Include` de dos colecciones en el listado (carga de más) | `TareaDtos.cs:53`, `TareaService.cs:41` | Media |
| 8 | AutoMapper y FluentValidation instalados y sin uso; `BuildServiceProvider` manual; migraciones aplicadas en dos sitios | `.csproj`, `Program.cs` | Baja |
| 9 | La marca en pantalla es "TabTask", el proyecto se llama Gestor de Tareas | SPA | Baja |

Verificado y **sin problema**: el adjunto se guarda con nombre `Guid` (no hay recorrido de rutas); el refresh rota y revoca tokens; los roles se validan en el servicio.

## Decisiones a tomar antes de empezar

1. **Base de datos:** PostgreSQL como única (recomendado: coincide con el CV y con producción; se usa también en local con el Postgres 16 que ya corre) o mantener las dos.
2. **Sesión en el SPA:** refresh token en cookie HttpOnly + access token en memoria (recomendado) o dejar `localStorage` y documentar el riesgo.
3. **Adjuntos en producción:** almacenamiento de objetos (Cloudflare R2 / S3) o limitar a base de datos/`bytea` (más simple, tope 5 MB).
4. **Dónde desplegar:** Render + Vercel (ya hay `render.yaml` y `vercel.json`) o cambiar.

## Fases

Cada fase termina con pruebas verdes, commit y una línea en `docs/decisiones/` si hubo una decisión de diseño (ADR corto).

### Fase 1 — Seguridad inmediata
- Eliminar el login fijo y el fallback simulado; el usuario demo vive solo en el seed de Development.
- Sacar el secreto JWT de `appsettings.json`; sin secreto válido la API **no arranca** (hoy cae a una clave por defecto).
- Tope de `PageSize`; límite de intentos de login.
- **Hecho cuando:** una prueba demuestra que `joel@demo.com`/`Demo1234!` no entra si no está sembrado y que el arranque falla sin secreto.

### Fase 2 — Base de datos única y migraciones
- Un solo proveedor (según decisión 1); regenerar la migración inicial; aplicar migraciones en un único lugar.
- Índices en claves de consulta (`ProyectoId`, `AsignadoAId`, `Email` único).
- **Hecho cuando:** `dotnet ef database update` crea el esquema limpio en PostgreSQL y el seed carga.

### Fase 3 — Pruebas y CI (API)
- xUnit + `WebApplicationFactory` + PostgreSQL real (Testcontainers o servicio de GitHub Actions).
- Casos: matriz de roles (Owner/Editor/Viewer × cada endpoint), rotación y revocación del refresh, regla "no borrar proyecto con tareas", adjuntos (tamaño, tipo, rol).
- GitHub Actions: build + pruebas en cada PR.
- **Hecho cuando:** CI verde y la matriz de roles es una prueba parametrizada, no una lista a mano.

### Fase 4 — Calidad del código de la API
- Quitar AutoMapper/FluentValidation, o usarlos de verdad (recomendado: quitar, DTOs con atributos ya validan).
- Quitar `BuildServiceProvider`; proyección a DTO en el listado de tareas en vez de `Include` doble; `AsNoTracking` en lecturas.
- Validar adjuntos por firma del archivo (magic bytes), no solo por `Content-Type`.
- **Hecho cuando:** sin advertencias de compilación y el listado de tareas hace una consulta sin duplicar filas.

### Fase 5 — SPA: sesión y calidad
- Sesión según decisión 2; el interceptor de refresh con cola se conserva y se prueba.
- Pruebas con Vitest + Testing Library (interceptor, rutas protegidas, permisos de Viewer) y una prueba de extremo a extremo del flujo Kanban con Playwright.
- Unificar nombre de marca; revisar móvil y modo oscuro; accesibilidad del arrastrar y soltar (hoy hay un select de respaldo en el modal, mantenerlo).
- **Hecho cuando:** CI de la SPA corre lint, pruebas y build.

### Fase 6 — Producto
- Orden de tareas dentro de la columna (hoy solo cambian de estado); fecha de vencimiento visible y filtro "vencidas".
- Notificación simple al ser asignado (sin correo, solo en la app).
- Solo lo que haga la demo más convincente; nada de tiempo real/WebSockets salvo que se decida.

### Fase 7 — Despliegue y documentación
- Desplegar API + SPA + PostgreSQL gestionado; CORS de producción; `/health` con comprobación de BD.
- README único y coherente (una sola tabla de credenciales demo), diagrama de arquitectura, ADR, capturas.
- URLs públicas en el README y en el CV.
- **Hecho cuando:** el flujo login → proyecto → tarea → adjunto funciona en la URL pública.

### Fase 8 — Cierre
- Actualizar el CV (.md y .tex) y escribir el STAR de este proyecto, con cifras reales (pruebas, fases, ADR) y los límites declarados.
- Decidir si se publica una vitrina o el repo ya público basta.

## Fuera de alcance (a propósito)
Correo de confirmación, recuperación de contraseña, tiempo real, aplicación móvil, multi-tenant.

## Riesgos
- El Postgres de Render gratis caduca a los 30 días: avisar en el README o elegir otro plan.
- Cambiar el proveedor de BD invalida la migración actual: no hay datos de producción que conservar (el despliegue nunca terminó), así que es el mejor momento.
- Los commits antiguos conservan el login fijo y el secreto de desarrollo; al ser repos públicos, eso queda en el historial (no es un secreto real, pero se menciona en el ADR).
