using HotelBooking.Data;
using HotelBooking.Models;
using HotelBooking.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Controllers
{
    public class ReservaController : Controller
    {
        private readonly HotelDbContext _context;

        public ReservaController(HotelDbContext context)
        {
            _context = context;
        }

        // Verificar si el usuario está logueado
        private bool EstaLogueado() => HttpContext.Session.GetString("UsuarioId") != null;
        private int GetUsuarioId() => int.Parse(HttpContext.Session.GetString("UsuarioId")!);

        // ============================================================
        // CREAR RESERVA
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Crear(int habitacionId)
        {
            if (!EstaLogueado())
            {
                TempData["Info"] = "Debes iniciar sesión para reservar una habitación";
                return RedirectToAction("Login", "Auth");
            }

            var habitacion = await _context.Habitaciones
                .Include(h => h.Categoria)
                .Include(h => h.Imagenes)
                .FirstOrDefaultAsync(h => h.Id == habitacionId && h.Disponible);

            if (habitacion == null)
            {
                TempData["Error"] = "Habitación no encontrada o no disponible";
                return RedirectToAction("Index", "Home");
            }

            var model = new ReservaViewModel
            {
                HabitacionId = habitacionId,
                Habitacion = habitacion,
                FechaEntrada = DateTime.Today.AddDays(1),
                FechaSalida = DateTime.Today.AddDays(2)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ReservaViewModel model)
        {
            if (!EstaLogueado())
                return RedirectToAction("Login", "Auth");

            // Recargar habitación para la vista si hay errores
            model.Habitacion = await _context.Habitaciones
                .Include(h => h.Categoria)
                .Include(h => h.Imagenes)
                .FirstOrDefaultAsync(h => h.Id == model.HabitacionId);

            // Validaciones de fechas
            if (model.FechaEntrada < DateTime.Today)
            {
                ModelState.AddModelError("FechaEntrada", "La fecha de entrada no puede ser en el pasado");
            }

            if (model.FechaSalida <= model.FechaEntrada)
            {
                ModelState.AddModelError("FechaSalida", "La fecha de salida debe ser posterior a la entrada");
            }

            if (!ModelState.IsValid) return View(model);

            // Verificar disponibilidad
            var conflicto = await _context.Reservas.AnyAsync(r =>
                r.HabitacionId == model.HabitacionId &&
                r.Estado != "Cancelada" &&
                r.FechaEntrada < model.FechaSalida &&
                r.FechaSalida > model.FechaEntrada);

            if (conflicto)
            {
                ModelState.AddModelError("", "La habitación no está disponible en esas fechas. Por favor elige otras fechas.");
                return View(model);
            }

            var dias = (model.FechaSalida - model.FechaEntrada).Days;
            var total = dias * model.Habitacion!.PrecioPorNoche;

            var reserva = new Reserva
            {
                UsuarioId = GetUsuarioId(),
                HabitacionId = model.HabitacionId,
                FechaEntrada = model.FechaEntrada,
                FechaSalida = model.FechaSalida,
                TotalPagar = total,
                Estado = "Pendiente",
                FechaReserva = DateTime.Now,
                Notas = model.Notas
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"¡Reserva realizada con éxito! Tu código de reserva es #{reserva.Id}";
            return RedirectToAction("MisReservas", "Cliente");
        }

        // ============================================================
        // CANCELAR RESERVA (solo el dueño)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (!EstaLogueado())
                return RedirectToAction("Login", "Auth");

            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null || reserva.UsuarioId != GetUsuarioId())
            {
                TempData["Error"] = "No puedes cancelar esta reserva";
                return RedirectToAction("MisReservas", "Cliente");
            }

            if (reserva.Estado == "CheckIn" || reserva.Estado == "CheckOut")
            {
                TempData["Error"] = "No puedes cancelar una reserva que ya está en proceso o finalizada";
                return RedirectToAction("MisReservas", "Cliente");
            }

            reserva.Estado = "Cancelada";
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Reserva cancelada correctamente";
            return RedirectToAction("MisReservas", "Cliente");
        }
    }
}
