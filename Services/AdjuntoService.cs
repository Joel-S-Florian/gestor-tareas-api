using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs.Adjuntos;
using GestorTareas.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Services;

public class AdjuntoService : IAdjuntoService
{
    private const long TamanoMaximoBytes = 5 * 1024 * 1024; // 5 MB

    // Se valida por Content-Type Y por extensión, porque el Content-Type lo controla el cliente
    // y no es 100% confiable por sí solo, pero es suficiente para el alcance de este proyecto.
    private static readonly Dictionary<string, string> TiposPermitidos = new()
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf"
    };

    private readonly ApplicationDbContext _db;
    private readonly IProyectoService _proyectoService;
    private readonly string _rutaBase;
    private readonly ILogger<AdjuntoService> _logger;

    public AdjuntoService(ApplicationDbContext db, IProyectoService proyectoService, IConfiguration config, ILogger<AdjuntoService> logger)
    {
        _db = db;
        _proyectoService = proyectoService;
        _logger = logger;

        var carpeta = config["UploadsPath"] ?? "Uploads";
        _rutaBase = Path.Combine(Directory.GetCurrentDirectory(), carpeta);
        Directory.CreateDirectory(_rutaBase);
    }

    public async Task<AdjuntoDto> SubirAsync(int tareaId, IFormFile archivo, int usuarioId)
    {
        var tarea = await _db.Tareas.FindAsync(tareaId)
            ?? throw new NotFoundException("Tarea no encontrada.");

        var rol = await _proyectoService.ObtenerRolEnProyectoAsync(tarea.ProyectoId, usuarioId);
        if (rol == Models.RolProyecto.Viewer)
            throw new ForbiddenException("Tu rol en este proyecto es de solo lectura.");

        if (archivo == null || archivo.Length == 0)
            throw new ConflictException("El archivo está vacío.");

        if (archivo.Length > TamanoMaximoBytes)
            throw new ConflictException("El archivo supera el tamaño máximo permitido de 5 MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!TiposPermitidos.TryGetValue(extension, out var mimeEsperado))
            throw new ConflictException("Tipo de archivo no permitido. Solo se aceptan imágenes (jpg, png, gif, webp) y PDF.");

        // Segunda validación: el Content-Type que envía el navegador debe coincidir con lo esperado.
        if (!string.Equals(archivo.ContentType, mimeEsperado, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("El tipo de archivo no coincide con su extensión.");

        var nombreEnDisco = $"{Guid.NewGuid()}{extension}";
        var rutaCompleta = Path.Combine(_rutaBase, nombreEnDisco);

        await using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        var adjunto = new Adjunto
        {
            TareaId = tareaId,
            NombreArchivo = archivo.FileName,
            RutaRelativa = nombreEnDisco,
            TamanoBytes = archivo.Length
        };

        _db.Adjuntos.Add(adjunto);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Adjunto {AdjuntoId} ({Nombre}, {Tamano} bytes) subido a la tarea {TareaId} por {UsuarioId}",
            adjunto.Id, adjunto.NombreArchivo, adjunto.TamanoBytes, tareaId, usuarioId);

        return new AdjuntoDto
        {
            Id = adjunto.Id,
            TareaId = adjunto.TareaId,
            NombreArchivo = adjunto.NombreArchivo,
            TamanoBytes = adjunto.TamanoBytes,
            FechaSubida = adjunto.FechaSubida
        };
    }

    public async Task<(byte[] contenido, string nombreArchivo, string contentType)> DescargarAsync(int adjuntoId, int usuarioId)
    {
        var adjunto = await _db.Adjuntos.Include(a => a.Tarea).FirstOrDefaultAsync(a => a.Id == adjuntoId)
            ?? throw new NotFoundException("Adjunto no encontrado.");

        await _proyectoService.ObtenerRolEnProyectoAsync(adjunto.Tarea.ProyectoId, usuarioId);

        var rutaCompleta = Path.Combine(_rutaBase, adjunto.RutaRelativa);
        if (!File.Exists(rutaCompleta))
            throw new NotFoundException("El archivo ya no existe en el servidor.");

        var contenido = await File.ReadAllBytesAsync(rutaCompleta);
        var extension = Path.GetExtension(adjunto.RutaRelativa).ToLowerInvariant();
        var contentType = TiposPermitidos.GetValueOrDefault(extension, "application/octet-stream");

        return (contenido, adjunto.NombreArchivo, contentType);
    }

    public async Task EliminarAsync(int adjuntoId, int usuarioId)
    {
        var adjunto = await _db.Adjuntos.Include(a => a.Tarea).FirstOrDefaultAsync(a => a.Id == adjuntoId)
            ?? throw new NotFoundException("Adjunto no encontrado.");

        var rol = await _proyectoService.ObtenerRolEnProyectoAsync(adjunto.Tarea.ProyectoId, usuarioId);
        if (rol == Models.RolProyecto.Viewer)
            throw new ForbiddenException("Tu rol en este proyecto es de solo lectura.");

        var rutaCompleta = Path.Combine(_rutaBase, adjunto.RutaRelativa);
        if (File.Exists(rutaCompleta))
            File.Delete(rutaCompleta);

        _db.Adjuntos.Remove(adjunto);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Adjunto {AdjuntoId} eliminado por {UsuarioId}", adjuntoId, usuarioId);
    }
}
