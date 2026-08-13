using GestorTareas.Api.DTOs.Proyectos;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Route("api/v1/proyectos")]
[Authorize]
public class ProyectosController : ControllerBase
{
    private readonly IProyectoService _proyectoService;

    public ProyectosController(IProyectoService proyectoService)
    {
        _proyectoService = proyectoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProyectoDto>>> Listar()
    {
        var proyectos = await _proyectoService.ListarPorUsuarioAsync(User.GetUsuarioId());
        return Ok(proyectos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProyectoDto>> ObtenerPorId(int id)
    {
        var proyecto = await _proyectoService.ObtenerPorIdAsync(id, User.GetUsuarioId());
        return Ok(proyecto);
    }

    [HttpPost]
    public async Task<ActionResult<ProyectoDto>> Crear(CrearProyectoDto dto)
    {
        var proyecto = await _proyectoService.CrearAsync(dto, User.GetUsuarioId());
        return CreatedAtAction(nameof(ObtenerPorId), new { id = proyecto.Id }, proyecto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProyectoDto>> Actualizar(int id, ActualizarProyectoDto dto)
    {
        var proyecto = await _proyectoService.ActualizarAsync(id, dto, User.GetUsuarioId());
        return Ok(proyecto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _proyectoService.EliminarAsync(id, User.GetUsuarioId());
        return NoContent();
    }

    [HttpGet("{id}/miembros")]
    public async Task<ActionResult<List<MiembroDto>>> ListarMiembros(int id)
    {
        var miembros = await _proyectoService.ListarMiembrosAsync(id, User.GetUsuarioId());
        return Ok(miembros);
    }

    [HttpPost("{id}/miembros")]
    public async Task<ActionResult<MiembroDto>> InvitarMiembro(int id, InvitarMiembroDto dto)
    {
        var miembro = await _proyectoService.InvitarMiembroAsync(id, dto, User.GetUsuarioId());
        return Ok(miembro);
    }

    [HttpDelete("{id}/miembros/{userId}")]
    public async Task<IActionResult> RemoverMiembro(int id, int userId)
    {
        await _proyectoService.RemoverMiembroAsync(id, userId, User.GetUsuarioId());
        return NoContent();
    }

    [HttpPut("{id}/miembros/{userId}/rol")]
    public async Task<ActionResult<MiembroDto>> CambiarRolMiembro(int id, int userId, CambiarRolMiembroDto dto)
    {
        var miembro = await _proyectoService.CambiarRolMiembroAsync(id, userId, dto, User.GetUsuarioId());
        return Ok(miembro);
    }
}
