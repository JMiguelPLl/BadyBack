using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Detalle_Pedido
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Pedido))]
        public int Id_pedido { get; set; }

        [JsonIgnore]
        public Pedido Pedido { get; set; } = null!;

        [ForeignKey(nameof(Producto))]
        public int Id_producto { get; set; }

        [JsonIgnore]
        public Producto Producto { get; set; } = null!;

        public int Cantidad { get; set; }

        public decimal Subtotal { get; set; }

        public string Estado { get; set; } = string.Empty;
    }
}