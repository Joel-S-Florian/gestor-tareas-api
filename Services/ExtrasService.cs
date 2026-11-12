using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs.Extras;
using GestorTareas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Services;

// Actividad, checklist y etiquetas. Lectura: cualquier miembro. Escritura: Owner y Editor.
public class ExtrasService(ApplicationDbContext db, IProyectoService proyectos)
{
    private const int MaxItemsPorTarea = 50;
    private const int MaxEtiquetasPorProyecto = 30;
    private const int MaxEtiquetasPorTarea = 10;

    private async Task RequerirEdicionAsync(int proyectoId, int usuarioId)
    {
        if (await proyectos.ObtenerRolEnProyectoAsync(proyectoId, usuarioId) == RolProyecto.Viewer)
            throw new ForbiddenException("Tu rol en este proyecto es de solo lectura.");
    }

    // ---------- Actividad ----------
    public async Task<List<ActividadDto>> ActividadDelProyectoAsync(int proyectoId, int usuarioId, int? tareaId = null)
    {
        await proyectos.ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        return await db.Actividades.AsNoTracking()
            .Where(a => a.ProyectoId == proyectoId && (tareaId == null || a.TareaId == tareaId))
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
            .Take(100)
            .Select(a => new ActividadDto { Id = a.Id, UsuarioNombre = a.Usuario.Nombre, Mensaje = a.Mensaje, TareaId = a.TareaId, Fecha = a.Fecha })
            .ToListAsync();
    }

    public async Task<List<ActividadDto>> ActividadDeLaTareaAsync(int tareaId, int usuarioId)
    {
        var tarea = await db.Tareas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");
        return await ActividadDelProyectoAsync(tarea.ProyectoId, usuarioId, tareaId);
    }

    // ---------- Checklist ----------
    private async Task<Tarea> TareaEditableAsync(int tareaId, int usuarioId)
    {
        var tarea = await db.Tareas.FindAsync(tareaId) ?? throw new NotFoundException("Tarea no encontrada.");
        await RequerirEdicionAsync(tarea.ProyectoId, usuarioId);
        return tarea;
    }

    public async Task<ItemChecklistDto> AgregarItemAsync(int tareaId, CrearItemDto dto, int usuarioId)
    {
        var tarea = await TareaEditableAsync(tareaId, usuarioId);
        var cantidad = await db.ItemsChecklist.CountAsync(i => i.TareaId == tareaId);
        if (cantidad >= MaxItemsPorTarea) throw new ConflictException($"Una tarea admite hasta {MaxItemsPorTarea} ítems.");

        var item = new ItemChecklist { TareaId = tarea.Id, Texto = dto.Texto.Trim(), Orden = cantidad };
        db.ItemsChecklist.Add(item);
        await db.SaveChangesAsync();
        return new ItemChecklistDto { Id = item.Id, Texto = item.Texto, Hecho = item.Hecho };
    }

    private async Task<(ItemChecklist item, Tarea tarea)> ItemEditableAsync(int itemId, int usuarioId)
    {
        var item = await db.ItemsChecklist.Include(i => i.Tarea).FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new NotFoundException("Ítem no encontrado.");
        await RequerirEdicionAsync(item.Tarea.ProyectoId, usuarioId);
        return (item, item.Tarea);
    }

    public async Task<ItemChecklistDto> ActualizarItemAsync(int itemId, ActualizarItemDto dto, int usuarioId)
    {
        var (item, tarea) = await ItemEditableAsync(itemId, usuarioId);
        if (dto.Texto != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Texto)) throw new ConflictException("El texto no puede estar vacío.");
            item.Texto = dto.Texto.Trim();
        }
        if (dto.Hecho.HasValue && dto.Hecho.Value != item.Hecho)
        {
            item.Hecho = dto.Hecho.Value;
            // Solo se registra cuando la lista queda completa: un toggle por ítem llenaría la bitácora de ruido.
            if (item.Hecho && !await db.ItemsChecklist.AnyAsync(i => i.TareaId == tarea.Id && i.Id != item.Id && !i.Hecho))
                Actividades.Registrar(db, tarea.ProyectoId, tarea.Id, usuarioId, $"completó la lista de «{tarea.Titulo}»");
        }
        await db.SaveChangesAsync();
        return new ItemChecklistDto { Id = item.Id, Texto = item.Texto, Hecho = item.Hecho };
    }

    public async Task EliminarItemAsync(int itemId, int usuarioId)
    {
        var (item, _) = await ItemEditableAsync(itemId, usuarioId);
        db.ItemsChecklist.Remove(item);
        await db.SaveChangesAsync();
    }

    // ---------- Etiquetas ----------
    public async Task<List<EtiquetaDto>> ListarEtiquetasAsync(int proyectoId, int usuarioId)
    {
        await proyectos.ObtenerRolEnProyectoAsync(proyectoId, usuarioId);
        return await db.Etiquetas.AsNoTracking().Where(e => e.ProyectoId == proyectoId).OrderBy(e => e.Nombre)
            .Select(e => new EtiquetaDto { Id = e.Id, Nombre = e.Nombre, Color = e.Color }).ToListAsync();
    }

    public async Task<EtiquetaDto> CrearEtiquetaAsync(int proyectoId, CrearEtiquetaDto dto, int usuarioId)
    {
        await RequerirEdicionAsync(proyectoId, usuarioId);
        var nombre = dto.Nombre.Trim();
        if (nombre.Length == 0) throw new ConflictException("El nombre no puede estar vacío.");
        if (await db.Etiquetas.CountAsync(e => e.ProyectoId == proyectoId) >= MaxEtiquetasPorProyecto)
            throw new ConflictException($"Un proyecto admite hasta {MaxEtiquetasPorProyecto} etiquetas.");
        if (await db.Etiquetas.AnyAsync(e => e.ProyectoId == proyectoId && e.Nombre.ToLower() == nombre.ToLower()))
            throw new ConflictException("Ya existe una etiqueta con ese nombre.");

        var etiqueta = new Etiqueta { ProyectoId = proyectoId, Nombre = nombre, Color = dto.Color };
        db.Etiquetas.Add(etiqueta);
        await db.SaveChangesAsync();
        return new EtiquetaDto { Id = etiqueta.Id, Nombre = etiqueta.Nombre, Color = etiqueta.Color };
    }

    public async Task EliminarEtiquetaAsync(int etiquetaId, int usuarioId)
    {
        var etiqueta = await db.Etiquetas.FindAsync(etiquetaId) ?? throw new NotFoundException("Etiqueta no encontrada.");
        await RequerirEdicionAsync(etiqueta.ProyectoId, usuarioId);
        db.Etiquetas.Remove(etiqueta);
        await db.SaveChangesAsync();
    }

    // Sustituye las etiquetas de la tarea por las indicadas (todas deben ser de su mismo proyecto).
    public async Task<List<EtiquetaDto>> AsignarEtiquetasAsync(int tareaId, AsignarEtiquetasDto dto, int usuarioId)
    {
        var tarea = await db.Tareas.Include(t => t.Etiquetas).FirstOrDefaultAsync(t => t.Id == tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");
        await RequerirEdicionAsync(tarea.ProyectoId, usuarioId);

        var ids = dto.EtiquetaIds.Distinct().ToList();
        if (ids.Count > MaxEtiquetasPorTarea) throw new ConflictException($"Una tarea admite hasta {MaxEtiquetasPorTarea} etiquetas.");

        var nuevas = await db.Etiquetas.Where(e => e.ProyectoId == tarea.ProyectoId && ids.Contains(e.Id)).ToListAsync();
        if (nuevas.Count != ids.Count) throw new ConflictException("Alguna etiqueta no existe en este proyecto.");

        var antes = tarea.Etiquetas.Select(e => e.Id).OrderBy(x => x).ToList();
        tarea.Etiquetas.Clear();
        foreach (var e in nuevas) tarea.Etiquetas.Add(e);

        if (!antes.SequenceEqual(ids.OrderBy(x => x)))
            Actividades.Registrar(db, tarea.ProyectoId, tarea.Id, usuarioId,
                nuevas.Count == 0 ? $"quitó las etiquetas de «{tarea.Titulo}»" : $"etiquetó «{tarea.Titulo}» como {string.Join(", ", nuevas.OrderBy(e => e.Nombre).Select(e => e.Nombre))}");

        await db.SaveChangesAsync();
        return nuevas.OrderBy(e => e.Nombre).Select(e => new EtiquetaDto { Id = e.Id, Nombre = e.Nombre, Color = e.Color }).ToList();
    }
}
