namespace BadyApi.Helpers
{
    public static class EstadosCierreCaja
    {
        public const string Abierta = "Abierta";
        public const string Cerrada = "Cerrada";
        public const string Anulada = "Anulada";

        public static readonly string[] Todos =
        {
            Abierta,
            Cerrada,
            Anulada
        };

        public static string? Normalizar(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return null;

            return Todos.FirstOrDefault(x =>
                x.Equals(
                    estado.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }
    }
}
