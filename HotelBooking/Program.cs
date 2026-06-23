using HotelBooking.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICIOS
// ============================================================
builder.Services.AddControllersWithViews();

// Entity Framework Core + SQL Server
// En producción (Render) usa la variable de entorno AZURE_SQL_CONNECTION.
// En local usa la cadena de appsettings.json.
var connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION")
    ?? builder.Configuration.GetConnectionString("HotelConnection");

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseNpgsql(connectionString));

// Sesiones para manejo de login
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Render asigna el puerto dinámicamente vía variable de entorno PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

// ============================================================
// MIDDLEWARE
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// En Render, el proxy ya maneja HTTPS — evitamos redirect loops
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // IMPORTANTE: antes de Authorization
app.UseAuthorization();

// Ruta principal
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ============================================================
// EJECUTAR MIGRACIONES AUTOMÁTICAMENTE AL INICIO
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    db.Database.Migrate();

    // Crear el usuario Admin si no existe (con hash real generado en C#)
    if (!db.Usuarios.Any(u => u.Email == "admin@hotelbooking.com"))
    {
        db.Usuarios.Add(new HotelBooking.Models.Usuario
        {
            Nombre = "Administrador",
            Email = "admin@hotelbooking.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Rol = "Admin",
            Telefono = "77712345",
            FechaRegistro = DateTime.Now,
            Activo = true
        });
        db.SaveChanges();
    }
}

app.Run();