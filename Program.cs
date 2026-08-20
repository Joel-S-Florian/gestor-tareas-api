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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

// Convertir postgres:// a formato Key-Value SIEMPRE para compatibilidad con Npgsql
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.LocalPath.TrimStart('/')};Pooling=true;SSL Mode=Require;Trust Server Certificate=True;";
}

// Guardar la cadena convertida en la configuración para que todos los componentes la usen
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

var dbProvider = builder.Configuration["DB_PROVIDER"] ?? "SQLSERVER";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider == "POSTGRES")
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
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
// Los enums se serializan como strings ("ToDo", "Owner", "Media") para que el
// cliente React los consuma de forma legible y no dependa de valores numéricos.
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

// ---------- CORS ----------
var corsOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>();
if (corsOrigins != null && corsOrigins.Any())
{
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
}

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

// Aplicar migraciones si la variable está activa
var runMigrations = Environment.GetEnvironmentVariable("RUN_MIGRATIONS");
if (runMigrations == "true")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}

// ---------- Swagger con soporte JWT (botón Authorize) ----------
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
