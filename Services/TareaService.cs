using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs.Adjuntos;
using GestorTareas.Api.DTOs.Comentarios;
using GestorTareas.Api.DTOs.Common;
using GestorTareas.Api.DTOs.Tareas;
using GestorTareas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Services;

public class TareaService : ITareaService
{
    private readonly ApplicationDbContext _db;
    private readonly IProyectoService _proyectoService;
    private readonly ILogger<TareaService> _logger;

    public TareaService(ApplicationDbContext db, IProyectoService proyectoService, ILogger<TareaService> logger)
    {
        _db = db;
        _proyectoService = proyectoService;
        _logger = logger;
    }

    public async Task<PagedResultDto<TareaDto>> ListarPorProyectoAsync(int proyectoId, TareaFiltroDto filtro, int usuarioId)
    {
        await _proyectoService.ObtenerRolEnProyectoAsync(proyectoId, usuarioId); // valida acceso, cualquier rol puede leer

        var query = _db.Tareas.Where(t => t.ProyectoId == proyectoId);

        if (filtro.Estado.HasValue)
            query = query.Where(t => t.Estado == filtro.Estado.Value);

        if (filtro.Prioridad.HasValue)
            query = query.Where(t => t.Prioridad == filtro.Prioridad.Value);

        if (filtro.AsignadoAId.HasValue)
            query = query.Where(t => t.AsignadoAId == filtro.AsignadoAId.Value);

        var totalItems = await query.CountAsync();

        var entidades = await query
            .Include(t => t.AsignadoA)
            .Include(t => t.Comentarios)
            .Include(t => t.Adjuntos)
            .OrderBy(t => t.Estado)
            .ThenByDescending(t => t.Prioridad)
            .ThenBy(t => t.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        var items = entidades.Select(MapearDto).ToList();

        return new PagedResultDto<TareaDto>
        {
            Items = items,
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalItems = totalItems
        };
    }

    public async Task<TareaDetalleDto> ObtenerPorIdAsync(int tareaId, int usuarioId)
    {
        var tarea = await _db.Tareas
            .Include(t => t.AsignadoA)
            .Include(t => t.Comentarios).ThenInclude(c => c.Usuario)
            .Include(t => t.Adjuntos)
            .FirstOrDefaultAsync(t => t.Id == tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");

        await _proyectoService.ObtenerRolEnProyectoAsync(tarea.ProyectoId, usuarioId);

        return MapearDetalleDto(tarea);
    }

    public async Task<TareaDto> CrearAsync(int proyectoId, CrearTareaDto dto, int usuarioId)
    {
        var rol = await _proyectoService.ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        RequerirEdicion(rol);

        if (dto.AsignadoAId.HasValue)
            await ValidarAsignadoEsMiembroAsync(proyectoId, dto.AsignadoAId.Value);

        var tarea = new Tarea
        {
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            Prioridad = dto.Prioridad,
            FechaVencimiento = dto.FechaVencimiento,
            AsignadoAId = dto.AsignadoAId,
            ProyectoId = proyectoId,
            Estado = EstadoTarea.ToDo
        };

        _db.Tareas.Add(tarea);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Tarea {TareaId} ({Titulo}) creada en el proyecto {ProyectoId} por {UsuarioId}",
            tarea.Id, tarea.Titulo, proyectoId, usuarioId);

        return await ObtenerPorIdAsync(tarea.Id, usuarioId);
    }

    public async Task<TareaDto> ActualizarAsync(int tareaId, ActualizarTareaDto dto, int usuarioId)
    {
        var tarea = await _db.Tareas.FindAsync(tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");

        var rol = await _proyectoService.ObtenerRolEnProyectoAsync(tarea.ProyectoId, usuarioId);
        RequerirEdicion(rol);

        if (dto.AsignadoAId.HasValue)
            await ValidarAsignadoEsMiembroAsync(tarea.ProyectoId, dto.AsignadoAId.Value);

        tarea.Titulo = dto.Titulo;
        tarea.Descripcion = dto.Descripcion;
        tarea.Prioridad = dto.Prioridad;
        tarea.FechaVencimiento = dto.FechaVencimiento;
        tarea.AsignadoAId = dto.AsignadoAId;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Tarea {TareaId} actualizada por {UsuarioId}", tarea.Id, usuarioId);
        return await ObtenerPorIdAsync(tarea.Id, usuarioId);
    }

    public async Task<TareaDto> CambiarEstadoAsync(int tareaId, CambiarEstadoTareaDto dto, int usuarioId)
    {
        var tarea = await _db.Tareas.FindAsync(tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");

        var rol = await _proyectoService.ObtenerRolEnProyectoAsync(tarea.ProyectoId, usuarioId);
        RequerirEdicion(rol);

        tarea.Estado = dto.Estado;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Tarea {TareaId} cambió a estado {Estado} por {UsuarioId}", tarea.Id, dto.Estado, usuarioId);

        return await ObtenerPorIdAsync(tarea.Id, usuarioId);
    }

    public async Task EliminarAsync(int tareaId, int usuarioId)
    {
        var tarea = await _db.Tareas.FindAsync(tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");

        var rol = await _proyectoService.ObtenerRolEnProyectoAsync(tarea.ProyectoId, usuarioId);
        RequerirEdicion(rol);

        _db.Tareas.Remove(tarea);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Tarea {TareaId} eliminada por {UsuarioId}", tareaId, usuarioId);
    }

    private async Task ValidarAsignadoEsMiembroAsync(int proyectoId, int asignadoAId)
    {
        var esMiembro = await _db.MiembrosProyecto
            .AnyAsync(m => m.ProyectoId == proyectoId && m.UsuarioId == asignadoAId);

        if (!esMiembro)
            throw new ConflictException("Solo puedes asignar la tarea a un miembro del proyecto.");
    }

    // Viewer solo puede leer; Editor y Owner pueden crear/modificar/eliminar tareas.
    private static void RequerirEdicion(RolProyecto rol)
    {
        if (rol == RolProyecto.Viewer)
            throw new ForbiddenException("Tu rol en este proyecto es de solo lectura.");
    }

    private static TareaDto MapearDto(Tarea t) => new()
    {
        Id = t.Id,
        Titulo = t.Titulo,
        Descripcion = t.Descripcion,
        Estado = t.Estado,
        Prioridad = t.Prioridad,
        FechaVencimiento = t.FechaVencimiento,
        ProyectoId = t.ProyectoId,
        AsignadoAId = t.AsignadoAId,
        AsignadoANombre = t.AsignadoA?.Nombre,
        TotalComentarios = t.Comentarios?.Count ?? 0,
        TotalAdjuntos = t.Adjuntos?.Count ?? 0
    };

    private static TareaDetalleDto MapearDetalleDto(Tarea t) => new()
    {
        Id = t.Id,
        Titulo = t.Titulo,
        Descripcion = t.Descripcion,
        Estado = t.Estado,
        Prioridad = t.Prioridad,
        FechaVencimiento = t.FechaVencimiento,
        ProyectoId = t.ProyectoId,
        AsignadoAId = t.AsignadoAId,
        AsignadoANombre = t.AsignadoA?.Nombre,
        TotalComentarios = t.Comentarios?.Count ?? 0,
        TotalAdjuntos = t.Adjuntos?.Count ?? 0,
        Comentarios = (t.Comentarios ?? new List<Comentario>())
            .OrderBy(c => c.FechaCreacion)
            .Select(c => new ComentarioDto
            {
                Id = c.Id,
                TareaId = c.TareaId,
                UsuarioId = c.UsuarioId,
                UsuarioNombre = c.Usuario?.Nombre ?? string.Empty,
                Contenido = c.Contenido,
                FechaCreacion = c.FechaCreacion
            }).ToList(),
        Adjuntos = (t.Adjuntos ?? new List<Adjunto>())
            .OrderByDescending(a => a.FechaSubida)
            .Select(a => new AdjuntoDto
            {
                Id = a.Id,
                TareaId = a.TareaId,
                NombreArchivo = a.NombreArchivo,
                TamanoBytes = a.TamanoBytes,
                FechaSubida = a.FechaSubida
            }).ToList()
    };
}
