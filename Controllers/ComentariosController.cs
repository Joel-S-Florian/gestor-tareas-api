using GestorTareas.Api.DTOs.Comentarios;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Authorize]
public class ComentariosController : ControllerBase
{
    private readonly IComentarioService _comentarioService;

    public ComentariosController(IComentarioService comentarioService)
    {
        _comentarioService = comentarioService;
    }

    [HttpPost("api/v1/tareas/{id}/comentarios")]
    public async Task<ActionResult<ComentarioDto>> Agregar(int id, CrearComentarioDto dto)
    {
        var comentario = await _comentarioService.AgregarAsync(id, dto, User.GetUsuarioId());
        return Ok(comentario);
    }

    [HttpDelete("api/v1/comentarios/{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _comentarioService.EliminarAsync(id, User.GetUsuarioId());
        return NoContent();
    }
}
