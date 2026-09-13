namespace BadyApi.Helpers
{
    public static class EstadosPedido
    {
        public const string Pendiente = "Pendiente";

        public const string Asignado = "Asignado";

        public const string EnCamino = "EnCamino";

        public const string PorConfirmarEntrega =
            "PorConfirmarEntrega";

        public const string Entregado = "Entregado";

        public const string Cancelado = "Cancelado";

        public const string Devuelto = "Devuelto";


        public static readonly string[] Todos =
        {
            Pendiente,
            Asignado,
            EnCamino,
            PorConfirmarEntrega,
            Entregado,
            Cancelado,
            Devuelto
        };


        public static string? Normalizar(string estado)
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


    public static class EstadosDetallePedido
    {
        public const string PendienteEntrega =
            "PendienteEntrega";

        public const string Entregado =
            "Entregado";

        public const string Cancelado =
            "Cancelado";

        public const string Devuelto =
            "Devuelto";
    }


    public static class EstadosAsignacionPedido
    {
        public const string Asignado =
            "Asignado";

        public const string Entregado =
            "Entregado";


        public static readonly string[] Todos =
        {
            Asignado,
            Entregado
        };


        public static string? Normalizar(
            string estado)
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