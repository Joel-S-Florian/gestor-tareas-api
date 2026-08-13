using GestorTareas.Api.DTOs.Adjuntos;
using Microsoft.AspNetCore.Http;

namespace GestorTareas.Api.Services;

public interface IAdjuntoService
{
    Task<AdjuntoDto> SubirAsync(int tareaId, IFormFile archivo, int usuarioId);
    Task<(byte[] contenido, string nombreArchivo, string contentType)> DescargarAsync(int adjuntoId, int usuarioId);
    Task EliminarAsync(int adjuntoId, int usuarioId);
}
