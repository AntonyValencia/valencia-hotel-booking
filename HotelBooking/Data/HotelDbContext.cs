using HotelBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Data
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }

        // Tablas de la base de datos
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Habitacion> Habitaciones { get; set; }
        public DbSet<ImagenHabitacion> ImagenesHabitacion { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PostgreSQL necesita precisión explícita para columnas decimal
            modelBuilder.Entity<Habitacion>()
                .Property(h => h.PrecioPorNoche)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Reserva>()
                .Property(r => r.TotalPagar)
                .HasPrecision(10, 2);

            // Relación: Habitacion -> Categoria
            modelBuilder.Entity<Habitacion>()
                .HasOne(h => h.Categoria)
                .WithMany(c => c.Habitaciones)
                .HasForeignKey(h => h.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación: ImagenHabitacion -> Habitacion
            modelBuilder.Entity<ImagenHabitacion>()
                .HasOne(i => i.Habitacion)
                .WithMany(h => h.Imagenes)
                .HasForeignKey(i => i.HabitacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación: Reserva -> Usuario
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación: Reserva -> Habitacion
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Habitacion)
                .WithMany(h => h.Reservas)
                .HasForeignKey(r => r.HabitacionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Email único para usuarios
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ============================================================
            // DATOS INICIALES (Seed Data)
            // ============================================================

            // Categorías
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { Id = 1, Nombre = "Individual", Descripcion = "Habitación para una persona, cómoda y acogedora" },
                new Categoria { Id = 2, Nombre = "Doble", Descripcion = "Habitación para dos personas con cama doble o twin" },
                new Categoria { Id = 3, Nombre = "Suite", Descripcion = "Suite de lujo con sala de estar y vista panorámica" },
                new Categoria { Id = 4, Nombre = "Familiar", Descripcion = "Habitación amplia para familias, ideal para 4 personas" }
            );

            // NOTA: El usuario Admin ya NO se crea aquí (HasData no puede
            // ejecutar BCrypt.HashPassword porque debe ser un valor fijo).
            // El Admin se crea automáticamente en Program.cs al iniciar la app.

            // Habitaciones de ejemplo
            modelBuilder.Entity<Habitacion>().HasData(
                new Habitacion
                {
                    Id = 1,
                    Nombre = "Habitación Individual Estándar",
                    CategoriaId = 1,
                    Descripcion = "Acogedora habitación individual con todas las comodidades. Perfecta para viajeros de negocios.",
                    PrecioPorNoche = 250,
                    Capacidad = 1,
                    Disponible = true,
                    Servicios = "WiFi Gratis, TV 40\", Aire Acondicionado, Baño Privado, Frigobar"
                },
                new Habitacion
                {
                    Id = 2,
                    Nombre = "Habitación Doble Superior",
                    CategoriaId = 2,
                    Descripcion = "Espaciosa habitación doble con vista a la ciudad. Ideal para parejas o compañeros de viaje.",
                    PrecioPorNoche = 380,
                    Capacidad = 2,
                    Disponible = true,
                    Servicios = "WiFi Gratis, TV 50\", Aire Acondicionado, Baño Privado, Frigobar, Balcón"
                },
                new Habitacion
                {
                    Id = 3,
                    Nombre = "Suite Ejecutiva",
                    CategoriaId = 3,
                    Descripcion = "Lujosa suite con sala de estar separada y vista panorámica al valle de Cochabamba.",
                    PrecioPorNoche = 750,
                    Capacidad = 2,
                    Disponible = true,
                    Servicios = "WiFi Gratis, Smart TV 65\", Jacuzzi, Sala de Estar, Cocina Equipada, Vista Panorámica"
                },
                new Habitacion
                {
                    Id = 4,
                    Nombre = "Suite Familiar Deluxe",
                    CategoriaId = 4,
                    Descripcion = "Amplia suite familiar con dos habitaciones, perfecta para familias con niños.",
                    PrecioPorNoche = 950,
                    Capacidad = 4,
                    Disponible = true,
                    Servicios = "WiFi Gratis, 2 TVs, Aire Acondicionado, 2 Baños, Sala Familiar, Desayuno Incluido"
                }
            );
        }
    }
}