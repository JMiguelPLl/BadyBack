using System.ComponentModel.DataAnnotations;

namespace BadyBackend.Models
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Numero { get; set; }

        public string Email { get; set; }
        public string Contraseña { get; set; }
        public string Estado { get; set; }
        public ICollection<Sucursal> Sucursales { get; set; }
    }
}
