using HotelBooking.Data;
using HotelBooking.Models;
using HotelBooking.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers
{
    public class AuthController : Controller
    {
        private readonly HotelDbContext _context;

        public AuthController(HotelDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // LOGIN
        // ============================================================
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UsuarioId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Activo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos");
                return View(model);
            }

            // Guardar sesión
            HttpContext.Session.SetString("UsuarioId", usuario.Id.ToString());
            HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol);
            HttpContext.Session.SetString("UsuarioEmail", usuario.Email);

            if (usuario.Rol == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // REGISTRO
        // ============================================================
        [HttpGet]
        public IActionResult Registro()
        {
            if (HttpContext.Session.GetString("UsuarioId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Verificar email único
            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con ese email");
                return View(model);
            }

            var usuario = new Usuario
            {
                Nombre = model.Nombre.Trim(),
                Email = model.Email.ToLower().Trim(),
                Telefono = model.Telefono,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Rol = "Cliente",
                FechaRegistro = DateTime.Now,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Iniciar sesión automáticamente
            HttpContext.Session.SetString("UsuarioId", usuario.Id.ToString());
            HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol);
            HttpContext.Session.SetString("UsuarioEmail", usuario.Email);

            TempData["Exito"] = $"¡Bienvenido {usuario.Nombre}! Tu cuenta fue creada exitosamente.";
            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // LOGOUT
        // ============================================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
