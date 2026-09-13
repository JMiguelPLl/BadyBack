using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    public static class PedidoDtos
    {
        public class SucursalCreateDto
        {
            [Required(ErrorMessage = "El cliente es obligatorio.")]
            [Range(1, int.MaxValue, ErrorMessage = "El cliente no es válido.")]
            public int IdCliente { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [MaxLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "La descripción es obligatoria.")]
            [MaxLength(300)]
            public string Descripcion { get; set; } = string.Empty;

            [Required(ErrorMessage = "La ubicación es obligatoria.")]
            [MaxLength(300)]
            public string Ubicacion { get; set; } = string.Empty;
        }

        public class SucursalUpdateDto
        {
            [Required(ErrorMessage = "El cliente es obligatorio.")]
            [Range(1, int.MaxValue, ErrorMessage = "El cliente no es válido.")]
            public int IdCliente { get; set; }

            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [MaxLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "La descripción es obligatoria.")]
            [MaxLength(300)]
            public string Descripcion { get; set; } = string.Empty;

            [Required(ErrorMessage = "La ubicación es obligatoria.")]
            [MaxLength(300)]
            public string Ubicacion { get; set; } = string.Empty;
        }

        public class SucursalResponseDto
        {
            public int Id { get; set; }

            public int IdCliente { get; set; }

            public string Cliente { get; set; } = string.Empty;

            public string Nombre { get; set; } = string.Empty;

            public string Descripcion { get; set; } = string.Empty;

            public string Ubicacion { get; set; } = string.Empty;

            public string Estado { get; set; } = string.Empty;
        }

        public class CambiarEstadoSucursalDto
        {
            [Required(ErrorMessage = "El estado es obligatorio.")]
            public string Estado { get; set; } = string.Empty;
        }
    }
}