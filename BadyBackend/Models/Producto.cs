using System.ComponentModel.DataAnnotations;

namespace BadyBackend.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; } 
        public int Stock  { get; set; }
        public decimal Precio { get; set; }
        public string Estado { get; set; }
        public string? Imagen { get; set; }
    }
}
