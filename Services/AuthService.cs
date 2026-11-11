using GestorTareas.Api.Data;
using GestorTareas.Api.DTOs.Auth;
using GestorTareas.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<Usuario> _hasher = new();
    private readonly int _refreshDias;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApplicationDbContext db, ITokenService tokenService, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _refreshDias = int.Parse(config["Jwt:RefreshTokenDias"] ?? "7");
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegistrarAsync(RegisterDto dto)
    {
        var existe = await _db.Usuarios.AnyAsync(u => u.Email == dto.Email);
        if (existe) throw new AuthException("Ya existe una cuenta con ese email.");

        var usuario = new Usuario { Nombre = dto.Nombre, Email = dto.Email };
        usuario.PasswordHash = _hasher.HashPassword(usuario, dto.Password);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Nuevo usuario registrado: {Email} (id {UsuarioId})", usuario.Email, usuario.Id);

        return await GenerarRespuestaAsync(usuario);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null)
        {
            _logger.LogWarning("Intento de login con email inexistente: {Email}", dto.Email);
            throw new AuthException("Credenciales inválidas.");
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login fallido por contraseña incorrecta: {Email}", dto.Email);
            throw new AuthException("Credenciales inválidas.");
        }

        _logger.LogInformation("Usuario {Email} inició sesión", usuario.Email);

        return await GenerarRespuestaAsync(usuario);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var tokenGuardado = await _db.RefreshTokens
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (tokenGuardado == null || !tokenGuardado.EstaActivo)
            throw new AuthException("Refresh token inválido o expirado.");

        // Rotación: se revoca el usado y se emite uno nuevo (mitiga el robo de tokens).
        tokenGuardado.Revocado = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Access token renovado para {Email}", tokenGuardado.Usuario.Email);

        return await GenerarRespuestaAsync(tokenGuardado.Usuario);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenGuardado = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (tokenGuardado == null) return;

        tokenGuardado.Revocado = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cierre de sesión (refresh token revocado) para el usuario {UsuarioId}", tokenGuardado.UsuarioId);
    }

    public async Task<UsuarioDto> ObtenerPerfilAsync(int usuarioId)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId)
            ?? throw new AuthException("Usuario no encontrado.");

        return new UsuarioDto { Id = usuario.Id, Nombre = usuario.Nombre, Email = usuario.Email };
    }

    public async Task<UsuarioDto> ActualizarPerfilAsync(int usuarioId, ActualizarPerfilDto dto)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId)
            ?? throw new AuthException("Usuario no encontrado.");

        var emailEnUso = await _db.Usuarios.AnyAsync(u => u.Email == dto.Email && u.Id != usuarioId);
        if (emailEnUso) throw new AuthException("Ya existe otra cuenta con ese email.");

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Email = dto.Email.Trim();
        await _db.SaveChangesAsync();

        return new UsuarioDto { Id = usuario.Id, Nombre = usuario.Nombre, Email = usuario.Email };
    }

    // Emite el par de tokens: access token JWT (corto) + refresh token (largo, persistido
    // en BD y revocable). Cada refresco gira el refresh token para limitar su vida útil.
    private async Task<AuthResponseDto> GenerarRespuestaAsync(Usuario usuario)
    {
        var (accessToken, expira) = _tokenService.GenerarAccessToken(usuario);
        var refreshToken = _tokenService.GenerarRefreshTokenString();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UsuarioId = usuario.Id,
            FechaExpiracion = DateTime.UtcNow.AddDays(_refreshDias)
        });
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpira = expira,
            Usuario = new UsuarioDto { Id = usuario.Id, Nombre = usuario.Nombre, Email = usuario.Email }
        };
    }
}
