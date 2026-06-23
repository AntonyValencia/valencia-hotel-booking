using HotelBooking.Data;
using HotelBooking.Models;
using HotelBooking.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers
{
    public class AdminController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(HotelDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Verificar rol Admin
        private bool EsAdmin() => HttpContext.Session.GetString("UsuarioRol") == "Admin";

        private IActionResult AccesoDenegado()
        {
            TempData["Error"] = "No tienes permisos para acceder a esta sección";
            return RedirectToAction("Login", "Auth");
        }

        // ============================================================
        // DASHBOARD
        // ============================================================
        public async Task<IActionResult> Dashboard()
        {
            if (!EsAdmin()) return AccesoDenegado();

            var ahora = DateTime.Now;
            var model = new DashboardViewModel
            {
                TotalHabitaciones = await _context.Habitaciones.CountAsync(h => h.Disponible),
                TotalReservas = await _context.Reservas.CountAsync(),
                ReservasPendientes = await _context.Reservas.CountAsync(r => r.Estado == "Pendiente"),
                ReservasConfirmadas = await _context.Reservas.CountAsync(r => r.Estado == "Confirmada"),
                IngresosMes = await _context.Reservas
                    .Where(r => r.Estado != "Cancelada" && r.FechaReserva.Month == ahora.Month && r.FechaReserva.Year == ahora.Year)
                    .SumAsync(r => r.TotalPagar),
                UltimasReservas = await _context.Reservas
                    .Include(r => r.Usuario)
                    .Include(r => r.Habitacion)
                    .OrderByDescending(r => r.FechaReserva)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }

        // ============================================================
        // GESTIÓN DE HABITACIONES
        // ============================================================
        public async Task<IActionResult> Habitaciones()
        {
            if (!EsAdmin()) return AccesoDenegado();

            var habitaciones = await _context.Habitaciones
                .Include(h => h.Categoria)
                .Include(h => h.Imagenes)
                .ToListAsync();

            return View(habitaciones);
        }

        [HttpGet]
        public async Task<IActionResult> CrearHabitacion()
        {
            if (!EsAdmin()) return AccesoDenegado();
            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
            return View(new Habitacion());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearHabitacion(Habitacion model, List<IFormFile> imagenes)
        {
            if (!EsAdmin()) return AccesoDenegado();

            // Remover validaciones de navegación
            ModelState.Remove("Categoria");
            ModelState.Remove("Imagenes");
            ModelState.Remove("Reservas");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
                return View(model);
            }

            _context.Habitaciones.Add(model);
            await _context.SaveChangesAsync();

            // Guardar imágenes subidas
            if (imagenes != null && imagenes.Any())
            {
                bool primera = true;
                foreach (var imagen in imagenes.Where(i => i.Length > 0))
                {
                    var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                    var ruta = Path.Combine(_env.WebRootPath, "images", "habitaciones", nombreArchivo);

                    Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
                    using var stream = new FileStream(ruta, FileMode.Create);
                    await imagen.CopyToAsync(stream);

                    _context.ImagenesHabitacion.Add(new ImagenHabitacion
                    {
                        HabitacionId = model.Id,
                        UrlImagen = $"/images/habitaciones/{nombreArchivo}",
                        EsPrincipal = primera
                    });
                    primera = false;
                }
                await _context.SaveChangesAsync();
            }

            TempData["Exito"] = $"Habitación '{model.Nombre}' creada exitosamente";
            return RedirectToAction("Habitaciones");
        }

        [HttpGet]
        public async Task<IActionResult> EditarHabitacion(int id)
        {
            if (!EsAdmin()) return AccesoDenegado();

            var habitacion = await _context.Habitaciones
                .Include(h => h.Imagenes)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (habitacion == null) return NotFound();

            ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre", habitacion.CategoriaId);
            return View(habitacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarHabitacion(int id, Habitacion model, List<IFormFile> nuevasImagenes)
        {
            if (!EsAdmin()) return AccesoDenegado();

            ModelState.Remove("Categoria");
            ModelState.Remove("Imagenes");
            ModelState.Remove("Reservas");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
                return View(model);
            }

            var habitacion = await _context.Habitaciones.FindAsync(id);
            if (habitacion == null) return NotFound();

            habitacion.Nombre = model.Nombre;
            habitacion.Descripcion = model.Descripcion;
            habitacion.PrecioPorNoche = model.PrecioPorNoche;
            habitacion.Capacidad = model.Capacidad;
            habitacion.Disponible = model.Disponible;
            habitacion.Servicios = model.Servicios;
            habitacion.CategoriaId = model.CategoriaId;

            // Agregar nuevas imágenes
            if (nuevasImagenes != null && nuevasImagenes.Any())
            {
                foreach (var imagen in nuevasImagenes.Where(i => i.Length > 0))
                {
                    var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                    var ruta = Path.Combine(_env.WebRootPath, "images", "habitaciones", nombreArchivo);
                    Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
                    using var stream = new FileStream(ruta, FileMode.Create);
                    await imagen.CopyToAsync(stream);

                    _context.ImagenesHabitacion.Add(new ImagenHabitacion
                    {
                        HabitacionId = id,
                        UrlImagen = $"/images/habitaciones/{nombreArchivo}",
                        EsPrincipal = false
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Exito"] = "Habitación actualizada correctamente";
            return RedirectToAction("Habitaciones");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarHabitacion(int id)
        {
            if (!EsAdmin()) return AccesoDenegado();

            var habitacion = await _context.Habitaciones
                .Include(h => h.Reservas)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (habitacion == null) return NotFound();

            if (habitacion.Reservas.Any(r => r.Estado == "Confirmada" || r.Estado == "CheckIn"))
            {
                TempData["Error"] = "No puedes eliminar una habitación con reservas activas";
                return RedirectToAction("Habitaciones");
            }

            _context.Habitaciones.Remove(habitacion);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Habitación eliminada correctamente";
            return RedirectToAction("Habitaciones");
        }

        // ============================================================
        // GESTIÓN DE RESERVAS
        // ============================================================
        public async Task<IActionResult> Reservas(string? estado)
        {
            if (!EsAdmin()) return AccesoDenegado();

            var query = _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Habitacion)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado == estado);

            var reservas = await query.OrderByDescending(r => r.FechaReserva).ToListAsync();
            ViewBag.EstadoFiltro = estado;
            return View(reservas);
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoReserva(int id, string nuevoEstado)
        {
            if (!EsAdmin()) return AccesoDenegado();

            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return NotFound();

            reserva.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Estado de reserva #{id} cambiado a '{nuevoEstado}'";
            return RedirectToAction("Reservas");
        }

        // ============================================================
        // ELIMINAR IMAGEN
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> EliminarImagen(int imagenId, int habitacionId)
        {
            if (!EsAdmin()) return AccesoDenegado();

            var imagen = await _context.ImagenesHabitacion.FindAsync(imagenId);
            if (imagen != null)
            {
                // Eliminar archivo físico
                var rutaFisica = Path.Combine(_env.WebRootPath, imagen.UrlImagen.TrimStart('/'));
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);

                _context.ImagenesHabitacion.Remove(imagen);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("EditarHabitacion", new { id = habitacionId });
        }
    }
}
