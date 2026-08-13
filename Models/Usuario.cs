namespace GestorTareas.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<Proyecto> ProyectosPropios { get; set; } = new List<Proyecto>();
    public ICollection<MiembroProyecto> Membresias { get; set; } = new List<MiembroProyecto>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
