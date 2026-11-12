namespace GestorTareas.Api.Models;

// Registro de lo que pasa en un proyecto. El mensaje se guarda ya redactado (con el título de la
// tarea de ese momento), así sigue siendo legible aunque la tarea se renombre o se elimine.
public class Actividad
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int? TareaId { get; set; } // sin clave foránea: la actividad de una tarea borrada se conserva
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public class ItemChecklist
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public Tarea Tarea { get; set; } = null!;
    public string Texto { get; set; } = string.Empty;
    public bool Hecho { get; set; }
    public int Orden { get; set; }
}

public class Etiqueta
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
