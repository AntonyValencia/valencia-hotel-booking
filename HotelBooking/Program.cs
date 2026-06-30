using HotelBooking.Data;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION")
    ?? builder.Configuration.GetConnectionString("HotelConnection");

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<HotelBooking.Services.ImgBbService>();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    db.Database.Migrate();

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
