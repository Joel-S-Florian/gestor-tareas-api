namespace GestorTareas.Api.Models;

public class Tarea
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public EstadoTarea Estado { get; set; } = EstadoTarea.ToDo;
    public PrioridadTarea Prioridad { get; set; } = PrioridadTarea.Media;
    public DateTime? FechaVencimiento { get; set; }

    public int ProyectoId { get; set; }
    public Proyecto Proyecto { get; set; } = null!;

    public int? AsignadoAId { get; set; }
    public Usuario? AsignadoA { get; set; }

    public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
    public ICollection<Adjunto> Adjuntos { get; set; } = new List<Adjunto>();
}
