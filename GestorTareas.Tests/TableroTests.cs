using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GestorTareas.Tests;

[Collection("api")]
public class TableroTests(ApiFactory api)
{
    private static async Task<List<(int id, string titulo, string estado)>> Listar(HttpClient h, int proyecto, string query = "")
    {
        var json = await h.GetFromJsonAsync<JsonElement>($"/api/v1/proyectos/{proyecto}/tareas?pageSize=100{query}");
        return json.GetProperty("items").EnumerateArray()
            .Select(t => (t.GetProperty("id").GetInt32(), t.GetProperty("titulo").GetString()!, t.GetProperty("estado").GetString()!)).ToList();
    }

    private static async Task<int> Crear(HttpClient h, int proyecto, object cuerpo)
    {
        var r = await h.PostAsJsonAsync($"/api/v1/proyectos/{proyecto}/tareas", cuerpo);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Las_tareas_nuevas_van_al_final_de_la_columna_y_se_pueden_reordenar()
    {
        var http = await api.DemoAsync();
        var a = await Crear(http, 2, new { titulo = "orden-A" });
        var b = await Crear(http, 2, new { titulo = "orden-B" });
        var c = await Crear(http, 2, new { titulo = "orden-C" });

        var todo = (await Listar(http, 2)).Where(t => t.estado == "ToDo" && t.titulo.StartsWith("orden-")).Select(t => t.id).ToList();
        Assert.Equal(new[] { a, b, c }, todo);

        // C pasa a la primera posición de la misma columna
        var r = await http.PatchAsJsonAsync($"/api/v1/tareas/{c}/mover", new { estado = "ToDo", posicion = 0 });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var primeras = (await Listar(http, 2)).Where(t => t.estado == "ToDo").Select(t => t.id).Take(1).ToList();
        Assert.Equal(c, primeras[0]);
        var resto = (await Listar(http, 2)).Where(t => t.estado == "ToDo" && t.titulo.StartsWith("orden-")).Select(t => t.id).ToList();
        Assert.Equal(new[] { c, a, b }, resto);
    }

    [Fact]
    public async Task Mover_a_otra_columna_la_coloca_en_la_posicion_pedida()
    {
        var http = await api.DemoAsync();
        var x = await Crear(http, 2, new { titulo = "mov-X" });
        var y = await Crear(http, 2, new { titulo = "mov-Y" });
        await http.PatchAsJsonAsync($"/api/v1/tareas/{x}/mover", new { estado = "Done", posicion = 0 });
        await http.PatchAsJsonAsync($"/api/v1/tareas/{y}/mover", new { estado = "Done", posicion = 0 });

        var hecho = (await Listar(http, 2)).Where(t => t.estado == "Done").Select(t => t.id).ToList();
        Assert.Equal(new[] { y, x }, hecho.Take(2));
    }

    [Fact]
    public async Task El_Viewer_no_puede_mover_tareas()
    {
        var http = await api.DemoAsync();
        var tarea = (await Listar(http, 3)).First().id; // proyecto 3: Viewer
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PatchAsJsonAsync($"/api/v1/tareas/{tarea}/mover", new { estado = "Done", posicion = 0 })).StatusCode);
    }

    [Fact]
    public async Task El_filtro_vencidas_solo_trae_las_pasadas_que_no_estan_hechas()
    {
        var http = await api.DemoAsync();
        var ayer = DateTime.UtcNow.AddDays(-1);
        var manana = DateTime.UtcNow.AddDays(1);
        var vencida = await Crear(http, 2, new { titulo = "venc-pasada", fechaVencimiento = ayer });
        var hecha = await Crear(http, 2, new { titulo = "venc-hecha", fechaVencimiento = ayer });
        await Crear(http, 2, new { titulo = "venc-futura", fechaVencimiento = manana });
        await Crear(http, 2, new { titulo = "venc-sin-fecha" });
        await http.PatchAsJsonAsync($"/api/v1/tareas/{hecha}/estado", new { estado = "Done" });

        var resultado = await Listar(http, 2, "&vencidas=true");
        var titulos = resultado.Select(t => t.titulo).ToList();
        Assert.Contains("venc-pasada", titulos);
        Assert.DoesNotContain("venc-hecha", titulos);
        Assert.DoesNotContain("venc-futura", titulos);
        Assert.DoesNotContain("venc-sin-fecha", titulos);
        Assert.All(resultado, t => Assert.NotEqual("Done", t.estado));
        Assert.Contains(vencida, resultado.Select(t => t.id));
    }
}

[Collection("api")]
public class AvisosTests(ApiFactory api)
{
    private static async Task<JsonElement> Avisos(HttpClient h) => await h.GetFromJsonAsync<JsonElement>("/api/v1/notificaciones");

    [Fact]
    public async Task Asignar_una_tarea_avisa_a_quien_la_recibe_y_se_puede_marcar_leida()
    {
        var demo = await api.DemoAsync();
        var ana = await api.ComoAsync("ana@demo.com", "ana123");
        var antes = (await Avisos(ana)).GetProperty("noLeidas").GetInt32();

        var r = await demo.PostAsJsonAsync("/api/v1/proyectos/1/tareas", new { titulo = "aviso-1", asignadoAId = 2 });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var despues = await Avisos(ana);
        Assert.Equal(antes + 1, despues.GetProperty("noLeidas").GetInt32());
        var primero = despues.GetProperty("items")[0];
        Assert.Contains("aviso-1", primero.GetProperty("mensaje").GetString());
        Assert.Contains("Diego Morales", primero.GetProperty("mensaje").GetString());

        var id = primero.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await ana.PostAsync($"/api/v1/notificaciones/{id}/leida", null)).StatusCode);
        Assert.Equal(antes, (await Avisos(ana)).GetProperty("noLeidas").GetInt32());
    }

    [Fact]
    public async Task Reasignar_avisa_al_nuevo_y_asignarse_a_si_mismo_no_avisa()
    {
        var demo = await api.DemoAsync();
        var luis = await api.ComoAsync("luis@demo.com", "luis123");
        var antesDemo = (await Avisos(demo)).GetProperty("noLeidas").GetInt32();
        var antesLuis = (await Avisos(luis)).GetProperty("noLeidas").GetInt32();

        var creada = await (await demo.PostAsJsonAsync("/api/v1/proyectos/1/tareas", new { titulo = "aviso-2", asignadoAId = 1 })).Content.ReadFromJsonAsync<JsonElement>();
        var id = creada.GetProperty("id").GetInt32();
        Assert.Equal(antesDemo, (await Avisos(demo)).GetProperty("noLeidas").GetInt32()); // se la asignó a sí mismo

        var put = await demo.PutAsJsonAsync($"/api/v1/tareas/{id}", new { titulo = "aviso-2", prioridad = "Media", asignadoAId = 3 });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(antesLuis + 1, (await Avisos(luis)).GetProperty("noLeidas").GetInt32());

        // editar sin cambiar al asignado no repite el aviso
        await demo.PutAsJsonAsync($"/api/v1/tareas/{id}", new { titulo = "aviso-2b", prioridad = "Alta", asignadoAId = 3 });
        Assert.Equal(antesLuis + 1, (await Avisos(luis)).GetProperty("noLeidas").GetInt32());
    }

    [Fact]
    public async Task Nadie_puede_marcar_ni_ver_los_avisos_de_otro()
    {
        var demo = await api.DemoAsync();
        var ana = await api.ComoAsync("ana@demo.com", "ana123");
        var luis = await api.ComoAsync("luis@demo.com", "luis123");
        await demo.PostAsJsonAsync("/api/v1/proyectos/1/tareas", new { titulo = "aviso-3", asignadoAId = 2 });
        var idAna = (await Avisos(ana)).GetProperty("items")[0].GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.NotFound, (await luis.PostAsync($"/api/v1/notificaciones/{idAna}/leida", null)).StatusCode);
        Assert.DoesNotContain(idAna, (await Avisos(luis)).GetProperty("items").EnumerateArray().Select(n => n.GetProperty("id").GetInt32()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/v1/notificaciones")).StatusCode);
    }
}
