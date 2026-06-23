using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBooking.Models
{
    public class Habitacion
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 99999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioPorNoche { get; set; }

        [Required(ErrorMessage = "La capacidad es obligatoria")]
        [Range(1, 20, ErrorMessage = "La capacidad debe ser entre 1 y 20 personas")]
        public int Capacidad { get; set; }

        public bool Disponible { get; set; } = true;

        public string? Servicios { get; set; } // WiFi, TV, AC, etc.

        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        // Imágenes
        public ICollection<ImagenHabitacion> Imagenes { get; set; } = new List<ImagenHabitacion>();

        // Reservas
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }

    public class ImagenHabitacion
    {
        public int Id { get; set; }

        [Required]
        public string UrlImagen { get; set; } = string.Empty;

        public bool EsPrincipal { get; set; } = false;

        public int HabitacionId { get; set; }
        public Habitacion? Habitacion { get; set; }
    }
}
