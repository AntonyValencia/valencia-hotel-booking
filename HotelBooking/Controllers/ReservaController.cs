using HotelBooking.Data;
using HotelBooking.Models;
using HotelBooking.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelBooking.Controllers
{
    public class ReservaController : Controller
    {
        private readonly HotelDbContext _context;

        public ReservaController(HotelDbContext context)
        {
            _context = context;
        }

        private bool EstaLogueado() => HttpContext.Session.GetString("UsuarioId") != null;
        private int GetUsuarioId() => int.Parse(HttpContext.Session.GetString("UsuarioId")!);

        [HttpGet]
        public async Task<IActionResult> Crear(int habitacionId, DateTime? fechaEntrada, DateTime? fechaSalida)
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
                FechaEntrada = fechaEntrada ?? DateTime.Today.AddDays(1),
                FechaSalida = fechaSalida ?? DateTime.Today.AddDays(2)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ReservaViewModel model)
        {
            if (!EstaLogueado()) return RedirectToAction("Login", "Auth");

            model.Habitacion = await _context.Habitaciones
                .Include(h => h.Categoria)
                .Include(h => h.Imagenes)
                .FirstOrDefaultAsync(h => h.Id == model.HabitacionId);

            if (model.FechaEntrada < DateTime.Today)
                ModelState.AddModelError("FechaEntrada", "La fecha de entrada no puede ser en el pasado");
            if (model.FechaSalida <= model.FechaEntrada)
                ModelState.AddModelError("FechaSalida", "La fecha de salida debe ser posterior a la entrada");

            if (!ModelState.IsValid) return View(model);

            var conflicto = await _context.Reservas.AnyAsync(r =>
                r.HabitacionId == model.HabitacionId &&
                r.Estado != "Cancelada" &&
                r.FechaEntrada < model.FechaSalida &&
                r.FechaSalida > model.FechaEntrada);

            if (conflicto)
            {
                ModelState.AddModelError("", "La habitación no está disponible en esas fechas.");
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

            TempData["Exito"] = $"¡Reserva realizada con éxito! Código: #{reserva.Id}";
            return RedirectToAction("Boleta", new { id = reserva.Id });
        }

        public async Task<IActionResult> Boleta(int id)
        {
            if (!EstaLogueado()) return RedirectToAction("Login", "Auth");

            var reserva = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Habitacion)!.ThenInclude(h => h!.Categoria)
                .Include(r => r.Habitacion)!.ThenInclude(h => h!.Imagenes)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null) return NotFound();
            if (reserva.UsuarioId != GetUsuarioId() && HttpContext.Session.GetString("UsuarioRol") != "Admin")
                return Forbid();

            return View(reserva);
        }

        public async Task<IActionResult> DescargarBoleta(int id)
        {
            if (!EstaLogueado()) return RedirectToAction("Login", "Auth");

            var reserva = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Habitacion)!.ThenInclude(h => h!.Categoria)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null) return NotFound();
            if (reserva.UsuarioId != GetUsuarioId() && HttpContext.Session.GetString("UsuarioRol") != "Admin")
                return Forbid();

            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(c => ComposeContent(c, reserva));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Hotel Booking Cochabamba  •  Av. Heroínas #123  •  Tel: +591 4 4123456");
                        x.DefaultTextStyle(s => s.FontSize(9).FontColor("#888888"));
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Boleta_Reserva_{id}.pdf");
        }

        static void ComposeHeader(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("HOTEL BOOKING COCHABAMBA").Bold().FontSize(18).FontColor("#C9A84C");
                        left.Item().Text("Sistema de Reservaciones Hoteleras").FontSize(10).FontColor("#666666");
                        left.Item().Text("Cochabamba, Bolivia").FontSize(10).FontColor("#666666");
                    });
                    row.ConstantItem(120).AlignRight().Column(right =>
                    {
                        right.Item().Text("BOLETA DE RESERVA").Bold().FontSize(13).FontColor("#1a1a1a");
                        right.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}").FontSize(9).FontColor("#666666");
                    });
                });
                col.Item().PaddingTop(5).LineHorizontal(2).LineColor("#C9A84C");
            });
        }

        static void ComposeContent(IContainer c, Reserva reserva)
        {
            var dias = reserva.TotalDias;
            c.PaddingTop(20).Column(col =>
            {
                col.Item().Background("#1a1a1a").Padding(12).Column(info =>
                {
                    info.Item().Text($"Reserva N.° {reserva.Id:D6}").Bold().FontSize(16).FontColor("#C9A84C");
                    info.Item().Text($"Estado: {reserva.Estado}").FontSize(11).FontColor("#ffffff");
                    info.Item().Text($"Fecha de reserva: {reserva.FechaReserva:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#aaaaaa");
                });

                col.Item().PaddingTop(20).Text("DATOS DEL HUÉSPED").Bold().FontSize(12).FontColor("#1a1a1a");
                col.Item().LineHorizontal(1).LineColor("#dddddd");
                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
                    t.Cell().Text("Nombre completo:").Bold();
                    t.Cell().Text(reserva.Usuario?.Nombre ?? "—");
                    t.Cell().Text("Email:").Bold();
                    t.Cell().Text(reserva.Usuario?.Email ?? "—");
                    t.Cell().Text("Teléfono:").Bold();
                    t.Cell().Text(reserva.Usuario?.Telefono ?? "—");
                });

                col.Item().PaddingTop(20).Text("DETALLES DE LA HABITACIÓN").Bold().FontSize(12).FontColor("#1a1a1a");
                col.Item().LineHorizontal(1).LineColor("#dddddd");
                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
                    t.Cell().Text("Habitación:").Bold();
                    t.Cell().Text(reserva.Habitacion?.Nombre ?? "—");
                    t.Cell().Text("Categoría:").Bold();
                    t.Cell().Text(reserva.Habitacion?.Categoria?.Nombre ?? "—");
                    t.Cell().Text("Servicios incluidos:").Bold();
                    t.Cell().Text(reserva.Habitacion?.Servicios ?? "—");
                    t.Cell().Text("Capacidad:").Bold();
                    t.Cell().Text($"{reserva.Habitacion?.Capacidad} persona(s)");
                });

                col.Item().PaddingTop(20).Text("FECHAS DE ESTADÍA").Bold().FontSize(12).FontColor("#1a1a1a");
                col.Item().LineHorizontal(1).LineColor("#dddddd");
                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
                    t.Cell().Text("Check-In:").Bold();
                    t.Cell().Text($"{reserva.FechaEntrada:dd/MM/yyyy}");
                    t.Cell().Text("Check-Out:").Bold();
                    t.Cell().Text($"{reserva.FechaSalida:dd/MM/yyyy}");
                    t.Cell().Text("Total de noches:").Bold();
                    t.Cell().Text($"{dias} noche(s)");
                    t.Cell().Text("Hora de check-in:").Bold();
                    t.Cell().Text("A partir de las 14:00 hrs");
                    t.Cell().Text("Hora de check-out:").Bold();
                    t.Cell().Text("Hasta las 12:00 hrs");
                });

                col.Item().PaddingTop(24).Background("#1a1a1a").Padding(16).Row(row =>
                {
                    row.RelativeItem().Text("TOTAL A PAGAR").Bold().FontSize(14).FontColor("#ffffff");
                    row.ConstantItem(200).AlignRight().Text($"Bs. {reserva.TotalPagar:N2}").Bold().FontSize(18).FontColor("#C9A84C");
                });

                col.Item().PaddingTop(6).AlignRight()
                    .Text($"({dias} noches × Bs. {reserva.Habitacion?.PrecioPorNoche:N2}/noche)").FontSize(9).FontColor("#888888");

                if (!string.IsNullOrEmpty(reserva.Notas))
                {
                    col.Item().PaddingTop(20).Text("NOTAS ADICIONALES").Bold().FontSize(11);
                    col.Item().LineHorizontal(1).LineColor("#dddddd");
                    col.Item().PaddingTop(6).Text(reserva.Notas).FontColor("#444444");
                }

                col.Item().PaddingTop(30).Background("#f9f6f0").Padding(14).Column(aviso =>
                {
                    aviso.Item().Text("POLÍTICAS DE RESERVA").Bold().FontSize(10).FontColor("#C9A84C");
                    aviso.Item().PaddingTop(4).Text("• Presente esta boleta al momento de su llegada al hotel.").FontSize(9);
                    aviso.Item().Text("• La cancelación es gratuita hasta 24 horas antes del check-in.").FontSize(9);
                    aviso.Item().Text("• Para modificaciones contacte al: +591 4 4123456").FontSize(9);
                });
            });
        }

        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (!EstaLogueado()) return RedirectToAction("Login", "Auth");
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null || reserva.UsuarioId != GetUsuarioId())
            {
                TempData["Error"] = "No puedes cancelar esta reserva";
                return RedirectToAction("MisReservas", "Cliente");
            }
            if (reserva.Estado == "CheckIn" || reserva.Estado == "CheckOut")
            {
                TempData["Error"] = "No puedes cancelar una reserva en proceso o finalizada";
                return RedirectToAction("MisReservas", "Cliente");
            }
            reserva.Estado = "Cancelada";
            await _context.SaveChangesAsync();
            TempData["Exito"] = "Reserva cancelada correctamente";
            return RedirectToAction("MisReservas", "Cliente");
        }
    }
}