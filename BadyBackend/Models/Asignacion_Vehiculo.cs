using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Asignacion_Vehiculo
    {

        [Key]
        public int Id { get; set; }
       
        [ForeignKey("Usuario")]
        public int id_usuario { get; set; }
        [JsonIgnore]
        public Usuario Usuario { get; set; }
        [ForeignKey("Vehiculo")]
        public int id_vehiculo { get; set; }
        [JsonIgnore]
        public Vehiculo Vehiculo { get; set; }
        
        public DateTime Fecha { get; set; }

      
        public string Estado { get; set; } = string.Empty;

        public ICollection<Asignacion_Pedido> Asignacion_Pedidos { get; set; }
            = new List<Asignacion_Pedido>();

        public ICollection<Asignacion_Pedido_Usuario>
          Asignacion_Pedido_Usuarios
        { get; set; }
              = new List<Asignacion_Pedido_Usuario>();
    }
}
