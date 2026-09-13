using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Asignacion_Pedido
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Asignacion_vehiculo))]
        public int id_asignacion_vehiculo { get; set; }

        [JsonIgnore]
        public Asignacion_Vehiculo Asignacion_vehiculo { get; set; }
            = null!;

        [ForeignKey(nameof(Pedido))]
        public int id_pedido { get; set; }

        [JsonIgnore]
        public Pedido Pedido { get; set; }
            = null!;

        public DateTime Fecha_Asignacion { get; set; }

        public DateTime? Fecha_Entrega { get; set; }

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = string.Empty;
        public ICollection<Asignacion_Pedido_Usuario>
        Asignacion_Pedido_Usuarios
        { get; set; }
            = new List<Asignacion_Pedido_Usuario>();
    }
}

