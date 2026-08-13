using GestorTareas.Api.DTOs.Proyectos;
using GestorTareas.Api.Models;

namespace GestorTareas.Api.Services;

public interface IProyectoService
{
    Task<List<ProyectoDto>> ListarPorUsuarioAsync(int usuarioId);
    Task<ProyectoDto> ObtenerPorIdAsync(int proyectoId, int usuarioId);
    Task<ProyectoDto> CrearAsync(CrearProyectoDto dto, int usuarioId);
    Task<ProyectoDto> ActualizarAsync(int proyectoId, ActualizarProyectoDto dto, int usuarioId);
    Task EliminarAsync(int proyectoId, int usuarioId);
    Task<List<MiembroDto>> ListarMiembrosAsync(int proyectoId, int usuarioId);
    Task<MiembroDto> InvitarMiembroAsync(int proyectoId, InvitarMiembroDto dto, int usuarioId);
    Task RemoverMiembroAsync(int proyectoId, int miembroId, int usuarioId);
    Task<MiembroDto> CambiarRolMiembroAsync(int proyectoId, int miembroId, CambiarRolMiembroDto dto, int usuarioId);

    // Usado por TareaService para validar acceso sin duplicar la lógica de roles.
    Task<RolProyecto> ObtenerRolEnProyectoAsync(int proyectoId, int usuarioId);
}
