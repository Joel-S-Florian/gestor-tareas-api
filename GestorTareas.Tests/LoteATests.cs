using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GestorTareas.Tests;

// Roles del usuario demo: proyecto 1 = Owner, proyecto 2 = Editor, proyecto 3 = Viewer.
[Collection("api")]
public class ActividadTests(ApiFactory api)
{
    private static async Task<List<string>> Mensajes(HttpClient h, string url) =>
        (await h.GetFromJsonAsync<JsonElement>(url)).EnumerateArray().Select(a => a.GetProperty("mensaje").GetString()!).ToList();

    private static async Task<int> Crear(HttpClient h, int proyecto, string titulo)
    {
        var r = await h.PostAsJsonAsync($"/api/v1/proyectos/{proyecto}/tareas", new { titulo });
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Crear_mover_asignar_comentar_y_eliminar_quedan_en_la_actividad()
    {
        var http = await api.DemoAsync();
        var id = await Crear(http, 2, "act-tarea");
        await http.PatchAsJsonAsync($"/api/v1/tareas/{id}/mover", new { estado = "InProgress", posicion = 0 });
        await http.PutAsJsonAsync($"/api/v1/tareas/{id}", new { titulo = "act-tarea", prioridad = "Media", asignadoAId = 2 });
        await http.PostAsJsonAsync($"/api/v1/tareas/{id}/comentarios", new { contenido = "listo" });

        var deTarea = await Mensajes(http, $"/api/v1/tareas/{id}/actividad");
        Assert.Contains("creó la tarea «act-tarea»", deTarea);
        Assert.Contains("movió «act-tarea» de Por hacer a En progreso", deTarea);
        Assert.Contains("asignó «act-tarea» a Ana Torres", deTarea);
        Assert.Contains("comentó en «act-tarea»", deTarea);
        Assert.Equal("comentó en «act-tarea»", deTarea[0]); // la más reciente primero

        await http.DeleteAsync($"/api/v1/tareas/{id}");
        Assert.Contains("eliminó la tarea «act-tarea»", await Mensajes(http, "/api/v1/proyectos/2/actividad"));
    }

    [Fact]
    public async Task Editar_sin_cambios_no_ensucia_la_bitacora_y_reordenar_tampoco()
    {
        var http = await api.DemoAsync();
        var id = await Crear(http, 2, "act-quieta");
        await http.PutAsJsonAsync($"/api/v1/tareas/{id}", new { titulo = "act-quieta", prioridad = "Media" });
        await http.PatchAsJsonAsync($"/api/v1/tareas/{id}/mover", new { estado = "ToDo", posicion = 0 });
        Assert.Single(await Mensajes(http, $"/api/v1/tareas/{id}/actividad")); // solo "creó"
    }

    [Fact]
    public async Task Cualquier_miembro_ve_la_actividad_y_quien_no_lo_es_no()
    {
        var demo = await api.DemoAsync();
        Assert.Equal(HttpStatusCode.OK, (await demo.GetAsync("/api/v1/proyectos/3/actividad")).StatusCode); // Viewer
        var luis = await api.ComoAsync("luis@demo.com", "luis123");
        Assert.Equal(HttpStatusCode.Forbidden, (await luis.GetAsync("/api/v1/proyectos/2/actividad")).StatusCode); // luis no está en el 2
    }
}

[Collection("api")]
public class ChecklistTests(ApiFactory api)
{
    private static async Task<int> Crear(HttpClient h, int proyecto, string titulo) =>
        (await (await h.PostAsJsonAsync($"/api/v1/proyectos/{proyecto}/tareas", new { titulo })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

    private static async Task<int> Item(HttpClient h, int tarea, string texto)
    {
        var r = await h.PostAsJsonAsync($"/api/v1/tareas/{tarea}/checklist", new { texto });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<JsonElement> EnLista(HttpClient h, int proyecto, int tarea) =>
        (await h.GetFromJsonAsync<JsonElement>($"/api/v1/proyectos/{proyecto}/tareas?pageSize=100")).GetProperty("items").EnumerateArray().First(t => t.GetProperty("id").GetInt32() == tarea);

    [Fact]
    public async Task El_progreso_se_ve_en_el_listado_y_el_detalle_trae_los_items()
    {
        var http = await api.DemoAsync();
        var tarea = await Crear(http, 2, "chk-progreso");
        var a = await Item(http, tarea, "uno");
        await Item(http, tarea, "dos");
        await http.PatchAsJsonAsync($"/api/v1/checklist/{a}", new { hecho = true });

        var fila = await EnLista(http, 2, tarea);
        Assert.Equal(2, fila.GetProperty("totalChecklist").GetInt32());
        Assert.Equal(1, fila.GetProperty("checklistHechos").GetInt32());

        var detalle = (await http.GetFromJsonAsync<JsonElement>($"/api/v1/tareas/{tarea}")).GetProperty("checklist");
        Assert.Equal(new[] { "uno", "dos" }, detalle.EnumerateArray().Select(i => i.GetProperty("texto").GetString()));
    }

    [Fact]
    public async Task Completar_el_ultimo_item_se_registra_una_sola_vez_en_la_actividad()
    {
        var http = await api.DemoAsync();
        var tarea = await Crear(http, 2, "chk-completa");
        var a = await Item(http, tarea, "a");
        var b = await Item(http, tarea, "b");
        await http.PatchAsJsonAsync($"/api/v1/checklist/{a}", new { hecho = true });
        await http.PatchAsJsonAsync($"/api/v1/checklist/{b}", new { hecho = true });

        var actividad = (await http.GetFromJsonAsync<JsonElement>($"/api/v1/tareas/{tarea}/actividad")).EnumerateArray().Select(x => x.GetProperty("mensaje").GetString());
        Assert.Single(actividad, m => m == "completó la lista de «chk-completa»");
    }

    [Fact]
    public async Task El_Viewer_no_puede_tocar_la_lista_y_Editor_si_puede_borrar_items()
    {
        var http = await api.DemoAsync();
        var tareaViewer = (await (await http.GetAsync("/api/v1/proyectos/3/tareas")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items")[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync($"/api/v1/tareas/{tareaViewer}/checklist", new { texto = "x" })).StatusCode);

        var tarea = await Crear(http, 2, "chk-borrar");
        var item = await Item(http, tarea, "borrame");
        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/v1/checklist/{item}")).StatusCode);
        Assert.Equal(0, (await EnLista(http, 2, tarea)).GetProperty("totalChecklist").GetInt32());
    }

    [Fact]
    public async Task Hay_un_tope_de_50_items_y_no_se_aceptan_textos_vacios()
    {
        var http = await api.DemoAsync();
        var tarea = await Crear(http, 2, "chk-tope");
        for (var i = 0; i < 50; i++) await Item(http, tarea, $"i{i}");
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsJsonAsync($"/api/v1/tareas/{tarea}/checklist", new { texto = "uno más" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync($"/api/v1/tareas/{tarea}/checklist", new { texto = "" })).StatusCode);
    }
}

[Collection("api")]
public class EtiquetasTests(ApiFactory api)
{
    private static async Task<int> Etiqueta(HttpClient h, int proyecto, string nombre, string color = "#ff0000")
    {
        var r = await h.PostAsJsonAsync($"/api/v1/proyectos/{proyecto}/etiquetas", new { nombre, color });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<int> Tarea(HttpClient h, int proyecto, string titulo) =>
        (await (await h.PostAsJsonAsync($"/api/v1/proyectos/{proyecto}/tareas", new { titulo })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

    [Fact]
    public async Task Crear_etiquetas_exige_rol_de_edicion_y_no_admite_nombres_repetidos()
    {
        var http = await api.DemoAsync();
        await Etiqueta(http, 2, "urgente");
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsJsonAsync("/api/v1/proyectos/2/etiquetas", new { nombre = "URGENTE", color = "#00ff00" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/v1/proyectos/3/etiquetas", new { nombre = "x", color = "#00ff00" })).StatusCode); // Viewer
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/v1/proyectos/2/etiquetas", new { nombre = "y", color = "rojo" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/v1/proyectos/3/etiquetas")).StatusCode); // el Viewer si las ve
    }

    [Fact]
    public async Task Las_etiquetas_se_asignan_aparecen_en_el_listado_y_filtran()
    {
        var http = await api.DemoAsync();
        var bug = await Etiqueta(http, 2, "bug-filtro");
        var etiquetada = await Tarea(http, 2, "eti-con");
        await Tarea(http, 2, "eti-sin");

        var r = await http.PutAsJsonAsync($"/api/v1/tareas/{etiquetada}/etiquetas", new { etiquetaIds = new[] { bug } });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var filtradas = (await http.GetFromJsonAsync<JsonElement>($"/api/v1/proyectos/2/tareas?pageSize=100&etiquetaId={bug}")).GetProperty("items").EnumerateArray().ToList();
        Assert.Single(filtradas);
        Assert.Equal("eti-con", filtradas[0].GetProperty("titulo").GetString());
        Assert.Equal("bug-filtro", filtradas[0].GetProperty("etiquetas")[0].GetProperty("nombre").GetString());

        var actividad = (await http.GetFromJsonAsync<JsonElement>($"/api/v1/tareas/{etiquetada}/actividad")).EnumerateArray().Select(a => a.GetProperty("mensaje").GetString());
        Assert.Contains("etiquetó «eti-con» como bug-filtro", actividad);
    }

    [Fact]
    public async Task No_se_puede_usar_una_etiqueta_de_otro_proyecto_y_borrarla_la_quita_de_las_tareas()
    {
        var http = await api.DemoAsync();
        var deOtro = await Etiqueta(http, 1, "solo-del-1");
        var propia = await Etiqueta(http, 2, "borrable");
        var tarea = await Tarea(http, 2, "eti-borrar");

        Assert.Equal(HttpStatusCode.Conflict, (await http.PutAsJsonAsync($"/api/v1/tareas/{tarea}/etiquetas", new { etiquetaIds = new[] { deOtro } })).StatusCode);

        await http.PutAsJsonAsync($"/api/v1/tareas/{tarea}/etiquetas", new { etiquetaIds = new[] { propia } });
        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/v1/etiquetas/{propia}")).StatusCode);
        var detalle = await http.GetFromJsonAsync<JsonElement>($"/api/v1/tareas/{tarea}");
        Assert.Equal(0, detalle.GetProperty("etiquetas").GetArrayLength());
    }

    [Fact]
    public async Task El_Viewer_no_puede_etiquetar_tareas()
    {
        var http = await api.DemoAsync();
        var tarea = (await (await http.GetAsync("/api/v1/proyectos/3/tareas")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items")[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PutAsJsonAsync($"/api/v1/tareas/{tarea}/etiquetas", new { etiquetaIds = Array.Empty<int>() })).StatusCode);
    }
}

[Collection("api")]
public class MencionesTests(ApiFactory api)
{
    private static async Task<int> NoLeidas(HttpClient h) => (await h.GetFromJsonAsync<JsonElement>("/api/v1/notificaciones")).GetProperty("noLeidas").GetInt32();

    private static async Task<int> Tarea(HttpClient h, int proyecto, string titulo) =>
        (await (await h.PostAsJsonAsync($"/api/v1/proyectos/{proyecto}/tareas", new { titulo })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

    [Fact]
    public async Task Mencionar_con_arroba_avisa_a_los_miembros_mencionados_una_sola_vez()
    {
        var demo = await api.DemoAsync();
        var ana = await api.ComoAsync("ana@demo.com", "ana123");
        var luis = await api.ComoAsync("luis@demo.com", "luis123");
        var tarea = await Tarea(demo, 1, "mencion-1");
        var (antesAna, antesLuis) = (await NoLeidas(ana), await NoLeidas(luis));

        await demo.PostAsJsonAsync($"/api/v1/tareas/{tarea}/comentarios", new { contenido = "Revisen esto @ana y @luis. @ANA, ¿puedes?" });

        Assert.Equal(antesAna + 1, await NoLeidas(ana));
        Assert.Equal(antesLuis + 1, await NoLeidas(luis));
        var aviso = (await ana.GetFromJsonAsync<JsonElement>("/api/v1/notificaciones")).GetProperty("items")[0].GetProperty("mensaje").GetString();
        Assert.Equal("Diego Morales te mencionó en «mencion-1».", aviso);
    }

    [Fact]
    public async Task No_se_avisa_a_uno_mismo_ni_a_quien_no_es_miembro_ni_por_un_email_escrito_en_el_texto()
    {
        var demo = await api.DemoAsync();
        var ana = await api.ComoAsync("ana@demo.com", "ana123");
        var luis = await api.ComoAsync("luis@demo.com", "luis123");
        var tareaDelDos = await Tarea(demo, 2, "mencion-2"); // luis no es miembro del proyecto 2
        var tareaDelUno = await Tarea(demo, 1, "mencion-3");
        var (antesDemo, antesLuis, antesAna) = (await NoLeidas(demo), await NoLeidas(luis), await NoLeidas(ana));

        await demo.PostAsJsonAsync($"/api/v1/tareas/{tareaDelDos}/comentarios", new { contenido = "@luis no deberia enterarse" });
        await demo.PostAsJsonAsync($"/api/v1/tareas/{tareaDelUno}/comentarios", new { contenido = "me menciono @demo y escribo a juan@ana.com" });

        Assert.Equal(antesLuis, await NoLeidas(luis));
        Assert.Equal(antesDemo, await NoLeidas(demo));
        Assert.Equal(antesAna, await NoLeidas(ana));
    }
}
