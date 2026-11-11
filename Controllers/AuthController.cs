using GestorTareas.Api.DTOs.Auth;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private const string CookieRefresh = "gt_rt";

    private readonly IAuthService _authService;
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        try
        {
            var respuesta = await _authService.RegistrarAsync(dto);
            PonerCookie(respuesta.RefreshToken);
            return Ok(respuesta);
        }
        catch (AuthException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        try
        {
            var respuesta = await _authService.LoginAsync(dto);
            PonerCookie(respuesta.RefreshToken);
            return Ok(respuesta);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status401Unauthorized });
        }
    }

    // El refresh token viaja solo en la cookie HttpOnly: el JavaScript del SPA nunca lo ve.
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var token = Request.Cookies[CookieRefresh];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new ProblemDetails { Title = "No hay sesión.", Status = StatusCodes.Status401Unauthorized });

        try
        {
            var respuesta = await _authService.RefreshAsync(token);
            PonerCookie(respuesta.RefreshToken);
            return Ok(respuesta);
        }
        catch (AuthException ex)
        {
            BorrarCookie();
            return Unauthorized(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status401Unauthorized });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[CookieRefresh];
        if (!string.IsNullOrEmpty(token)) await _authService.LogoutAsync(token);
        BorrarCookie();
        return NoContent();
    }

    [HttpGet("perfil")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> ObtenerPerfil()
    {
        return Ok(await _authService.ObtenerPerfilAsync(User.GetUsuarioId()));
    }

    [HttpPut("perfil")]
    [Authorize]
    public async Task<ActionResult<UsuarioDto>> ActualizarPerfil(ActualizarPerfilDto dto)
    {
        try
        {
            return Ok(await _authService.ActualizarPerfilAsync(User.GetUsuarioId(), dto));
        }
        catch (AuthException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    // Lax: el SPA y la API deben compartir sitio (en producción, la API detrás del mismo dominio
    // del SPA con un rewrite). Para desplegarlos en sitios distintos: Auth:CookieSameSite = None
    // (exige HTTPS y los navegadores pueden bloquear cookies de terceros).
    private CookieOptions OpcionesCookie() => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = Enum.TryParse<SameSiteMode>(_config["Auth:CookieSameSite"], true, out var modo) ? modo : SameSiteMode.Lax,
        Path = "/api/v1/auth"
    };

    private void PonerCookie(string refreshToken)
    {
        var opciones = OpcionesCookie();
        opciones.Expires = DateTimeOffset.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDias"] ?? "7"));
        Response.Cookies.Append(CookieRefresh, refreshToken, opciones);
    }

    private void BorrarCookie() => Response.Cookies.Delete(CookieRefresh, OpcionesCookie());
}
