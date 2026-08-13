using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs.Comentarios;
using GestorTareas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Services;

public class ComentarioService : IComentarioService
{
    private readonly ApplicationDbContext _db;
    private readonly IProyectoService _proyectoService;
    private readonly ILogger<ComentarioService> _logger;

    public ComentarioService(ApplicationDbContext db, IProyectoService proyectoService, ILogger<ComentarioService> logger)
    {
        _db = db;
        _proyectoService = proyectoService;
        _logger = logger;
    }

    public async Task<ComentarioDto> AgregarAsync(int tareaId, CrearComentarioDto dto, int usuarioId)
    {
        var tarea = await _db.Tareas.FindAsync(tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");

        // Cualquier miembro del proyecto (incluido Viewer) puede comentar; solo
        // crear/editar/eliminar tareas y proyectos requiere Editor u Owner.
        await _proyectoService.ObtenerRolEnProyectoAsync(tarea.ProyectoId, usuarioId);

        var usuario = await _db.Usuarios.FindAsync(usuarioId)
            ?? throw new NotFoundException("Usuario no encontrado.");

        var comentario = new Comentario
        {
            TareaId = tareaId,
            UsuarioId = usuarioId,
            Contenido = dto.Contenido
        };

        _db.Comentarios.Add(comentario);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Comentario {ComentarioId} agregado a la tarea {TareaId} por {UsuarioId}",
            comentario.Id, tareaId, usuarioId);

        return new ComentarioDto
        {
            Id = comentario.Id,
            TareaId = comentario.TareaId,
            UsuarioId = usuario.Id,
            UsuarioNombre = usuario.Nombre,
            Contenido = comentario.Contenido,
            FechaCreacion = comentario.FechaCreacion
        };
    }

    public async Task EliminarAsync(int comentarioId, int usuarioId)
    {
        var comentario = await _db.Comentarios.FirstOrDefaultAsync(c => c.Id == comentarioId)
            ?? throw new NotFoundException("Comentario no encontrado.");

        if (comentario.UsuarioId != usuarioId)
            throw new ForbiddenException("Solo puedes eliminar tus propios comentarios.");

        _db.Comentarios.Remove(comentario);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Comentario {ComentarioId} eliminado por {UsuarioId}", comentarioId, usuarioId);
    }
}
