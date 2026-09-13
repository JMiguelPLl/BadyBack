using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Sucursal))]
        public int Id_sucursal { get; set; }

        [JsonIgnore]
        public Sucursal Sucursal { get; set; } = null!;

        [ForeignKey(nameof(Cliente))]
        public int Id_cliente { get; set; }

        [JsonIgnore]
        public Cliente Cliente { get; set; } = null!;

        public DateTime Fecha { get; set; }

        public string? Observacion { get; set; }

        [MaxLength(500)]
        public string? Motivo_Edicion { get; set; }

        [MaxLength(500)]
        public string? Motivo_Devolucion { get; set; }

        public DateTime? Fecha_Devolucion { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } = string.Empty;

        public ICollection<Detalle_Pedido> Detalle_Pedidos { get; set; }
            = new List<Detalle_Pedido>();

        public ICollection<Pago> Pagos { get; set; }
              = new List<Pago>();

    }
}