using System.ComponentModel.DataAnnotations;

namespace GestorTareas.Api.DTOs.Comentarios;

public class CrearComentarioDto
{
    [Required, MaxLength(2000)]
    public string Contenido { get; set; } = string.Empty;
}

public class ComentarioDto
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
