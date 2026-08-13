using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Middleware;

// Convierte cualquier excepción no manejada en un ProblemDetails (RFC 7807)
// y evita que se filtre un stack trace al cliente. Las excepciones de dominio
// (NotFound/Forbidden/Conflict) se mapean a su código HTTP correspondiente.
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, titulo) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
                ConflictException => (StatusCodes.Status409Conflict, ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
            };

            // Solo se loguea como error lo inesperado; lo previsible (403/404/409) es un warning.
            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Error no controlado procesando {Path}", context.Request.Path);
            else
                _logger.LogWarning("{Tipo} en {Path}: {Mensaje}", ex.GetType().Name, context.Request.Path, ex.Message);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;

            var problem = new ProblemDetails
            {
                Title = titulo,
                Status = status,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "Inténtalo de nuevo. Si el problema persiste, contacta al administrador."
                    : null
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}