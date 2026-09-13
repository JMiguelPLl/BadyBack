using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Vehiculo
    {

        [Key]
        public int Id { get; set; }
        public string Marca { get; set; }

        public string? Placa { get; set; }

        public string Cantidad_Carga { get; set; }

        public string Estado { get; set; } = string.Empty;

        public ICollection<Asignacion_Vehiculo> Asignacion_Vehiculos { get; set; }
            = new List<Asignacion_Vehiculo>();

     
}
}
