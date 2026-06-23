using HotelBooking.Data;
using HotelBooking.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers
{
    public class ClienteController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ClienteController(HotelDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private bool EstaLogueado() => HttpContext.Session.GetString("UsuarioId") != null;
        private int GetUsuarioId() => int.Parse(HttpContext.Session.GetString("UsuarioId")!);

        // ============================================================
        // MIS RESERVAS
        // ============================================================
        public async Task<IActionResult> MisReservas(string? estado)
        {
            if (!EstaLogueado())
                return RedirectToAction("Login", "Auth");

            var query = _context.Reservas
                .Include(r => r.Habitacion)
                    .ThenInclude(h => h!.Imagenes)
                .Include(r => r.Habitacion)
                    .ThenInclude(h => h!.Categoria)
                .Where(r => r.UsuarioId == GetUsuarioId())
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado == estado);

            var reservas = await query.OrderByDescending(r => r.FechaReserva).ToListAsync();

            ViewBag.EstadoFiltro = estado;
            return View(reservas);
        }

        // ============================================================
        // PERFIL
        // ============================================================
        public async Task<IActionResult> Perfil()
        {
            if (!EstaLogueado())
                return RedirectToAction("Login", "Auth");

            var usuario = await _context.Usuarios.FindAsync(GetUsuarioId());
            if (usuario == null) return RedirectToAction("Logout", "Auth");

            var model = new EditarPerfilViewModel
            {
                Nombre = usuario.Nombre,
                Telefono = usuario.Telefono
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(EditarPerfilViewModel model)
        {
            if (!EstaLogueado())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            var usuario = await _context.Usuarios.FindAsync(GetUsuarioId());
            if (usuario == null) return RedirectToAction("Logout", "Auth");

            usuario.Nombre = model.Nombre.Trim();
            usuario.Telefono = model.Telefono;

            // Cambiar contraseña si se proporcionó
            if (!string.IsNullOrEmpty(model.NuevaPassword))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);
            }

            await _context.SaveChangesAsync();

            // Actualizar sesión con nuevo nombre
            HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);

            TempData["Exito"] = "Tu perfil fue actualizado correctamente";
            return RedirectToAction("Perfil");
        }
    }
}
