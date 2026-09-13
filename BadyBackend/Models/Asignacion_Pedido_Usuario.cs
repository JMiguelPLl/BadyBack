using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class Asignacion_Pedido_Usuario
    {
        [Key]
        public int Id { get; set; }


        // ================================================
        // ASIGNACIÓN DEL PEDIDO
        // ================================================

        [ForeignKey(nameof(AsignacionPedido))]
        public int Id_asignacion_pedido { get; set; }

        [JsonIgnore]
        public Asignacion_Pedido AsignacionPedido { get; set; }
            = null!;


        // ================================================
        // ASIGNACIÓN VEHÍCULO / USUARIO
        // ================================================
        //
        // Esto apunta a la fila histórica concreta de
        // Asignacion_Vehiculo.
        //
        // Ejemplo:
        // Juan + Toyota + 12/06/2026
        //
        // ================================================

        [ForeignKey(nameof(AsignacionVehiculo))]
        public int Id_asignacion_vehiculo { get; set; }

        [JsonIgnore]
        public Asignacion_Vehiculo AsignacionVehiculo { get; set; }
            = null!;


        // ================================================
        // FECHA
        // ================================================

        public DateTime Fecha { get; set; }


        // ================================================
        // ESTADO
        // ================================================

        public string Estado { get; set; }
            = "Activo";
    }
}