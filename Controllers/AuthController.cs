using GestorTareas.Api.DTOs.Auth;
using GestorTareas.Api.Extensions;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorTareas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        try
        {
            var respuesta = await _authService.RegistrarAsync(dto);
            return Ok(respuesta);
        }
        catch (AuthException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        try
        {
            var respuesta = await _authService.LoginAsync(dto);
            return Ok(respuesta);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status401Unauthorized });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshRequestDto dto)
    {
        try
        {
            var respuesta = await _authService.RefreshAsync(dto.RefreshToken);
            return Ok(respuesta);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status401Unauthorized });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequestDto dto)
    {
        await _authService.LogoutAsync(dto.RefreshToken);
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
}
