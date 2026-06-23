using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty; // Individual, Doble, Suite, Familiar

        [StringLength(200)]
        public string? Descripcion { get; set; }

        // Navegación
        public ICollection<Habitacion> Habitaciones { get; set; } = new List<Habitacion>();
    }
}
