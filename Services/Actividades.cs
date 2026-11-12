using GestorTareas.Api.Data;
using GestorTareas.Api.Models;

namespace GestorTareas.Api.Services;

// Escribe una línea en la bitácora del proyecto. No guarda: va en el mismo SaveChanges que la acción.
public static class Actividades
{
    public static void Registrar(ApplicationDbContext db, int proyectoId, int? tareaId, int usuarioId, string mensaje) =>
        db.Actividades.Add(new Actividad { ProyectoId = proyectoId, TareaId = tareaId, UsuarioId = usuarioId, Mensaje = mensaje });

    public static string Nombre(EstadoTarea e) => e switch
    {
        EstadoTarea.ToDo => "Por hacer",
        EstadoTarea.InProgress => "En progreso",
        _ => "Hecho"
    };
}
