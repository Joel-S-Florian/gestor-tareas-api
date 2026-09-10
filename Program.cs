using System.Text;
using GestorTareas.Api.Data;
using GestorTareas.Api.Middleware;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ---------- Base de datos ----------
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

// Log para debug
logger.LogInformation("Connection String detected: {conn}", connectionString?.Substring(0, Math.Min(connectionString.Length, 20)) + "...");

if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.LocalPath.TrimStart('/')};Pooling=true;SSL Mode=Require;Trust Server Certificate=True;";
}

// Fallback inteligente: si no hay variable de entorno, detecta LocalDB automáticamente
var dbProvider = builder.Configuration["DB_PROVIDER"] 
    ?? (connectionString?.Contains("(localdb)", StringComparison.OrdinalIgnoreCase) == true ? "SQLSERVER" : "POSTGRES");

logger.LogInformation("DB_PROVIDER configured as: {provider}", dbProvider);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider.Equals("POSTGRES", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }
    else if (dbProvider.Equals("SQLSERVER", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        throw new InvalidOperationException($"DB_PROVIDER '{dbProvider}' is not supported. Use POSTGRES or SQLSERVER.");
    }
});

// ---------- Servicios propios ----------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IProyectoService, ProyectoService>();
builder.Services.AddScoped<ITareaService, TareaService>();
builder.Services.AddScoped<IComentarioService, ComentarioService>();
builder.Services.AddScoped<IAdjuntoService, AdjuntoService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ---------- Controllers ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// ---------- JWT Authentication ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSection["Secret"];

// Validación defensiva: garantiza mínimo 32 bytes (256 bits) para HMAC-SHA256
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetBytes(jwtSecret).Length < 32)
{
    jwtSecret = "DESARROLLO_SECRET_KEY_CON_MAS_DE_32_CARACTERES_123456789";
    logger.LogWarning("JWT Secret inválido o ausente. Usando clave de desarrollo por defecto.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

// ---------- CORS ----------
var corsOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
    ?? new[] { builder.Configuration["SpaOrigin"] ?? "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ---------- Swagger con soporte JWT (botón Authorize) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Gestor de Tareas API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingresa: Bearer {tu token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------- Health check ----------
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors("AllowSpecificOrigins");

// Aplicar migraciones si la variable está activa (para producción en Render)
var runMigrations = Environment.GetEnvironmentVariable("RUN_MIGRATIONS");
if (runMigrations == "true")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}

// ---------- Swagger ----------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestor de Tareas API v1");
    c.RoutePrefix = "swagger";
});

// ---------- Migraciones + seed automáticos en Development ----------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    SeedData.Seed(db);
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();