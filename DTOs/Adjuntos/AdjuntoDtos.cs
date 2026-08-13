namespace GestorTareas.Api.DTOs.Adjuntos;

public class AdjuntoDto
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public DateTime FechaSubida { get; set; }
}
