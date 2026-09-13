namespace BadyApi.Helpers
{
    public static class EstadosPago
    {
        public const string Activo = "Activo";
        public const string Anulado = "Anulado";
    }

    public static class EstadosDeuda
    {
        public const string Pendiente = "Pendiente";
        public const string Pagado = "Pagado";

        public static readonly string[] Todos =
        {
            Pendiente,
            Pagado
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