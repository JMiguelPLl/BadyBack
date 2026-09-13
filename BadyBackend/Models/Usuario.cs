using BadyBackend.Controllers;
using System.ComponentModel.DataAnnotations;

namespace BadyBackend.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Numero { get; set; }

        public string Email { get; set; }
        public string Contraseña { get; set; }
        public string Estado { get; set; }
        public ICollection<Usuario_rol> Usuario_Rols { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Cierre_Caja> Cierre_Cajas { get; set; } = new List<Cierre_Caja>();
    }
}
