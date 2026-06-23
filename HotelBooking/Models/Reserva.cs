using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBooking.Models
{
    public class Reserva
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required]
        public int HabitacionId { get; set; }
        public Habitacion? Habitacion { get; set; }

        [Required(ErrorMessage = "La fecha de entrada es obligatoria")]
        public DateTime FechaEntrada { get; set; }

        [Required(ErrorMessage = "La fecha de salida es obligatoria")]
        public DateTime FechaSalida { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPagar { get; set; }

        // Estado: Pendiente, Confirmada, CheckIn, CheckOut, Cancelada
        public string Estado { get; set; } = "Pendiente";

        public DateTime FechaReserva { get; set; } = DateTime.Now;

        public string? Notas { get; set; }

        // Días calculados
        [NotMapped]
        public int TotalDias => (FechaSalida - FechaEntrada).Days;
    }
}
