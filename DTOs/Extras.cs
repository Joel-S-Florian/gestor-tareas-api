using System.ComponentModel.DataAnnotations;

namespace GestorTareas.Api.DTOs.Extras;

public class ActividadDto
{
    public int Id { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public int? TareaId { get; set; }
    public DateTime Fecha { get; set; }
}

public class ItemChecklistDto
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool Hecho { get; set; }
}

public class CrearItemDto
{
    [Required, MaxLength(200)]
    public string Texto { get; set; } = string.Empty;
}

public class ActualizarItemDto
{
    [MaxLength(200)]
    public string? Texto { get; set; }
    public bool? Hecho { get; set; }
}

public class EtiquetaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class CrearEtiquetaDto
{
    [Required, MaxLength(40)]
    public string Nombre { get; set; } = string.Empty;

    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "El color debe ser un hex válido, ej: #6366f1")]
    public string Color { get; set; } = "#6366f1";
}

public class AsignarEtiquetasDto
{
    [Required, MaxLength(20)]
    public List<int> EtiquetaIds { get; set; } = new();
}
