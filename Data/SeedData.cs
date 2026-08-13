using GestorTareas.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace GestorTareas.Api.Data;

public static class SeedData
{
    // Se ejecuta al arrancar en Development si la base está vacía (ver Program.cs).
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Usuarios.Any()) return;

        var hasher = new PasswordHasher<Usuario>();

        var user1 = new Usuario { Nombre = "Joel Pérez", Email = "joel@demo.com", FechaRegistro = DateTime.UtcNow };
        user1.PasswordHash = hasher.HashPassword(user1, "Demo1234!");

        var user2 = new Usuario { Nombre = "Ana Gómez", Email = "ana@demo.com", FechaRegistro = DateTime.UtcNow };
        user2.PasswordHash = hasher.HashPassword(user2, "Demo1234!");

        context.Usuarios.AddRange(user1, user2);
        context.SaveChanges();

        var proyecto1 = new Proyecto
        {
            Nombre = "Proyecto Capstone",
            Descripcion = "Gestor de tareas para Tecnologías del Internet",
            Color = "#6366f1",
            PropietarioId = user1.Id
        };

        var proyecto2 = new Proyecto
        {
            Nombre = "Ideas personales",
            Descripcion = "Backlog de ideas y pendientes",
            Color = "#10b981",
            PropietarioId = user2.Id
        };

        context.Proyectos.AddRange(proyecto1, proyecto2);
        context.SaveChanges();

        context.MiembrosProyecto.AddRange(
            new MiembroProyecto { ProyectoId = proyecto1.Id, UsuarioId = user1.Id, Rol = RolProyecto.Owner },
            new MiembroProyecto { ProyectoId = proyecto1.Id, UsuarioId = user2.Id, Rol = RolProyecto.Editor },
            new MiembroProyecto { ProyectoId = proyecto2.Id, UsuarioId = user2.Id, Rol = RolProyecto.Owner }
        );

        context.Tareas.AddRange(
            new Tarea { Titulo = "Diseñar modelo de datos", Estado = EstadoTarea.Done, Prioridad = PrioridadTarea.Alta, ProyectoId = proyecto1.Id, AsignadoAId = user1.Id },
            new Tarea { Titulo = "Implementar autenticación JWT", Estado = EstadoTarea.InProgress, Prioridad = PrioridadTarea.Alta, ProyectoId = proyecto1.Id, AsignadoAId = user1.Id },
            new Tarea { Titulo = "Crear tablero Kanban en React", Estado = EstadoTarea.ToDo, Prioridad = PrioridadTarea.Media, ProyectoId = proyecto1.Id, AsignadoAId = user2.Id },
            new Tarea { Titulo = "Configurar despliegue en la nube", Estado = EstadoTarea.ToDo, Prioridad = PrioridadTarea.Media, ProyectoId = proyecto1.Id },
            new Tarea { Titulo = "Escribir README", Estado = EstadoTarea.ToDo, Prioridad = PrioridadTarea.Baja, ProyectoId = proyecto2.Id, AsignadoAId = user2.Id }
        );

        context.SaveChanges();
    }
}
