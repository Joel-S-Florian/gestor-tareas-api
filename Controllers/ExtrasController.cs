using GestorTareas.Api.DTOs.Extras;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public class ExtrasController(ExtrasService extras) : ControllerBase
{
    private int Yo => User.GetUsuarioId();

    // Actividad
    [HttpGet("proyectos/{proyectoId}/actividad")]
    public async Task<ActionResult<List<ActividadDto>>> ActividadProyecto(int proyectoId) => await extras.ActividadDelProyectoAsync(proyectoId, Yo);

    [HttpGet("tareas/{tareaId}/actividad")]
    public async Task<ActionResult<List<ActividadDto>>> ActividadTarea(int tareaId) => await extras.ActividadDeLaTareaAsync(tareaId, Yo);

    // Checklist
    [HttpPost("tareas/{tareaId}/checklist")]
    public async Task<ActionResult<ItemChecklistDto>> AgregarItem(int tareaId, CrearItemDto dto) => await extras.AgregarItemAsync(tareaId, dto, Yo);

    [HttpPatch("checklist/{itemId}")]
    public async Task<ActionResult<ItemChecklistDto>> ActualizarItem(int itemId, ActualizarItemDto dto) => await extras.ActualizarItemAsync(itemId, dto, Yo);

    [HttpDelete("checklist/{itemId}")]
    public async Task<IActionResult> EliminarItem(int itemId)
    {
        await extras.EliminarItemAsync(itemId, Yo);
        return NoContent();
    }

    // Etiquetas
    [HttpGet("proyectos/{proyectoId}/etiquetas")]
    public async Task<ActionResult<List<EtiquetaDto>>> Etiquetas(int proyectoId) => await extras.ListarEtiquetasAsync(proyectoId, Yo);

    [HttpPost("proyectos/{proyectoId}/etiquetas")]
    public async Task<ActionResult<EtiquetaDto>> CrearEtiqueta(int proyectoId, CrearEtiquetaDto dto)
    {
        var creada = await extras.CrearEtiquetaAsync(proyectoId, dto, Yo);
        return CreatedAtAction(nameof(Etiquetas), new { proyectoId }, creada);
    }

    [HttpDelete("etiquetas/{etiquetaId}")]
    public async Task<IActionResult> EliminarEtiqueta(int etiquetaId)
    {
        await extras.EliminarEtiquetaAsync(etiquetaId, Yo);
        return NoContent();
    }

    [HttpPut("tareas/{tareaId}/etiquetas")]
    public async Task<ActionResult<List<EtiquetaDto>>> AsignarEtiquetas(int tareaId, AsignarEtiquetasDto dto) => await extras.AsignarEtiquetasAsync(tareaId, dto, Yo);
}
