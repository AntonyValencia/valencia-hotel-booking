using HotelBooking.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly HotelDbContext _context;

        public HomeController(HotelDbContext context)
        {
            _context = context;
        }

        // Página principal - muestra habitaciones disponibles
        public async Task<IActionResult> Index(int? categoriaId, string? busqueda)
        {
            var query = _context.Habitaciones
                .Include(h => h.Categoria)
                .Include(h => h.Imagenes)
                .Where(h => h.Disponible)
                .AsQueryable();

            // Filtro por categoría
            if (categoriaId.HasValue)
                query = query.Where(h => h.CategoriaId == categoriaId.Value);

            // Búsqueda por nombre
            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(h => h.Nombre.Contains(busqueda) || h.Descripcion.Contains(busqueda));

            var habitaciones = await query.ToListAsync();
            var categorias = await _context.Categorias.ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.CategoriaSeleccionada = categoriaId;
            ViewBag.Busqueda = busqueda;

            return View(habitaciones);
        }

        // Detalle de una habitación
        public async Task<IActionResult> Detalle(int id)
        {
            var habitacion = await _context.Habitaciones
                .Include(h => h.Categoria)
                .Include(h => h.Imagenes)
                .Include(h => h.Reservas)
                .FirstOrDefaultAsync(h => h.Id == id && h.Disponible);

            if (habitacion == null)
                return NotFound();

            // Fechas reservadas para deshabilitar en el calendario
            var fechasReservadas = habitacion.Reservas
                .Where(r => r.Estado != "Cancelada")
                .Select(r => new { r.FechaEntrada, r.FechaSalida })
                .ToList();

            ViewBag.FechasReservadas = fechasReservadas;
            return View(habitacion);
        }

        // Verificar disponibilidad por AJAX
        [HttpGet]
        public async Task<IActionResult> VerificarDisponibilidad(int habitacionId, DateTime entrada, DateTime salida)
        {
            if (entrada >= salida)
                return Json(new { disponible = false, mensaje = "La fecha de salida debe ser posterior a la entrada" });

            if (entrada < DateTime.Today)
                return Json(new { disponible = false, mensaje = "La fecha de entrada no puede ser en el pasado" });

            var conflicto = await _context.Reservas.AnyAsync(r =>
                r.HabitacionId == habitacionId &&
                r.Estado != "Cancelada" &&
                r.FechaEntrada < salida &&
                r.FechaSalida > entrada);

            if (conflicto)
                return Json(new { disponible = false, mensaje = "La habitación no está disponible en esas fechas" });

            var habitacion = await _context.Habitaciones.FindAsync(habitacionId);
            var dias = (salida - entrada).Days;
            var total = dias * habitacion!.PrecioPorNoche;

            return Json(new { disponible = true, dias, total, mensaje = "¡Disponible!" });
        }
    }
}
