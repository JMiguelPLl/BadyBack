using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    
    
    public class VehiculoUpdateDto
    {
        [Required(ErrorMessage = "La marca es obligatoria.")]
        [MaxLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "La placa es obligatoria.")]
        [StringLength(7, MinimumLength = 7, ErrorMessage = "La placa debe tener exactamente 7 caracteres.")]
        [RegularExpression(@"^(?:[0-9]{4}[A-Za-z]{3}|[A-Za-z]{3}[0-9]{4})$", ErrorMessage = "El formato de la placa debe ser de 3 letras y 4 números (ejemplo: 1234ABC o ABC1234).")]
        public string Placa { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "La placa es obligatoria.")]
        [StringLength(7, MinimumLength = 7, ErrorMessage = "La placa debe tener exactamente 7 caracteres.")]
        [RegularExpression(@"^(?:[0-9]{4}[A-Za-z]{3}|[A-Za-z]{3}[0-9]{4})$", ErrorMessage = "El formato de la placa debe ser de 3 letras y 4 números (ejemplo: 1234ABC o ABC1234).")]
        public string Placa { get; set; } = string.Empty;

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

