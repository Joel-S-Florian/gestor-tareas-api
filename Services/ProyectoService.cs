using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs.Proyectos;
using GestorTareas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Services;

public class ProyectoService : IProyectoService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ProyectoService> _logger;

    public ProyectoService(ApplicationDbContext db, ILogger<ProyectoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RolProyecto> ObtenerRolEnProyectoAsync(int proyectoId, int usuarioId)
    {
        var membresia = await _db.MiembrosProyecto
            .FirstOrDefaultAsync(m => m.ProyectoId == proyectoId && m.UsuarioId == usuarioId);

        if (membresia == null)
            throw new ForbiddenException("No tienes acceso a este proyecto.");

        return membresia.Rol;
    }

    public async Task<List<ProyectoDto>> ListarPorUsuarioAsync(int usuarioId)
    {
        var proyectos = await _db.MiembrosProyecto
            .Where(m => m.UsuarioId == usuarioId)
            .Select(m => new ProyectoDto
            {
                Id = m.Proyecto.Id,
                Nombre = m.Proyecto.Nombre,
                Descripcion = m.Proyecto.Descripcion,
                Color = m.Proyecto.Color,
                FechaCreacion = m.Proyecto.FechaCreacion,
                PropietarioId = m.Proyecto.PropietarioId,
                PropietarioNombre = m.Proyecto.Propietario.Nombre,
                MiRol = m.Rol,
                TotalTareas = m.Proyecto.Tareas.Count,
                TotalMiembros = m.Proyecto.Miembros.Count
            })
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();

        return proyectos;
    }

    public async Task<ProyectoDto> ObtenerPorIdAsync(int proyectoId, int usuarioId)
    {
        var rol = await ObtenerRolEnProyectoAsync(proyectoId, usuarioId);

        var proyecto = await _db.Proyectos
            .Include(p => p.Propietario)
            .Include(p => p.Tareas)
            .Include(p => p.Miembros)
            .FirstOrDefaultAsync(p => p.Id == proyectoId);

        if (proyecto == null) throw new NotFoundException("Proyecto no encontrado.");

        return MapearDto(proyecto, rol);
    }

    public async Task<ProyectoDto> CrearAsync(CrearProyectoDto dto, int usuarioId)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId)
            ?? throw new NotFoundException("Usuario no encontrado.");

        var proyecto = new Proyecto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Color = dto.Color,
            PropietarioId = usuarioId
        };

        _db.Proyectos.Add(proyecto);
        await _db.SaveChangesAsync();

        // El creador queda automáticamente como Owner.
        _db.MiembrosProyecto.Add(new MiembroProyecto
        {
            ProyectoId = proyecto.Id,
            UsuarioId = usuarioId,
            Rol = RolProyecto.Owner
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Proyecto {ProyectoId} ({Nombre}) creado por {UsuarioId}", proyecto.Id, proyecto.Nombre, usuarioId);

        proyecto.Propietario = usuario;
        return MapearDto(proyecto, RolProyecto.Owner);
    }

    public async Task<ProyectoDto> ActualizarAsync(int proyectoId, ActualizarProyectoDto dto, int usuarioId)
    {
        var rol = await ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        RequerirRolMinimo(rol, RolProyecto.Editor); // Owner o Editor

        var proyecto = await _db.Proyectos
            .Include(p => p.Propietario)
            .Include(p => p.Tareas)
            .Include(p => p.Miembros)
            .FirstOrDefaultAsync(p => p.Id == proyectoId)
            ?? throw new NotFoundException("Proyecto no encontrado.");

        proyecto.Nombre = dto.Nombre;
        proyecto.Descripcion = dto.Descripcion;
        proyecto.Color = dto.Color;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Proyecto {ProyectoId} actualizado por {UsuarioId}", proyectoId, usuarioId);
        return MapearDto(proyecto, rol);
    }

    public async Task EliminarAsync(int proyectoId, int usuarioId)
    {
        var rol = await ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        if (rol != RolProyecto.Owner)
            throw new ForbiddenException("Solo el propietario puede eliminar el proyecto.");

        var proyecto = await _db.Proyectos
            .Include(p => p.Tareas)
            .FirstOrDefaultAsync(p => p.Id == proyectoId)
            ?? throw new NotFoundException("Proyecto no encontrado.");

        if (proyecto.Tareas.Any())
            throw new ConflictException("No se puede eliminar un proyecto que tiene tareas. Elimínalas o transfiérelas primero.");

        _db.Proyectos.Remove(proyecto);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Proyecto {ProyectoId} eliminado por {UsuarioId}", proyectoId, usuarioId);
    }

    public async Task<List<MiembroDto>> ListarMiembrosAsync(int proyectoId, int usuarioId)
    {
        await ObtenerRolEnProyectoAsync(proyectoId, usuarioId); // cualquier miembro puede ver la lista

        return await _db.MiembrosProyecto
            .Where(m => m.ProyectoId == proyectoId)
            .Select(m => new MiembroDto
            {
                UsuarioId = m.UsuarioId,
                Nombre = m.Usuario.Nombre,
                Email = m.Usuario.Email,
                Rol = m.Rol,
                FechaIngreso = m.FechaIngreso
            })
            .ToListAsync();
    }

    public async Task<MiembroDto> InvitarMiembroAsync(int proyectoId, InvitarMiembroDto dto, int usuarioId)
    {
        var rolSolicitante = await ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        if (rolSolicitante != RolProyecto.Owner)
            throw new ForbiddenException("Solo el propietario puede invitar miembros.");

        var usuarioInvitado = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email)
            ?? throw new NotFoundException("No existe un usuario registrado con ese email.");

        var yaEsMiembro = await _db.MiembrosProyecto
            .AnyAsync(m => m.ProyectoId == proyectoId && m.UsuarioId == usuarioInvitado.Id);
        if (yaEsMiembro)
            throw new ConflictException("Ese usuario ya es miembro del proyecto.");

        var membresia = new MiembroProyecto
        {
            ProyectoId = proyectoId,
            UsuarioId = usuarioInvitado.Id,
            Rol = dto.Rol
        };

        _db.MiembrosProyecto.Add(membresia);
        await _db.SaveChangesAsync();

        _logger.LogInformation("{UsuarioId} invitó a {Email} al proyecto {ProyectoId} con rol {Rol}",
            usuarioId, usuarioInvitado.Email, proyectoId, dto.Rol);

        return new MiembroDto
        {
            UsuarioId = usuarioInvitado.Id,
            Nombre = usuarioInvitado.Nombre,
            Email = usuarioInvitado.Email,
            Rol = membresia.Rol,
            FechaIngreso = membresia.FechaIngreso
        };
    }

    public async Task RemoverMiembroAsync(int proyectoId, int miembroId, int usuarioId)
    {
        var rolSolicitante = await ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        if (rolSolicitante != RolProyecto.Owner)
            throw new ForbiddenException("Solo el propietario puede remover miembros.");

        var proyecto = await _db.Proyectos.FindAsync(proyectoId)
            ?? throw new NotFoundException("Proyecto no encontrado.");

        if (proyecto.PropietarioId == miembroId)
            throw new ConflictException("No puedes remover al propietario del proyecto.");

        var membresia = await _db.MiembrosProyecto
            .FirstOrDefaultAsync(m => m.ProyectoId == proyectoId && m.UsuarioId == miembroId)
            ?? throw new NotFoundException("Ese usuario no es miembro del proyecto.");

        _db.MiembrosProyecto.Remove(membresia);
        await _db.SaveChangesAsync();
        _logger.LogInformation("{UsuarioId} removió a {MiembroId} del proyecto {ProyectoId}", usuarioId, miembroId, proyectoId);
    }

    public async Task<MiembroDto> CambiarRolMiembroAsync(int proyectoId, int miembroId, CambiarRolMiembroDto dto, int usuarioId)
    {
        var rolSolicitante = await ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        if (rolSolicitante != RolProyecto.Owner)
            throw new ForbiddenException("Solo el propietario puede cambiar roles.");

        var proyecto = await _db.Proyectos.FindAsync(proyectoId)
            ?? throw new NotFoundException("Proyecto no encontrado.");

        if (proyecto.PropietarioId == miembroId)
            throw new ConflictException("No puedes cambiar el rol del propietario del proyecto.");

        var membresia = await _db.MiembrosProyecto
            .Include(m => m.Usuario)
            .FirstOrDefaultAsync(m => m.ProyectoId == proyectoId && m.UsuarioId == miembroId)
            ?? throw new NotFoundException("Ese usuario no es miembro del proyecto.");

        membresia.Rol = dto.Rol;
        await _db.SaveChangesAsync();

        _logger.LogInformation("{UsuarioId} cambió el rol de {MiembroId} a {Rol} en el proyecto {ProyectoId}",
            usuarioId, miembroId, dto.Rol, proyectoId);

        return new MiembroDto
        {
            UsuarioId = membresia.UsuarioId,
            Nombre = membresia.Usuario.Nombre,
            Email = membresia.Usuario.Email,
            Rol = membresia.Rol,
            FechaIngreso = membresia.FechaIngreso
        };
    }

    // Owner > Editor > Viewer. Lanza ForbiddenException si el rol actual no alcanza el mínimo requerido.
    private static void RequerirRolMinimo(RolProyecto rolActual, RolProyecto rolMinimo)
    {
        var jerarquia = new Dictionary<RolProyecto, int>
        {
            [RolProyecto.Viewer] = 0,
            [RolProyecto.Editor] = 1,
            [RolProyecto.Owner] = 2
        };

        if (jerarquia[rolActual] < jerarquia[rolMinimo])
            throw new ForbiddenException("No tienes permisos suficientes para esta acción.");
    }

    private static ProyectoDto MapearDto(Proyecto p, RolProyecto miRol) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Color = p.Color,
        FechaCreacion = p.FechaCreacion,
        PropietarioId = p.PropietarioId,
        PropietarioNombre = p.Propietario.Nombre,
        MiRol = miRol,
        TotalTareas = p.Tareas.Count,
        TotalMiembros = p.Miembros.Count
    };
}
