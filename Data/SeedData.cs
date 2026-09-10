using GestorTareas.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace GestorTareas.Api.Data;

public static class SeedData
{
    public static void Seed(ApplicationDbContext context)
    {
        Console.WriteLine("[SEED] 0. Iniciando seed...");

        if (context.Usuarios.Any())
        {
            Console.WriteLine($"[SEED] La base ya tiene {context.Usuarios.Count()} usuarios. Omitiendo seed (idempotencia).");
            return;
        }

        try
        {
            var hasher = new PasswordHasher<Usuario>();
            var now = DateTime.UtcNow;

            // 1. USUARIOS
            Console.WriteLine("[SEED] 1. Creando usuarios...");
            var diego = new Usuario { Nombre = "Diego Morales", Email = "demo@demo.com", FechaRegistro = now.AddDays(-92) };
            diego.PasswordHash = hasher.HashPassword(diego, "demo123");

            var ana = new Usuario { Nombre = "Ana Torres", Email = "ana@demo.com", FechaRegistro = now.AddDays(-80) };
            ana.PasswordHash = hasher.HashPassword(ana, "ana123");

            var luis = new Usuario { Nombre = "Luis Vega", Email = "luis@demo.com", FechaRegistro = now.AddDays(-64) };
            luis.PasswordHash = hasher.HashPassword(luis, "luis123");

            // ── USUARIO AÑADIDO: Joel ─────────────────────────────────────────────
            var joel = new Usuario { Nombre = "Joel Ramírez", Email = "joel@demo.com", FechaRegistro = now.AddDays(-50) };
            joel.PasswordHash = hasher.HashPassword(joel, "joel123");
            // ─────────────────────────────────────────────────────────────────────

            context.Usuarios.AddRange(diego, ana, luis, joel);
            context.SaveChanges();
            Console.WriteLine($"[SEED] 1. Usuarios creados: 4 (IDs {diego.Id}, {ana.Id}, {luis.Id}, {joel.Id})");

            // 2. PROYECTOS
            Console.WriteLine("[SEED] 2. Creando proyectos...");
            var p1 = new Proyecto { Nombre = "Rediseño Web Corporativa", Descripcion = "Renovación completa del sitio institucional: sistema de diseño, landing y CMS.", Color = "#FF5A36", FechaCreacion = now.AddDays(-40), PropietarioId = diego.Id };
            var p2 = new Proyecto { Nombre = "App Móvil de Inventarios", Descripcion = "App de escaneo y sincronización offline de inventarios para bodega.", Color = "#0FA48A", FechaCreacion = now.AddDays(-30), PropietarioId = ana.Id };
            var p3 = new Proyecto { Nombre = "Plan de Marketing Q3", Descripcion = "Calendario editorial, campañas de email y reporte de métricas del trimestre.", Color = "#2F6FED", FechaCreacion = now.AddDays(-20), PropietarioId = luis.Id };

            context.Proyectos.AddRange(p1, p2, p3);
            context.SaveChanges();
            Console.WriteLine($"[SEED] 2. Proyectos creados: 3 (IDs {p1.Id}, {p2.Id}, {p3.Id})");

            // 3. MIEMBROS
            Console.WriteLine("[SEED] 3. Creando miembros...");
            context.MiembrosProyecto.AddRange(
                // Proyecto 1 — Rediseño Web
                new MiembroProyecto { ProyectoId = p1.Id, UsuarioId = diego.Id, Rol = RolProyecto.Owner,  FechaIngreso = now.AddDays(-40) },
                new MiembroProyecto { ProyectoId = p1.Id, UsuarioId = ana.Id,   Rol = RolProyecto.Editor, FechaIngreso = now.AddDays(-39) },
                new MiembroProyecto { ProyectoId = p1.Id, UsuarioId = luis.Id,  Rol = RolProyecto.Viewer, FechaIngreso = now.AddDays(-38) },
                new MiembroProyecto { ProyectoId = p1.Id, UsuarioId = joel.Id,  Rol = RolProyecto.Editor, FechaIngreso = now.AddDays(-35) }, // ← Joel

                // Proyecto 2 — App Móvil
                new MiembroProyecto { ProyectoId = p2.Id, UsuarioId = ana.Id,   Rol = RolProyecto.Owner,  FechaIngreso = now.AddDays(-30) },
                new MiembroProyecto { ProyectoId = p2.Id, UsuarioId = diego.Id, Rol = RolProyecto.Editor, FechaIngreso = now.AddDays(-29) },
                new MiembroProyecto { ProyectoId = p2.Id, UsuarioId = joel.Id,  Rol = RolProyecto.Editor, FechaIngreso = now.AddDays(-25) }, // ← Joel

                // Proyecto 3 — Marketing Q3
                new MiembroProyecto { ProyectoId = p3.Id, UsuarioId = luis.Id,  Rol = RolProyecto.Owner,  FechaIngreso = now.AddDays(-20) },
                new MiembroProyecto { ProyectoId = p3.Id, UsuarioId = diego.Id, Rol = RolProyecto.Viewer, FechaIngreso = now.AddDays(-19) },
                new MiembroProyecto { ProyectoId = p3.Id, UsuarioId = joel.Id,  Rol = RolProyecto.Viewer, FechaIngreso = now.AddDays(-15) }  // ← Joel
            );
            context.SaveChanges();
            Console.WriteLine("[SEED] 3. Miembros creados: 10");

            // 4. TAREAS
            Console.WriteLine("[SEED] 4. Creando tareas...");
            var t1  = new Tarea { Titulo = "Definir sistema de diseño",      Descripcion = "Tokens de color, tipografías y componentes base.",                                      Estado = EstadoTarea.Done,       Prioridad = PrioridadTarea.Alta,  FechaVencimiento = now.AddDays(-12), ProyectoId = p1.Id, AsignadoAId = ana.Id   };
            var t2  = new Tarea { Titulo = "Wireframes de landing",          Descripcion = "Propuestas de jerarquía para hero, beneficios y CTA.",                                  Estado = EstadoTarea.Done,       Prioridad = PrioridadTarea.Media, FechaVencimiento = now.AddDays(-8),  ProyectoId = p1.Id, AsignadoAId = diego.Id };
            var t3  = new Tarea { Titulo = "Maquetación de página de inicio",Descripcion = "HTML/CSS responsivo con componentes del sistema de diseño.",                            Estado = EstadoTarea.InProgress, Prioridad = PrioridadTarea.Alta,  FechaVencimiento = now.AddDays(3),   ProyectoId = p1.Id, AsignadoAId = ana.Id   };
            var t4  = new Tarea { Titulo = "Integración con CMS",            Descripcion = "Conectar secciones editables con el headless CMS.",                                     Estado = EstadoTarea.InProgress, Prioridad = PrioridadTarea.Media, FechaVencimiento = now.AddDays(-2),  ProyectoId = p1.Id, AsignadoAId = luis.Id  };
            var t5  = new Tarea { Titulo = "Auditoría SEO inicial",          Descripcion = "Lighthouse, meta tags, datos estructurados y performance.",                             Estado = EstadoTarea.ToDo,       Prioridad = PrioridadTarea.Media, FechaVencimiento = now.AddDays(7),   ProyectoId = p1.Id, AsignadoAId = diego.Id };
            var t6  = new Tarea { Titulo = "Pruebas de accesibilidad",       Descripcion = "Contraste, foco visible, navegación por teclado.",                                      Estado = EstadoTarea.ToDo,       Prioridad = PrioridadTarea.Baja,  FechaVencimiento = now.AddDays(10),  ProyectoId = p1.Id, AsignadoAId = null     };
            var t7  = new Tarea { Titulo = "Migración de contenidos",        Descripcion = "Mover textos y assets del sitio legacy al nuevo CMS.",                                  Estado = EstadoTarea.ToDo,       Prioridad = PrioridadTarea.Alta,  FechaVencimiento = now.AddDays(6),   ProyectoId = p1.Id, AsignadoAId = ana.Id   };
            var t8  = new Tarea { Titulo = "Diseño de pantalla de escáner",  Descripcion = "Flujo de escaneo de código de barras con feedback.",                                    Estado = EstadoTarea.InProgress, Prioridad = PrioridadTarea.Alta,  FechaVencimiento = now.AddDays(2),   ProyectoId = p2.Id, AsignadoAId = diego.Id };
            var t9  = new Tarea { Titulo = "API de sincronización offline",  Descripcion = "Cola local de operaciones y reconciliación al reconectar.",                             Estado = EstadoTarea.ToDo,       Prioridad = PrioridadTarea.Alta,  FechaVencimiento = now.AddDays(8),   ProyectoId = p2.Id, AsignadoAId = ana.Id   };
            var t10 = new Tarea { Titulo = "Tests de integración",           Descripcion = "Cobertura de flujos críticos de inventario.",                                           Estado = EstadoTarea.ToDo,       Prioridad = PrioridadTarea.Baja,  FechaVencimiento = null,             ProyectoId = p2.Id, AsignadoAId = null     };
            var t11 = new Tarea { Titulo = "Publicar build beta",            Descripcion = "Distribución interna por TestFlight / Play Console.",                                   Estado = EstadoTarea.Done,       Prioridad = PrioridadTarea.Media, FechaVencimiento = now.AddDays(-4),  ProyectoId = p2.Id, AsignadoAId = diego.Id };
            var t12 = new Tarea { Titulo = "Calendario de contenidos",       Descripcion = "Planificación de publicaciones de julio a septiembre.",                                 Estado = EstadoTarea.InProgress, Prioridad = PrioridadTarea.Media, FechaVencimiento = now.AddDays(4),   ProyectoId = p3.Id, AsignadoAId = luis.Id  };
            var t13 = new Tarea { Titulo = "Campaña de email",               Descripcion = "Secuencia de 3 correos para reactivación de clientes.",                                Estado = EstadoTarea.ToDo,       Prioridad = PrioridadTarea.Baja,  FechaVencimiento = now.AddDays(5),   ProyectoId = p3.Id, AsignadoAId = luis.Id  };
            var t14 = new Tarea { Titulo = "Informe de métricas",            Descripcion = "KPIs de junio: CAC, conversión y engagement.",                                         Estado = EstadoTarea.Done,       Prioridad = PrioridadTarea.Media, FechaVencimiento = now.AddDays(-1),  ProyectoId = p3.Id, AsignadoAId = luis.Id  };

            context.Tareas.AddRange(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
            context.SaveChanges();
            Console.WriteLine("[SEED] 4. Tareas creadas: 14");

            // 5. COMENTARIOS
            Console.WriteLine("[SEED] 5. Creando comentarios...");
            context.Comentarios.AddRange(
                new Comentario { TareaId = t3.Id, UsuarioId = ana.Id,   Contenido = "Subí la primera propuesta del hero. El CTA principal usa el coral de marca.", FechaCreacion = now.AddDays(-2) },
                new Comentario { TareaId = t3.Id, UsuarioId = diego.Id, Contenido = "¡Muy bien! Ajustemos el contraste del subtítulo y queda listo para review.",   FechaCreacion = now.AddDays(-1) },
                new Comentario { TareaId = t8.Id, UsuarioId = ana.Id,   Contenido = "Ojo: en Android 12 el botón de escanear queda tapado por la barra de gestos.", FechaCreacion = now.AddDays(-3) }
            );
            context.SaveChanges();
            Console.WriteLine("[SEED] 5. Comentarios creados: 3");

            // 6. ADJUNTOS
            Console.WriteLine("[SEED] 6. Creando adjuntos...");
            context.Adjuntos.AddRange(
                new Adjunto { TareaId = t3.Id, NombreArchivo = "propuesta-hero.pdf",   RutaRelativa = "/uploads/propuesta-hero.pdf",   TamanoBytes = 1_248_576, FechaSubida = now.AddDays(-2) },
                new Adjunto { TareaId = t3.Id, NombreArchivo = "paleta-colores.png",   RutaRelativa = "/uploads/paleta-colores.png",   TamanoBytes =   348_160, FechaSubida = now.AddDays(-2) }
            );
            context.SaveChanges();
            Console.WriteLine("[SEED] 6. Adjuntos creados: 2");

            Console.WriteLine("[SEED] Seed completado con éxito.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SEED ERROR] {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
                Console.WriteLine($"[SEED ERROR INNER] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            throw;
        }
    }
}