using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BadyBackend.Models
{
    public class UsuarioDtos
    {
        public class UsuarioListDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } 
            public string Numero { get; set; }
            public string Email { get; set; } 
            public string Estado { get; set; }

            public int IdRol { get; set; }
            public string Rol { get; set; } = string.Empty;
        }

        public class UsuarioCreateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El teléfono es obligatorio.")]
            public string Numero { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo es obligatorio.")]
            [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
            public string Contrasena { get; set; } = string.Empty;

            [Required(ErrorMessage = "El rol es obligatorio.")]
            public int IdRol { get; set; }
        }
        public class UsuarioUpdateDto
        {
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El teléfono es obligatorio.")]
            public string Telefono { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo es obligatorio.")]
            [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
            public string Email { get; set; } = string.Empty;

            public string? Contrasena { get; set; }

            [Required(ErrorMessage = "El rol es obligatorio.")]
            public int IdRol { get; set; }
        }

        public class CambiarEstadoDto
        {
            [Required(ErrorMessage = "El estado es obligatorio.")]
            public string Estado { get; set; } = string.Empty;
        }
    }
}
