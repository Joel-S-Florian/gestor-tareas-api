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
        Actividades.Registrar(_db, tarea.ProyectoId, tareaId, usuarioId, $"comentó en «{tarea.Titulo}»");
        await AvisarMencionesAsync(tarea, usuario, dto.Contenido);
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

    // @handle = parte del email antes de la arroba. Solo se avisa a miembros del proyecto (no se filtra
    // quién existe en el sistema) y nunca al autor.
    private async Task AvisarMencionesAsync(Tarea tarea, Usuario autor, string contenido)
    {
        var handles = MencionRegex.Matches(contenido).Select(m => m.Groups[1].Value.TrimEnd('.', '-', '_').ToLowerInvariant())
            .Where(h => h.Length > 0).Distinct().ToList();
        if (handles.Count == 0) return;

        var miembros = await _db.MiembrosProyecto.Where(m => m.ProyectoId == tarea.ProyectoId && m.UsuarioId != autor.Id)
            .Select(m => new { m.UsuarioId, m.Usuario.Email }).ToListAsync();

        foreach (var m in miembros.Where(m => handles.Contains(m.Email.Split('@')[0].ToLowerInvariant())))
            _db.Notificaciones.Add(new Notificacion
            {
                UsuarioId = m.UsuarioId,
                Mensaje = $"{autor.Nombre} te mencionó en «{tarea.Titulo}».",
                ProyectoId = tarea.ProyectoId,
                TareaId = tarea.Id
            });
    }

    private static readonly System.Text.RegularExpressions.Regex MencionRegex = new(@"(?<![\w.])@([A-Za-z0-9._-]{1,60})");

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
