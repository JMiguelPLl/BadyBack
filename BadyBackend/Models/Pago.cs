using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Pago
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Usuario))]
        public int Id_usuario { get; set; }

        [JsonIgnore]
        public Usuario Usuario { get; set; } = null!;

        [ForeignKey(nameof(Pedido))]
        public int Id_pedido { get; set; }

        [JsonIgnore]
        public Pedido Pedido { get; set; } = null!;

        [ForeignKey(nameof(TipoPago))]
        public int Id_tipoPago { get; set; }

        [JsonIgnore]
        public Tipo_Pago TipoPago { get; set; } = null!;

        public DateTime Fecha { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoPagado { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal SaldoPendiente { get; set; }

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<Cierre_Caja_Detalle> Cierre_Caja_Detalles { get; set; } = new List<Cierre_Caja_Detalle>();
    }
}