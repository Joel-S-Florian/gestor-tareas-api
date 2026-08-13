using GestorTareas.Api.Models;

namespace GestorTareas.Api.Services;

public interface ITokenService
{
    (string token, DateTime expira) GenerarAccessToken(Usuario usuario);
    string GenerarRefreshTokenString();
}
