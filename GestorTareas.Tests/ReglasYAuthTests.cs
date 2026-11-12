using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GestorTareas.Tests;

[Collection("api")]
public class ReglasYAuthTests(ApiFactory api)
{
    private static Task<HttpResponseMessage> Login(HttpClient h, string email, string clave) =>
        h.PostAsJsonAsync("/api/v1/auth/login", new { email, password = clave });

    [Fact]
    public async Task El_login_fijo_que_existia_ya_no_entra()
    {
        var r = await Login(api.CreateClient(), "joel@demo.com", "Demo1234!");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // Cliente sin manejo automático de cookies: así se puede reenviar a mano una cookie vieja.
    private HttpClient Manual() => api.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

    private static string CookieDe(HttpResponseMessage r) =>
        r.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("gt_rt=")).Split(';')[0];

    private static Task<HttpResponseMessage> ConCookie(HttpClient h, string ruta, string cookie)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, ruta);
        req.Headers.Add("Cookie", cookie);
        return h.SendAsync(req);
    }

    [Fact]
    public async Task El_refresh_token_va_en_cookie_HttpOnly_y_no_en_el_JSON()
    {
        var r = await Login(Manual(), "ana@demo.com", "ana123");
        var setCookie = r.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("gt_rt="));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", await r.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task El_refresh_token_rota_y_el_viejo_deja_de_servir()
    {
        var http = Manual();
        var viejo = CookieDe(await Login(http, "ana@demo.com", "ana123"));

        var r1 = await ConCookie(http, "/api/v1/auth/refresh", viejo);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var nuevo = CookieDe(r1);
        Assert.NotEqual(viejo, nuevo);
        Assert.Contains("accessToken", await r1.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, (await ConCookie(http, "/api/v1/auth/refresh", viejo)).StatusCode);
    }

    [Fact]
    public async Task Reusar_un_token_rotado_cierra_todas_las_sesiones_del_usuario()
    {
        var http = Manual();
        var viejo = CookieDe(await Login(http, "ana@demo.com", "ana123"));
        var vigente = CookieDe(await ConCookie(http, "/api/v1/auth/refresh", viejo));

        // Alguien presenta el token ya rotado (robo): el vigente también queda inservible.
        Assert.Equal(HttpStatusCode.Unauthorized, (await ConCookie(http, "/api/v1/auth/refresh", viejo)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ConCookie(http, "/api/v1/auth/refresh", vigente)).StatusCode);
    }

    [Fact]
    public async Task Sin_cookie_el_refresh_es_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Manual().PostAsync("/api/v1/auth/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task Logout_revoca_el_refresh_token_y_borra_la_cookie()
    {
        var http = Manual();
        var cookie = CookieDe(await Login(http, "luis@demo.com", "luis123"));

        var salida = await ConCookie(http, "/api/v1/auth/logout", cookie);
        Assert.Equal(HttpStatusCode.NoContent, salida.StatusCode);
        Assert.Contains("gt_rt=;", salida.Headers.GetValues("Set-Cookie").First());
        Assert.Equal(HttpStatusCode.Unauthorized, (await ConCookie(http, "/api/v1/auth/refresh", cookie)).StatusCode);
    }

    [Fact]
    public async Task Registro_rechaza_clave_corta_y_email_repetido()
    {
        var http = api.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await http.PostAsJsonAsync("/api/v1/auth/register", new { nombre = "X", email = "x@x.com", password = "corta" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await http.PostAsJsonAsync("/api/v1/auth/register", new { nombre = "X", email = "demo@demo.com", password = "Larga12345" })).StatusCode);
    }

    [Fact]
    public async Task No_se_puede_asignar_una_tarea_a_quien_no_es_miembro()
    {
        var http = await api.DemoAsync();
        // luis (id 3) no es miembro del proyecto 2
        var r = await http.PostAsJsonAsync("/api/v1/proyectos/2/tareas", new { titulo = "Asignada mal", asignadoAId = 3 });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task El_tamano_de_pagina_tiene_tope()
    {
        var http = await api.DemoAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/v1/proyectos/1/tareas?pageSize=100000")).StatusCode);
    }

    [Fact]
    public async Task Un_proyecto_sin_tareas_si_se_puede_eliminar()
    {
        var http = await api.DemoAsync();
        var creado = await (await http.PostAsJsonAsync("/api/v1/proyectos", new { nombre = "Vacío" })).Content.ReadFromJsonAsync<JsonElement>();
        var id = creado.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/v1/proyectos/{id}")).StatusCode);
    }
}

public class LimiteDeIntentosTests
{
    [Fact]
    public async Task El_login_se_limita_por_ip()
    {
        var api = new ApiFactory { LimiteIntentos = 3 };
        await api.InitializeAsync();
        try
        {
            var http = api.CreateClient();
            var codigos = new List<HttpStatusCode>();
            for (var i = 0; i < 4; i++)
                codigos.Add((await http.PostAsJsonAsync("/api/v1/auth/login", new { email = "x@x.com", password = "mala" })).StatusCode);

            Assert.Equal(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests }, codigos);
        }
        finally { await ((IAsyncLifetime)api).DisposeAsync(); }
    }
}
