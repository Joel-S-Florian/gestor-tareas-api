namespace GestorTareas.Api.DTOs.Notificaciones;

public class NotificacionDto
{
    public int Id { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? ProyectoId { get; set; }
    public int? TareaId { get; set; }
    public bool Leida { get; set; }
    public DateTime Fecha { get; set; }
}

public class NotificacionesDto
{
    public int NoLeidas { get; set; }
    public List<NotificacionDto> Items { get; set; } = new();
}
