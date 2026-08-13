namespace GestorTareas.Api.Models;

// Tabla para poder revocar y renovar tokens sin reautenticar con contraseña.
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public bool Revocado { get; set; } = false;

    public bool EstaActivo => !Revocado && DateTime.UtcNow < FechaExpiracion;
}
