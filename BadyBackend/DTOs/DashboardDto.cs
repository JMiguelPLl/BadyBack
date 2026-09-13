namespace BadyBackend.DTOs
{
    public class DashboardResumenDto
    {
        public int Anio { get; set; }

        public int Mes { get; set; }

        public decimal IngresosMes { get; set; }

        public decimal IngresosMesAnterior { get; set; }

        public decimal VariacionIngresosPorcentaje { get; set; }

        public decimal DeudaPendienteTotal { get; set; }

        public decimal DeudaPendienteMes { get; set; }

        public int PedidosMes { get; set; }

        public int PedidosPendientesConfirmacion { get; set; }

        public int ProductosStockBajo { get; set; }

        public decimal TotalVendidoEntregado { get; set; }

        public decimal TotalCobrado { get; set; }

        public decimal PorcentajeCobranza { get; set; }
    }
    public class IngresoMensualDto
    {
        public int Mes { get; set; }

        public string NombreMes { get; set; }
            = string.Empty;

        public decimal Total { get; set; }
    }
    public class PedidosPorEstadoDto
    {
        public string Estado { get; set; }
            = string.Empty;

        public int Cantidad { get; set; }
    }
    public class ClienteMayorDeudaDto
    {
        public int IdCliente { get; set; }

        public string Cliente { get; set; }
            = string.Empty;

        public decimal DeudaPendiente { get; set; }

        public int CantidadPedidosConDeuda { get; set; }
    }

        public class ProductoStockBajoDto
        {
            public int IdProducto { get; set; }

            public string Producto { get; set; }
                = string.Empty;

            public int Stock { get; set; }

            public decimal Precio { get; set; }

            public string Estado { get; set; }
                = string.Empty;
        }
   public class UltimoPagoDto
        {
            public int IdPago { get; set; }

            public int IdPedido { get; set; }

            public string Cliente { get; set; }
                = string.Empty;

            public string Usuario { get; set; }
                = string.Empty;

            public string TipoPago { get; set; }
                = string.Empty;

            public DateTime FechaPago { get; set; }

            public decimal MontoPagado { get; set; }

            public decimal SaldoPendiente { get; set; }
        }

    public class UltimoPedidoDto
    {
        public int IdPedido { get; set; }

        public string Cliente { get; set; }
            = string.Empty;

        public string Sucursal { get; set; }
            = string.Empty;

        public DateTime FechaPedido { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; }
            = string.Empty;
    }
    public class DashboardActividadDto
    {
        public List<PedidosPorEstadoDto>
            PedidosPorEstado
        { get; set; }
                = new();

        public List<ClienteMayorDeudaDto>
            ClientesMayorDeuda
        { get; set; }
                = new();

        public List<ProductoStockBajoDto>
            ProductosStockBajo
        { get; set; }
                = new();

        public List<UltimoPagoDto>
            UltimosPagos
        { get; set; }
                = new();

        public List<UltimoPedidoDto>
            UltimosPedidos
        { get; set; }
                = new();
    }
}
