namespace BadyBackend.DTOs
{
    public class PersonalEntregaDto
    {
        public int IdUsuario { get; set; }

        public string Usuario { get; set; }
            = string.Empty;

        public string? Correo { get; set; }
    }
}
