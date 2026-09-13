using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    
    
        public class VehiculoUpdateDto
        {
            [Required(ErrorMessage = "La marca es obligatoria.")]
            [MaxLength(50)]
            public string Marca { get; set; } = string.Empty;

            [MaxLength(20)]
            public string? Placa { get; set; }

            [Required(ErrorMessage = "La capacidad de carga es obligatoria.")]
            [MaxLength(50)]
            public string CantidadCarga { get; set; } = string.Empty;
        }
        public class VehiculoResponseDto
        {
            public int Id { get; set; }
            public string Marca { get; set; } = string.Empty;
            public string? Placa { get; set; }
            public string CantidadCarga { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
        }
        public class VehiculoCreateDto
        {
            [Required(ErrorMessage = "La marca es obligatoria.")]
            [MaxLength(50)]
            public string Marca { get; set; } = string.Empty;

            [MaxLength(20)]
            public string? Placa { get; set; }

            [Required(ErrorMessage = "La capacidad de carga es obligatoria.")]
            [MaxLength(50)]
            public string CantidadCarga { get; set; } = string.Empty;
        }
        public class CambiarEstadoVehiculoDto
        {
            [Required(ErrorMessage = "El estado es obligatorio.")]
            public string Estado { get; set; } = string.Empty;
        }
    }

