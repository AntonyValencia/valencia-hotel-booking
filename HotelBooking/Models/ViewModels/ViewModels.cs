using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Models.ViewModels
{
    // ViewModel para Login
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un email válido (ej: usuario@gmail.com)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool Recordarme { get; set; }
    }

    // ViewModel para Registro
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un email válido (ej: usuario@gmail.com)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Phone(ErrorMessage = "Ingresa un número de teléfono válido")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe tener al menos una mayúscula, una minúscula y un número")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma tu contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [DataType(DataType.Password)]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }

    // ViewModel para crear/editar reserva
    public class ReservaViewModel
    {
        public int HabitacionId { get; set; }
        public Habitacion? Habitacion { get; set; }

        [Required(ErrorMessage = "Selecciona la fecha de entrada")]
        public DateTime FechaEntrada { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Selecciona la fecha de salida")]
        public DateTime FechaSalida { get; set; } = DateTime.Today.AddDays(2);

        public string? Notas { get; set; }
    }

    // ViewModel para editar perfil
    public class EditarPerfilViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Número de teléfono inválido")]
        public string? Telefono { get; set; }

        [DataType(DataType.Password)]
        public string? NuevaPassword { get; set; }

        [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden")]
        [DataType(DataType.Password)]
        public string? ConfirmarPassword { get; set; }
    }

    // ViewModel para Dashboard Admin
    public class DashboardViewModel
    {
        public int TotalHabitaciones { get; set; }
        public int TotalReservas { get; set; }
        public int ReservasPendientes { get; set; }
        public int ReservasConfirmadas { get; set; }
        public decimal IngresosMes { get; set; }
        public List<Reserva> UltimasReservas { get; set; } = new();
    }
}
