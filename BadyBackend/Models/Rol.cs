using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BadyBackend.Models
{
    public class Rol
    {

        [Key]
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; }
        public ICollection<Usuario_rol> Usuario_Rols {  get; set; }
    }
}
