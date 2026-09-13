using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Contrasena { get; set; } = string.Empty;
    }
    namespace BadyBackend.DTOs
    {
        public class LoginResponseDto
        {
            public string Token { get; set; } = string.Empty;
            public DateTime Expiracion { get; set; }

            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Rol { get; set; } = string.Empty;
            public string TipoCuenta { get; set; } = string.Empty;
        }
    }
}