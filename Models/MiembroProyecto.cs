namespace GestorTareas.Api.Models;

// Clave primaria compuesta (ProyectoId, UsuarioId) configurada en el DbContext.
public class MiembroProyecto
{
    public int ProyectoId { get; set; }
    public Proyecto Proyecto { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public RolProyecto Rol { get; set; } = RolProyecto.Viewer;
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
}
