using System.ComponentModel.DataAnnotations;

namespace BadyBackend.Models
{
    public class Tipo_Pago
    {
        [Key]
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; }
        public ICollection<Pago> Pagos { get; set; }

    }
}
