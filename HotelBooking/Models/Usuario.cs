using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido (ej: usuario@gmail.com)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = "Cliente"; // "Admin" o "Cliente"

        public string? Telefono { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        // Navegación
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
