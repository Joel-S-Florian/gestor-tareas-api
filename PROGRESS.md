# Progreso — Gestor de Proyectos y Tareas (Capstone)

## ✅ Hito 1 — Modelo de datos + migraciones + autenticación (Semana 12)
- [x] Entidades: Usuario, Proyecto, MiembroProyecto, Tarea, Comentario, Adjunto, RefreshToken
- [x] DbContext con Fluent API (3FN, claves compuestas, cascadas correctas)
- [x] SeedData: 2 usuarios, 2 proyectos, 5+ tareas
- [x] Auth: register / login / refresh (con rotación) / logout
- [x] JWT + PasswordHasher (ASP.NET Core Identity hasher)
- [x] Swagger con botón Authorize
- [x] Middleware global de errores (ProblemDetails)
- [x] Health check `/health`
- [x] CORS configurado (variable `SpaOrigin`)

## ✅ Hito 2 — CRUD de Proyectos y Tareas + Swagger (Semana 13)
- [x] DTOs de Proyecto (crear/actualizar/respuesta/miembros)
- [x] DTOs de Tarea (crear/actualizar/cambiar estado/filtros/respuesta paginada)
- [x] ProyectosController (GET/POST/PUT/DELETE + invitar/remover/cambiar rol de miembros)
- [x] TareasController (CRUD + PATCH estado + paginación/filtros por query string)
- [x] Autorización por ownership y rol (Owner/Editor/Viewer) centralizada en ProyectoService
- [x] Regla: no se puede eliminar un proyecto con tareas
- [x] Regla: no se puede asignar una tarea a alguien que no es miembro del proyecto
- [x] Excepciones de dominio (404/403/409) mapeadas a ProblemDetails en el middleware

## ✅ Hito 3 — SPA con login, dashboard y tablero Kanban (Semana 13–14)
- [x] Scaffolding Vite + React Router + Axios
- [x] AuthContext + interceptor de refresh automático (con cola para no duplicar refresh en paralelo)
- [x] Pantallas: Login, Registro, Dashboard, Tablero Kanban, Detalle/creación de tarea, 404
- [x] Drag & drop entre columnas (con fallback de select de estado en el modal)
- [x] Filtros por prioridad y asignado
- [x] Rutas protegidas
- [x] Permisos en UI: Viewer no ve botones de edición

## ✅ Hito 4 — Comentarios, adjuntos, miembros, perfil (Semana 14)
- [x] ComentariosController + AdjuntosController (multipart, límite 5MB, validación MIME)
- [x] UI de comentarios y adjuntos en el detalle de tarea
- [x] UI de gestión de miembros (invitar/remover/cambiar rol)
- [x] Pantalla de Perfil (GET/PUT `/auth/perfil`)
- [x] Editar y eliminar proyecto desde el tablero (Owner/Editor), con confirmación
- [x] **Integración**: enums serializados como strings (JsonStringEnumConverter) para que el SPA los consuma legibles
- [x] **Calidad**: ExceptionMiddleware mapea excepciones de dominio a 404/403/409/401 (antes devolvía 500 en todo)
- [x] **Calidad**: logging estructurado con `ILogger<T>` en Auth/Proyectos/Tareas/Comentarios/Adjuntos

## ⬜ Hito 5 — Despliegue en la nube (Semana 14–15)
- [ ] Backend en Azure App Service / Render / Railway
- [ ] Base de datos gestionada (Azure SQL / PostgreSQL)
- [ ] Frontend en Vercel / Netlify
- [ ] CORS de producción apuntando al dominio real del SPA
- [ ] Migraciones ejecutadas contra la BD productiva
- [ ] URLs públicas en el README

---
**Cómo retomar:** dime "sigamos con Hito 5 (despliegue)" y configuramos el proveedor de nube.