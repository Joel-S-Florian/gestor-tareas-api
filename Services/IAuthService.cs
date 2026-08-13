using GestorTareas.Api.DTOs.Auth;

namespace GestorTareas.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegistrarAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<UsuarioDto> ObtenerPerfilAsync(int usuarioId);
    Task<UsuarioDto> ActualizarPerfilAsync(int usuarioId, ActualizarPerfilDto dto);
}

public class AuthException : Exception
{
    public AuthException(string mensaje) : base(mensaje) { }
}
