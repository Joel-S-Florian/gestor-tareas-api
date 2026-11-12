# ADR 005 — Actividad, menciones, lista de pasos y etiquetas

**Estado:** aceptada (2026-11-12)

## Contexto
El tablero solo permitía mover tareas y comentar. Faltaba saber qué pasó y quién lo hizo, avisar a una persona concreta en un comentario, dividir una tarea en pasos y clasificarlas.

## Decisiones

**Actividad como mensajes ya redactados.** Cada línea de la bitácora guarda el texto de la acción con el título de la tarea *de ese momento* (`movió «X» de Por hacer a Hecho`) y el autor por clave foránea. Así sigue siendo legible si la tarea se renombra o se elimina (`TareaId` no tiene clave foránea a propósito). Se escribe en el mismo `SaveChanges` que la acción. Solo se registra lo que cambia algo: reordenar dentro de la columna, guardar sin cambios y marcar pasos sueltos no generan líneas; completar el último paso de la lista sí, una sola vez.

**Menciones por email.** `@ana` coincide con el email que empieza por `ana@`. No hay nombres de usuario en el sistema y los nombres completos son ambiguos; el prefijo del email es único mientras los emails lo sean y no añade columnas. Solo se avisa a *miembros del proyecto* (no se revela quién existe), nunca al autor, y un email escrito dentro del texto (`juan@ana.com`) no cuenta (el `@` debe ir precedido de espacio o inicio). Límite: dos personas con el mismo prefijo en dominios distintos y miembros del mismo proyecto recibirían ambas el aviso.

**Lista de pasos.** Tabla propia con `Orden`, tope de 50 por tarea y 200 caracteres por paso. La lista y el progreso viajan en el listado (conteos por proyección) y el detalle trae los pasos completos.

**Etiquetas por proyecto.** Relación muchos a muchos con la tarea, nombre único por proyecto sin distinguir mayúsculas, topes de 30 por proyecto y 10 por tarea, y solo se asigna una etiqueta del mismo proyecto. Borrar una etiqueta la quita de las tareas (cascada de la tabla de unión).

**Permisos.** Leer: cualquier miembro. Crear, editar y borrar pasos y etiquetas y etiquetar tareas: Owner y Editor, igual que las tareas. Comentar sigue abierto al Viewer.

## Consecuencias
- Las bitácoras crecen sin límite en la base; la API devuelve las últimas 100 y no hay purga.
- Las cuatro funciones están cubiertas por 13 pruebas de integración, y una por mutación (quitar el filtro de miembros en las menciones) falla como debe.

## Alternativas descartadas
- **Reconstruir la actividad desde las tablas:** no sobreviviría a borrados ni renombrados.
- **Nombre de usuario propio para mencionar:** más fricción en el registro para un proyecto de este tamaño.
- **Registrar cada paso marcado:** llenaría la bitácora de ruido.
