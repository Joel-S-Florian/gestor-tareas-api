using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GestorTareas.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUsuarioId(this ClaimsPrincipal user)
    {
        // El TokenService guarda el id en el claim "sub".
        var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (raw == null || !int.TryParse(raw, out var id))
            throw new UnauthorizedAccessException("Token sin identificador de usuario válido.");

        return id;
    }
}
