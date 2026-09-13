using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    public class AsignacionVehiculoCreateDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El usuario es obligatorio.")]
        public int IdUsuario { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El vehículo es obligatorio.")]
        public int IdVehiculo { get; set; }
    }
    public class AsignacionVehiculoResponseDto
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? CorreoUsuario { get; set; }
        public int IdVehiculo { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public string? Placa { get; set; }
        public string CantidadCarga { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
    public class AsignacionVehiculoUpdateDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El usuario es obligatorio.")]
        public int IdUsuario { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El vehículo es obligatorio.")]
        public int IdVehiculo { get; set; }
    }
    public class CambiarEstadoAsignacionVehiculoDto
    {
        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string Estado { get; set; } = string.Empty;
    }
    public class UsuarioAsignadoVehiculoDto
    {
        public int IdAsignacion { get; set; }

        public int IdUsuario { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string? Correo { get; set; }

        public DateTime FechaAsignacion { get; set; }

        public string Estado { get; set; } = string.Empty;
    }
    public class DetalleVehiculoAsignadoDto
    {
        public int IdVehiculo { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string? Placa { get; set; }

        public string CantidadCarga { get; set; } = string.Empty;

        public string EstadoVehiculo { get; set; } = string.Empty;

        public int CantidadUsuariosAsignados { get; set; }

        public List<UsuarioAsignadoVehiculoDto> UsuariosAsignados
        {
            get;
            set;
        } = new();
    }
}
