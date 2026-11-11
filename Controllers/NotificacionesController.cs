using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs.Notificaciones;
using GestorTareas.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Controllers;

// Cada usuario solo ve y marca las suyas: todas las consultas filtran por el usuario del token.
[ApiController]
[Authorize]
[Route("api/v1/notificaciones")]
public class NotificacionesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificacionesDto>> Listar()
    {
        var yo = User.GetUsuarioId();
        var propias = db.Notificaciones.Where(n => n.UsuarioId == yo);
        return new NotificacionesDto
        {
            NoLeidas = await propias.CountAsync(n => !n.Leida),
            Items = await propias.AsNoTracking().OrderByDescending(n => n.Fecha).Take(30)
                .Select(n => new NotificacionDto { Id = n.Id, Mensaje = n.Mensaje, ProyectoId = n.ProyectoId, TareaId = n.TareaId, Leida = n.Leida, Fecha = n.Fecha })
                .ToListAsync()
        };
    }

    [HttpPost("{id}/leida")]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        var yo = User.GetUsuarioId();
        var filas = await db.Notificaciones.Where(n => n.Id == id && n.UsuarioId == yo)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
        return filas == 0 ? NotFound() : NoContent();
    }

    [HttpPost("leer-todas")]
    public async Task<IActionResult> LeerTodas()
    {
        var yo = User.GetUsuarioId();
        await db.Notificaciones.Where(n => n.UsuarioId == yo && !n.Leida)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
        return NoContent();
    }
}
