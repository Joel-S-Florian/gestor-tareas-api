using GestorTareas.Api.DTOs.Tareas;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Authorize]
public class TareasController : ControllerBase
{
    private readonly ITareaService _tareaService;

    public TareasController(ITareaService tareaService)
    {
        _tareaService = tareaService;
    }

    // GET /api/v1/proyectos/{projectId}/tareas?estado=&prioridad=&asignadoAId=&page=&pageSize=
    [HttpGet("api/v1/proyectos/{projectId}/tareas")]
    public async Task<IActionResult> ListarPorProyecto(int projectId, [FromQuery] TareaFiltroDto filtro)
    {
        var resultado = await _tareaService.ListarPorProyectoAsync(projectId, filtro, User.GetUsuarioId());
        return Ok(resultado);
    }

    // POST /api/v1/proyectos/{projectId}/tareas
    [HttpPost("api/v1/proyectos/{projectId}/tareas")]
    public async Task<IActionResult> Crear(int projectId, CrearTareaDto dto)
    {
        var tarea = await _tareaService.CrearAsync(projectId, dto, User.GetUsuarioId());
        return CreatedAtAction(nameof(ObtenerPorId), new { id = tarea.Id }, tarea);
    }

    // GET /api/v1/tareas/{id}
    [HttpGet("api/v1/tareas/{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var tarea = await _tareaService.ObtenerPorIdAsync(id, User.GetUsuarioId());
        return Ok(tarea);
    }

    // PUT /api/v1/tareas/{id}
    [HttpPut("api/v1/tareas/{id}")]
    public async Task<IActionResult> Actualizar(int id, ActualizarTareaDto dto)
    {
        var tarea = await _tareaService.ActualizarAsync(id, dto, User.GetUsuarioId());
        return Ok(tarea);
    }

    // PATCH /api/v1/tareas/{id}/estado — pensado para el drag-and-drop del Kanban
    [HttpPatch("api/v1/tareas/{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoTareaDto dto)
    {
        var tarea = await _tareaService.CambiarEstadoAsync(id, dto, User.GetUsuarioId());
        return Ok(tarea);
    }

    // DELETE /api/v1/tareas/{id}
    [HttpDelete("api/v1/tareas/{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _tareaService.EliminarAsync(id, User.GetUsuarioId());
        return NoContent();
    }
}
