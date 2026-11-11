namespace GestorTareas.Api.Models;

// Aviso dentro de la app (sin correo). Hoy solo se genera al asignar una tarea.
public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Mensaje { get; set; } = string.Empty;
    public int? ProyectoId { get; set; }
    public int? TareaId { get; set; }
    public bool Leida { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
