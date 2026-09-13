using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Sucursal
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Cliente")]
        public int Id_cliente { get; set; }
        [JsonIgnore]
        public Cliente Cliente { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Ubicacion { get; set; }
        public string Estado { get; set; }
    }
}
