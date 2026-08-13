using System.ComponentModel.DataAnnotations;
using GestorTareas.Api.Models;

namespace GestorTareas.Api.DTOs.Tareas;

public class CrearTareaDto
{
    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Descripcion { get; set; }

    public PrioridadTarea Prioridad { get; set; } = PrioridadTarea.Media;

    public DateTime? FechaVencimiento { get; set; }

    public int? AsignadoAId { get; set; }
}

public class ActualizarTareaDto
{
    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Descripcion { get; set; }

    public PrioridadTarea Prioridad { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int? AsignadoAId { get; set; }
}

public class CambiarEstadoTareaDto
{
    [Required]
    public EstadoTarea Estado { get; set; }
}

// Se recibe por query string en el GET, no por body.
public class TareaFiltroDto
{
    public EstadoTarea? Estado { get; set; }
    public PrioridadTarea? Prioridad { get; set; }
    public int? AsignadoAId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public class TareaDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public EstadoTarea Estado { get; set; }
    public PrioridadTarea Prioridad { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public int ProyectoId { get; set; }
    public int? AsignadoAId { get; set; }
    public string? AsignadoANombre { get; set; }
    public int TotalComentarios { get; set; }
    public int TotalAdjuntos { get; set; }
}

// Se usa en el detalle de una tarea individual: incluye comentarios y adjuntos completos,
// no solo el conteo (que es lo único que trae TareaDto en el listado del tablero).
public class TareaDetalleDto : TareaDto
{
    public List<Comentarios.ComentarioDto> Comentarios { get; set; } = new();
    public List<Adjuntos.AdjuntoDto> Adjuntos { get; set; } = new();
}
