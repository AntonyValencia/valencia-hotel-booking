using HotelBooking.Data;
using HotelBooking.Models;
using HotelBooking.Models.ViewModels;
using HotelBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers
{
    public class AdminController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly ImgBbService _imgBb;

        public AdminController(HotelDbContext context, ImgBbService imgBb)
        {
            _context = context;
            _imgBb = imgBb;
        }

        private bool EsAdmin() => HttpContext.Session.GetString("UsuarioRol") == "Admin";
        private IActionResult AccesoDenegado()
        {
            TempData["Error"] = "No tienes permisos para acceder a esta sección";
            return RedirectToAction("Login", "Auth");
        }

        // ── DASHBOARD ────────────────────────────────────────────────
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
                    .Where(r => r.Estado != "Cancelada"
                             && r.FechaReserva.Month == ahora.Month
                             && r.FechaReserva.Year == ahora.Year)
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

        // ── HABITACIONES ─────────────────────────────────────────────
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
            ModelState.Remove("Categoria"); ModelState.Remove("Imagenes"); ModelState.Remove("Reservas");

            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(await _context.Categorias.ToListAsync(), "Id", "Nombre");
                return View(model);
            }

            _context.Habitaciones.Add(model);
            await _context.SaveChangesAsync();

            if (imagenes != null && imagenes.Any(i => i != null && i.Length > 0))
            {
                bool primera = true;
                foreach (var imagen in imagenes.Where(i => i != null && i.Length > 0))
                {
                    try
                    {
                        var url = await _imgBb.SubirImagenAsync(imagen);
                        if (url != null)
                        {
                            _context.ImagenesHabitacion.Add(new ImagenHabitacion
                            {
                                HabitacionId = model.Id,
                                UrlImagen = url,
                                EsPrincipal = primera
                            });
                            primera = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error al subir una imagen: " + ex.Message;
                    }
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
            ModelState.Remove("Categoria"); ModelState.Remove("Imagenes"); ModelState.Remove("Reservas");

            if (!ModelState.IsValid)
            {
                model.Imagenes = await _context.ImagenesHabitacion.Where(i => i.HabitacionId == id).ToListAsync();
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

            if (nuevasImagenes != null && nuevasImagenes.Any(i => i != null && i.Length > 0))
            {
                bool hayImagenes = await _context.ImagenesHabitacion.AnyAsync(i => i.HabitacionId == id);

                foreach (var imagen in nuevasImagenes.Where(i => i != null && i.Length > 0))
                {
                    try
                    {
                        var url = await _imgBb.SubirImagenAsync(imagen);
                        if (url != null)
                        {
                            _context.ImagenesHabitacion.Add(new ImagenHabitacion
                            {
                                HabitacionId = id,
                                UrlImagen = url,
                                EsPrincipal = !hayImagenes
                            });
                            hayImagenes = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Error al subir una imagen: " + ex.Message;
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["Exito"] = "Habitación actualizada correctamente";
            return RedirectToAction("Habitaciones");
        }

        // ── ELIMINAR HABITACIÓN (con manejo de errores) ───────────────
        [HttpPost]
        public async Task<IActionResult> EliminarHabitacion(int id)
        {
            if (!EsAdmin()) return AccesoDenegado();

            try
            {
                var habitacion = await _context.Habitaciones
                    .Include(h => h.Reservas)
                    .Include(h => h.Imagenes)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (habitacion == null)
                {
                    TempData["Error"] = "La habitación ya no existe";
                    return RedirectToAction("Habitaciones");
                }

                bool tieneReservasActivas = habitacion.Reservas
                    .Any(r => r.Estado == "Confirmada" || r.Estado == "CheckIn" || r.Estado == "Pendiente");

                if (tieneReservasActivas)
                {
                    TempData["Error"] = "No puedes eliminar esta habitación porque tiene reservas activas o pendientes asociadas";
                    return RedirectToAction("Habitaciones");
                }

                // PASO 1: Borrar TODAS las reservas asociadas (incluidas Canceladas/CheckOut) y GUARDAR primero
                if (habitacion.Reservas.Any())
                {
                    _context.Reservas.RemoveRange(habitacion.Reservas);
                    await _context.SaveChangesAsync();
                }

                // PASO 2: Borrar las imágenes asociadas y GUARDAR
                if (habitacion.Imagenes.Any())
                {
                    _context.ImagenesHabitacion.RemoveRange(habitacion.Imagenes);
                    await _context.SaveChangesAsync();
                }

                // PASO 3: Ahora sí, borrar la habitación (ya no tiene nada relacionado)
                _context.Habitaciones.Remove(habitacion);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Habitación eliminada correctamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo eliminar la habitación: " + ex.Message;
            }

            return RedirectToAction("Habitaciones");
        }

        // ── ELIMINAR IMAGEN INDIVIDUAL (solo BD, ya no hay archivo físico) ──
        [HttpPost]
        public async Task<IActionResult> EliminarImagen(int imagenId, int habitacionId)
        {
            if (!EsAdmin()) return AccesoDenegado();

            try
            {
                var imagen = await _context.ImagenesHabitacion.FindAsync(imagenId);
                if (imagen == null)
                {
                    TempData["Error"] = "Imagen no encontrada";
                    return RedirectToAction("EditarHabitacion", new { id = habitacionId });
                }

                bool eraPrincipal = imagen.EsPrincipal;

                _context.ImagenesHabitacion.Remove(imagen);
                await _context.SaveChangesAsync();

                if (eraPrincipal)
                {
                    var siguiente = await _context.ImagenesHabitacion
                        .Where(i => i.HabitacionId == habitacionId)
                        .FirstOrDefaultAsync();
                    if (siguiente != null)
                    {
                        siguiente.EsPrincipal = true;
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Exito"] = "Imagen eliminada correctamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo eliminar la imagen: " + ex.Message;
            }

            return RedirectToAction("EditarHabitacion", new { id = habitacionId });
        }

        [HttpPost]
        public async Task<IActionResult> SetImagenPrincipal(int imagenId, int habitacionId)
        {
            if (!EsAdmin()) return AccesoDenegado();

            var imagenes = await _context.ImagenesHabitacion
                .Where(i => i.HabitacionId == habitacionId)
                .ToListAsync();

            foreach (var img in imagenes) img.EsPrincipal = false;
            var elegida = imagenes.FirstOrDefault(i => i.Id == imagenId);
            if (elegida != null) elegida.EsPrincipal = true;

            await _context.SaveChangesAsync();
            TempData["Exito"] = "Imagen principal actualizada";
            return RedirectToAction("EditarHabitacion", new { id = habitacionId });
        }

        // ── GESTIÓN DE RESERVAS ───────────────────────────────────────
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
    }
}