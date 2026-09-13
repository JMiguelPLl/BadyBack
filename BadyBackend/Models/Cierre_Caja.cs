using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Cierre_Caja
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Usuario))]
        public int Id_usuario { get; set; }

        [JsonIgnore]
        public Usuario Usuario { get; set; } = null!;

        public DateTime Fecha_Apertura { get; set; } = DateTime.UtcNow;

        public DateTime? Fecha_Cierre { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total_Efectivo { get; set; } = 0m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total_QR { get; set; } = 0m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total_Recaudado { get; set; } = 0m;

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = "Abierta";

        [MaxLength(500)]
        public string? Observacion { get; set; }

        [JsonIgnore]
        public ICollection<Cierre_Caja_Detalle> Cierre_Caja_Detalles { get; set; } = new List<Cierre_Caja_Detalle>();
    }
}
