using GestorTareas.Api.DTOs.Comentarios;

namespace GestorTareas.Api.Services;

public interface IComentarioService
{
    Task<ComentarioDto> AgregarAsync(int tareaId, CrearComentarioDto dto, int usuarioId);
    Task EliminarAsync(int comentarioId, int usuarioId);
}
