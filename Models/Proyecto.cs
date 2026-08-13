namespace GestorTareas.Api.Models;

public class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Color { get; set; } = "#6366f1";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public int PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;

    public ICollection<MiembroProyecto> Miembros { get; set; } = new List<MiembroProyecto>();
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
