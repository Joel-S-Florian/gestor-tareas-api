using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GestorTareas.Tests;

[Collection("api")]
public class AdjuntosTests(ApiFactory api)
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private static async Task<int> TareaAsync(HttpClient h, int proyecto) =>
        (await h.GetFromJsonAsync<JsonElement>($"/api/v1/proyectos/{proyecto}/tareas")).GetProperty("items")[0].GetProperty("id").GetInt32();

    private static MultipartFormDataContent Archivo(string nombre, string mime, byte[] bytes)
    {
        var parte = new ByteArrayContent(bytes);
        parte.Headers.ContentType = new MediaTypeHeaderValue(mime);
        return new MultipartFormDataContent { { parte, "archivo", nombre } };
    }

    [Fact]
    public async Task Editor_sube_y_descarga_una_imagen_valida()
    {
        var http = await api.DemoAsync();
        var tarea = await TareaAsync(http, 2); // Editor
        var subida = await http.PostAsync($"/api/v1/tareas/{tarea}/adjuntos", Archivo("foto.png", "image/png", Png));
        Assert.Equal(HttpStatusCode.OK, subida.StatusCode);

        var id = (await subida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var bajada = await http.GetAsync($"/api/v1/adjuntos/{id}");
        Assert.Equal(HttpStatusCode.OK, bajada.StatusCode);
        Assert.Equal(Png, await bajada.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Viewer_no_puede_subir()
    {
        var http = await api.DemoAsync();
        var tarea = await TareaAsync(http, 3); // Viewer
        var r = await http.PostAsync($"/api/v1/tareas/{tarea}/adjuntos", Archivo("foto.png", "image/png", Png));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Theory]
    [InlineData("virus.exe", "application/octet-stream")]
    [InlineData("foto.png", "application/pdf")]   // el tipo declarado no coincide con la extensión
    public async Task Tipos_no_permitidos_se_rechazan(string nombre, string mime)
    {
        var http = await api.DemoAsync();
        var tarea = await TareaAsync(http, 2);
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync($"/api/v1/tareas/{tarea}/adjuntos", Archivo(nombre, mime, Png))).StatusCode);
    }

    [Fact]
    public async Task Un_ejecutable_renombrado_como_png_se_rechaza_por_su_contenido()
    {
        var http = await api.DemoAsync();
        var tarea = await TareaAsync(http, 2);
        var falso = "MZ esto no es una imagen"u8.ToArray();
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync($"/api/v1/tareas/{tarea}/adjuntos", Archivo("foto.png", "image/png", falso))).StatusCode);
    }

    [Fact]
    public async Task Mas_de_5_MB_se_rechaza()
    {
        var http = await api.DemoAsync();
        var tarea = await TareaAsync(http, 2);
        var grande = new byte[5 * 1024 * 1024 + 1];
        "%PDF"u8.CopyTo(grande);
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync($"/api/v1/tareas/{tarea}/adjuntos", Archivo("grande.pdf", "application/pdf", grande))).StatusCode);
    }
}
