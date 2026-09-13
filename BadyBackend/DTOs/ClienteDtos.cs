using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    public static class ClienteDtos
    {
        public class ClienteCreateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [MaxLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
            [MaxLength(20)]
            public string Numero { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo es obligatorio.")]
            [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
            [MaxLength(150)]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
            public string Contrasena { get; set; } = string.Empty;
        }

        public class ClienteUpdateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [MaxLength(150)]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
            [MaxLength(20)]
            public string Numero { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo es obligatorio.")]
            [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
            [MaxLength(150)]
            public string Email { get; set; } = string.Empty;

            /*
             * Es opcional al editar.
             * Si llega vacío, se mantiene la contraseña actual.
             */

            public string? Contrasena { get; set; }
        }

        public class ClienteResponseDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Contraseña { get; set; } = string.Empty;
            public string Numero { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
        }
        public class CambiarEstadoClienteDto
        {
            public string Estado { get; set; } = string.Empty;
        }
    }
}