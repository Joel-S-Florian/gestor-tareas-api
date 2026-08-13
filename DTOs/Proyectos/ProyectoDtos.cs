using System.ComponentModel.DataAnnotations;
using GestorTareas.Api.Models;

namespace GestorTareas.Api.DTOs.Proyectos;

public class CrearProyectoDto
{
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "El color debe ser un hex válido, ej: #6366f1")]
    public string Color { get; set; } = "#6366f1";
}

public class ActualizarProyectoDto
{
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "El color debe ser un hex válido, ej: #6366f1")]
    public string Color { get; set; } = "#6366f1";
}

public class ProyectoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Color { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public int PropietarioId { get; set; }
    public string PropietarioNombre { get; set; } = string.Empty;
    public RolProyecto MiRol { get; set; }
    public int TotalTareas { get; set; }
    public int TotalMiembros { get; set; }
}

public class InvitarMiembroDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public RolProyecto Rol { get; set; } = RolProyecto.Viewer;
}

public class CambiarRolMiembroDto
{
    [Required]
    public RolProyecto Rol { get; set; }
}

public class MiembroDto
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolProyecto Rol { get; set; }
    public DateTime FechaIngreso { get; set; }
}
