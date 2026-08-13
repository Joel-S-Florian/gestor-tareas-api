using GestorTareas.Api.Extensions;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Authorize]
public class AdjuntosController : ControllerBase
{
    private readonly IAdjuntoService _adjuntoService;

    public AdjuntosController(IAdjuntoService adjuntoService)
    {
        _adjuntoService = adjuntoService;
    }

    [HttpPost("api/v1/tareas/{id}/adjuntos")]
    [RequestSizeLimit(6_000_000)] // 5 MB + margen para el resto del multipart
    public async Task<IActionResult> Subir(int id, IFormFile archivo)
    {
        var adjunto = await _adjuntoService.SubirAsync(id, archivo, User.GetUsuarioId());
        return Ok(adjunto);
    }

    [HttpGet("api/v1/adjuntos/{id}")]
    public async Task<IActionResult> Descargar(int id)
    {
        var (contenido, nombreArchivo, contentType) = await _adjuntoService.DescargarAsync(id, User.GetUsuarioId());
        return File(contenido, contentType, nombreArchivo);
    }

    [HttpDelete("api/v1/adjuntos/{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _adjuntoService.EliminarAsync(id, User.GetUsuarioId());
        return NoContent();
    }
}
