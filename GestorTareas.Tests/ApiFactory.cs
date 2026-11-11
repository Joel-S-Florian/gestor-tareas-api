using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace GestorTareas.Tests;

// Levanta la API real contra una base PostgreSQL propia (se crea y se borra por ejecución).
// Servidor: variable GT_TEST_PG (por defecto, el PostgreSQL local sin clave; en CI, el servicio de Actions).
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string Servidor =
        Environment.GetEnvironmentVariable("GT_TEST_PG") ?? "Host=localhost;Username=postgres";

    private readonly string _bd = "gt_test_" + Guid.NewGuid().ToString("N")[..12];
    private readonly Dictionary<string, string> _tokens = new();
    private readonly SemaphoreSlim _login = new(1, 1);

    public int LimiteIntentos { get; init; } = 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // migra y siembra los usuarios demo
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"{Servidor};Database={_bd}");
        builder.UseSetting("RateLimit:Auth:Permit", LimiteIntentos.ToString());
        builder.UseSetting("UploadsPath", Path.Combine(Path.GetTempPath(), _bd));
    }

    public async Task InitializeAsync()
    {
        await using var c = new NpgsqlConnection(Servidor + ";Database=postgres");
        await c.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {_bd}", c).ExecuteNonQueryAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var c = new NpgsqlConnection(Servidor + ";Database=postgres");
        await c.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE IF EXISTS {_bd} WITH (FORCE)", c).ExecuteNonQueryAsync();
    }

    // Cliente autenticado como ese usuario (el login se hace una sola vez por usuario).
    public async Task<HttpClient> ComoAsync(string email, string clave)
    {
        await _login.WaitAsync();
        try
        {
            if (!_tokens.TryGetValue(email, out var token))
            {
                var r = await CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email, password = clave });
                r.EnsureSuccessStatusCode();
                token = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
                _tokens[email] = token;
            }
            var http = CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return http;
        }
        finally { _login.Release(); }
    }

    public Task<HttpClient> DemoAsync() => ComoAsync("demo@demo.com", "demo123");
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory> { }
