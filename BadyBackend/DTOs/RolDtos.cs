namespace BadyBackend.DTOs
{
    public class RolDtos
    {
        public class RolCreateDto
        {
            public int Id { get; set; }
            public string Descripcion { get; set; }
            public string Estado { get; set; }
        }
        public class RolUpdateDto
        {
            public int Id { get; set; }
            public string Descripcion { get; set; }
            public string Estado { get; set; }
        }
    }
}
