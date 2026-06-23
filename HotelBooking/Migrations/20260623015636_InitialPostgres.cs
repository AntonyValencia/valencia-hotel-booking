using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelBooking.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Habitaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    PrecioPorNoche = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    Servicios = table.Column<string>(type: "text", nullable: true),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habitaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Habitaciones_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImagenesHabitacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UrlImagen = table.Column<string>(type: "text", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    HabitacionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesHabitacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagenesHabitacion_Habitaciones_HabitacionId",
                        column: x => x.HabitacionId,
                        principalTable: "Habitaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    HabitacionId = table.Column<int>(type: "integer", nullable: false),
                    FechaEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalPagar = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    FechaReserva = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notas = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Habitaciones_HabitacionId",
                        column: x => x.HabitacionId,
                        principalTable: "Habitaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Descripcion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Habitación para una persona, cómoda y acogedora", "Individual" },
                    { 2, "Habitación para dos personas con cama doble o twin", "Doble" },
                    { 3, "Suite de lujo con sala de estar y vista panorámica", "Suite" },
                    { 4, "Habitación amplia para familias, ideal para 4 personas", "Familiar" }
                });

            migrationBuilder.InsertData(
                table: "Habitaciones",
                columns: new[] { "Id", "Capacidad", "CategoriaId", "Descripcion", "Disponible", "Nombre", "PrecioPorNoche", "Servicios" },
                values: new object[,]
                {
                    { 1, 1, 1, "Acogedora habitación individual con todas las comodidades. Perfecta para viajeros de negocios.", true, "Habitación Individual Estándar", 250m, "WiFi Gratis, TV 40\", Aire Acondicionado, Baño Privado, Frigobar" },
                    { 2, 2, 2, "Espaciosa habitación doble con vista a la ciudad. Ideal para parejas o compañeros de viaje.", true, "Habitación Doble Superior", 380m, "WiFi Gratis, TV 50\", Aire Acondicionado, Baño Privado, Frigobar, Balcón" },
                    { 3, 2, 3, "Lujosa suite con sala de estar separada y vista panorámica al valle de Cochabamba.", true, "Suite Ejecutiva", 750m, "WiFi Gratis, Smart TV 65\", Jacuzzi, Sala de Estar, Cocina Equipada, Vista Panorámica" },
                    { 4, 4, 4, "Amplia suite familiar con dos habitaciones, perfecta para familias con niños.", true, "Suite Familiar Deluxe", 950m, "WiFi Gratis, 2 TVs, Aire Acondicionado, 2 Baños, Sala Familiar, Desayuno Incluido" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Habitaciones_CategoriaId",
                table: "Habitaciones",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesHabitacion_HabitacionId",
                table: "ImagenesHabitacion",
                column: "HabitacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_HabitacionId",
                table: "Reservas",
                column: "HabitacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagenesHabitacion");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Habitaciones");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Categorias");
        }
    }
}
