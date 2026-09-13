using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Usuario_rol
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("Rol")]
        public int id_rol { get; set; }
        [JsonIgnore]
        public Rol Rol { get; set; }
        [ForeignKey("Usuario")]
        public int id_usuario { get; set; }
        [JsonIgnore]
        public Usuario Usuario { get; set; }
        public string Estado { get; set; }
    }
}
