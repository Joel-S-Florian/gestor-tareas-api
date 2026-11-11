using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GestorTareas.Tests;

// El usuario demo tiene un rol distinto en cada proyecto sembrado:
// proyecto 1 = Owner, proyecto 2 = Editor, proyecto 3 = Viewer.
[Collection("api")]
public class RolesTests(ApiFactory api)
{
    private static async Task<int> UnaTareaAsync(HttpClient http, int proyecto)
    {
        var json = await http.GetFromJsonAsync<JsonElement>($"/api/v1/proyectos/{proyecto}/tareas");
        return json.GetProperty("items")[0].GetProperty("id").GetInt32();
    }

    [Theory]
    // proyecto, acción, esperado
    [InlineData(1, "leer", HttpStatusCode.OK)]
    [InlineData(2, "leer", HttpStatusCode.OK)]
    [InlineData(3, "leer", HttpStatusCode.OK)]
    [InlineData(1, "editar-proyecto", HttpStatusCode.OK)]
    [InlineData(2, "editar-proyecto", HttpStatusCode.OK)]
    [InlineData(3, "editar-proyecto", HttpStatusCode.Forbidden)]
    [InlineData(1, "crear-tarea", HttpStatusCode.Created)]
    [InlineData(2, "crear-tarea", HttpStatusCode.Created)]
    [InlineData(3, "crear-tarea", HttpStatusCode.Forbidden)]
    [InlineData(1, "cambiar-estado", HttpStatusCode.OK)]
    [InlineData(2, "cambiar-estado", HttpStatusCode.OK)]
    [InlineData(3, "cambiar-estado", HttpStatusCode.Forbidden)]
    [InlineData(1, "comentar", HttpStatusCode.OK)]
    [InlineData(2, "comentar", HttpStatusCode.OK)]
    [InlineData(3, "comentar", HttpStatusCode.OK)]
    [InlineData(1, "invitar", HttpStatusCode.Conflict)]   // Owner; ana ya es miembro del proyecto 1
    [InlineData(2, "invitar", HttpStatusCode.Forbidden)]
    [InlineData(3, "invitar", HttpStatusCode.Forbidden)]
    [InlineData(1, "cambiar-rol", HttpStatusCode.OK)]
    [InlineData(2, "cambiar-rol", HttpStatusCode.Forbidden)]
    [InlineData(3, "cambiar-rol", HttpStatusCode.Forbidden)]
    [InlineData(1, "eliminar-proyecto", HttpStatusCode.Conflict)]   // Owner, pero tiene tareas
    [InlineData(2, "eliminar-proyecto", HttpStatusCode.Forbidden)]
    [InlineData(3, "eliminar-proyecto", HttpStatusCode.Forbidden)]
    public async Task Matriz_de_roles(int proyecto, string accion, HttpStatusCode esperado)
    {
        var http = await api.DemoAsync();
        var tarea = await UnaTareaAsync(http, proyecto);
        var p = $"/api/v1/proyectos/{proyecto}";

        var r = accion switch
        {
            "leer" => await http.GetAsync($"{p}/tareas"),
            "editar-proyecto" => await http.PutAsJsonAsync(p, new { nombre = "Nombre editado", color = "#112233" }),
            "crear-tarea" => await http.PostAsJsonAsync($"{p}/tareas", new { titulo = "Nueva" }),
            "cambiar-estado" => await http.PatchAsJsonAsync($"/api/v1/tareas/{tarea}/estado", new { estado = "InProgress" }),
            "comentar" => await http.PostAsJsonAsync($"/api/v1/tareas/{tarea}/comentarios", new { contenido = "Hola" }),
            "invitar" => await http.PostAsJsonAsync($"{p}/miembros", new { email = "ana@demo.com", rol = "Viewer" }),
            "cambiar-rol" => await http.PutAsJsonAsync($"{p}/miembros/3/rol", new { rol = "Editor" }),
            "eliminar-proyecto" => await http.DeleteAsync(p),
            _ => throw new ArgumentException(accion)
        };

        Assert.Equal(esperado, r.StatusCode);
    }

    [Fact]
    public async Task Quien_no_es_miembro_no_ve_nada_del_proyecto()
    {
        var registro = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
            new { nombre = "Ajeno", email = "ajeno@demo.com", password = "Ajeno12345" });
        Assert.Equal(HttpStatusCode.OK, registro.StatusCode);
        var http = await api.ComoAsync("ajeno@demo.com", "Ajeno12345");

        foreach (var url in new[] { "/api/v1/proyectos/1", "/api/v1/proyectos/1/tareas", "/api/v1/proyectos/1/miembros", "/api/v1/tareas/1" })
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync(url)).StatusCode);

        var lista = await http.GetFromJsonAsync<JsonElement>("/api/v1/proyectos");
        Assert.Equal(0, lista.GetArrayLength());
    }

    [Fact]
    public async Task Sin_token_todo_es_401()
    {
        var anonimo = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/v1/proyectos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/v1/tareas/1")).StatusCode);
    }
}
