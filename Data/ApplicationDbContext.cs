using GestorTareas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<MiembroProyecto> MiembrosProyecto => Set<MiembroProyecto>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<Comentario> Comentarios => Set<Comentario>();
    public DbSet<Adjunto> Adjuntos => Set<Adjunto>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
        });

        // Proyecto
        modelBuilder.Entity<Proyecto>(e =>
        {
            e.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
            e.HasOne(p => p.Propietario)
                .WithMany(u => u.ProyectosPropios)
                .HasForeignKey(p => p.PropietarioId)
                .OnDelete(DeleteBehavior.Restrict); // el owner no se borra en cascada
        });

        // MiembroProyecto: clave compuesta
        modelBuilder.Entity<MiembroProyecto>(e =>
        {
            e.HasKey(m => new { m.ProyectoId, m.UsuarioId });

            e.HasOne(m => m.Proyecto)
                .WithMany(p => p.Miembros)
                .HasForeignKey(m => m.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Usuario)
                .WithMany(u => u.Membresias)
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Tarea
        modelBuilder.Entity<Tarea>(e =>
        {
            e.Property(t => t.Titulo).HasMaxLength(200).IsRequired();

            e.HasOne(t => t.Proyecto)
                .WithMany(p => p.Tareas)
                .HasForeignKey(t => t.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.AsignadoA)
                .WithMany()
                .HasForeignKey(t => t.AsignadoAId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Comentario
        modelBuilder.Entity<Comentario>(e =>
        {
            e.Property(c => c.Contenido).HasMaxLength(2000).IsRequired();

            e.HasOne(c => c.Tarea)
                .WithMany(t => t.Comentarios)
                .HasForeignKey(c => c.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Adjunto
        modelBuilder.Entity<Adjunto>(e =>
        {
            e.Property(a => a.NombreArchivo).HasMaxLength(255).IsRequired();

            e.HasOne(a => a.Tarea)
                .WithMany(t => t.Adjuntos)
                .HasForeignKey(a => a.TareaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(r => r.Token).IsUnique();

            e.HasOne(r => r.Usuario)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
