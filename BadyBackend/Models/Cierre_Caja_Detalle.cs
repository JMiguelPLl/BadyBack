using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Cierre_Caja_Detalle
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Cierre_Caja))]
        public int Id_cierre_caja { get; set; }

        [JsonIgnore]
        public Cierre_Caja Cierre_Caja { get; set; } = null!;

        [ForeignKey(nameof(Pago))]
        public int Id_pago { get; set; }

        [JsonIgnore]
        public Pago Pago { get; set; } = null!;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
