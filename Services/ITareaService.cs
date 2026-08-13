using GestorTareas.Api.DTOs.Common;
using GestorTareas.Api.DTOs.Tareas;

namespace GestorTareas.Api.Services;

public interface ITareaService
{
    Task<PagedResultDto<TareaDto>> ListarPorProyectoAsync(int proyectoId, TareaFiltroDto filtro, int usuarioId);
    Task<TareaDetalleDto> ObtenerPorIdAsync(int tareaId, int usuarioId);
    Task<TareaDto> CrearAsync(int proyectoId, CrearTareaDto dto, int usuarioId);
    Task<TareaDto> ActualizarAsync(int tareaId, ActualizarTareaDto dto, int usuarioId);
    Task<TareaDto> CambiarEstadoAsync(int tareaId, CambiarEstadoTareaDto dto, int usuarioId);
    Task EliminarAsync(int tareaId, int usuarioId);
}
