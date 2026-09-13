using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    public class PagoCreateDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdPedido { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdUsuario { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdTipoPago { get; set; }

        [Required]
        [Range(
            0.01,
            999999999,
            ErrorMessage =
                "El monto pagado debe ser mayor a cero."
        )]
        public decimal MontoPagado { get; set; }
    }
    
        public class PagoUpdateDto
        {
            [Required]
            [Range(1, int.MaxValue)]
            public int IdTipoPago { get; set; }

            [Required]
            [Range(
                0.01,
                999999999,
                ErrorMessage =
                    "El monto pagado debe ser mayor a cero."
            )]
            public decimal MontoPagado { get; set; }
        
    }
    public class DeudaPedidoDto
    {
        public int IdPedido { get; set; }

        public int IdCliente { get; set; }

        public string Cliente { get; set; }
            = string.Empty;

        public int IdSucursal { get; set; }

        public string Sucursal { get; set; }
            = string.Empty;

        public DateTime FechaPedido { get; set; }

        public string EstadoPedido { get; set; }
            = string.Empty;

        public decimal TotalPedido { get; set; }

        public decimal TotalPagado { get; set; }

        public decimal SaldoPendiente { get; set; }

        public string EstadoDeuda { get; set; }
            = string.Empty;

        public int CantidadPagos { get; set; }
    }
    public class ProductoDeudaDetalleDto
    {
        public int IdDetalle { get; set; }

        public int IdProducto { get; set; }

        public string Producto { get; set; }
            = string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }

        public string Estado { get; set; }
            = string.Empty;
    }
    public class PagoDetalleDto
    {
        public int Id { get; set; }
        public int IdPedido { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? CorreoUsuario { get; set; }
        public int IdTipoPago { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
    public class PagoPedidoUsuarioDto
    {
        public int IdPago { get; set; }
        public int IdPedido { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public decimal TotalPedido { get; set; }
        public string EstadoPedido { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string EstadoPago { get; set; } = string.Empty;
    }
    public class PagoRespuestaDto
    {
        public int Id { get; set; }
        public int IdPedido { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public int IdTipoPago { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
    public class PagosPorPedidoDto
    {
        public int IdPedido { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public int IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public DateTime FechaPedido { get; set; }
        public string EstadoPedido { get; set; } = string.Empty;
        public decimal TotalPedido { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendienteActual { get; set; }
        public int CantidadPagos { get; set; }
        public List<PagoRespuestaDto> Pagos { get; set; } = new();
    }
    public class PagoHistorialAdminDto
    {
        public int Id { get; set; }

        public int IdUsuario { get; set; }

        public string Usuario { get; set; }
            = string.Empty;

        public string? CorreoUsuario { get; set; }

        public int IdTipoPago { get; set; }

        public string TipoPago { get; set; }
            = string.Empty;

        public DateTime FechaPago { get; set; }

        public decimal MontoPagado { get; set; }

        public decimal SaldoPendiente { get; set; }

        public string Estado { get; set; }
            = string.Empty;
    }
    public class DetalleAdministrativoDeudaDto
    {
        public int IdPedido { get; set; }

        public int IdCliente { get; set; }

        public string Cliente { get; set; }
            = string.Empty;

        public int IdSucursal { get; set; }

        public string Sucursal { get; set; }
            = string.Empty;

        public DateTime FechaPedido { get; set; }

        public string EstadoPedido { get; set; }
            = string.Empty;

        public string? Observacion { get; set; }

        public decimal TotalPedido { get; set; }

        public decimal TotalPagado { get; set; }

        public decimal SaldoPendiente { get; set; }

        public string EstadoDeuda { get; set; }
            = string.Empty;

        public int CantidadPagos { get; set; }

        public EntregaPedidoDetalleDto? Entrega
        {
            get;
            set;
        }

        public List<ProductoDeudaDetalleDto> Productos
        {
            get;
            set;
        } = new();

        public List<PagoHistorialAdminDto> Pagos
        {
            get;
            set;
        } = new();
    }
    // =========================================================
    // PEDIDO DISPONIBLE PARA COBRAR - DISTRIBUIDOR
    // =========================================================

    public class PedidoCobrableDistribuidorDto
    {
        public int IdPedido { get; set; }

        public int IdCliente { get; set; }

        public string Cliente { get; set; }
            = string.Empty;

        public int IdSucursal { get; set; }

        public string Sucursal { get; set; }
            = string.Empty;

        public string Ubicacion { get; set; }
            = string.Empty;

        public DateTime FechaPedido { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public string EstadoPedido { get; set; }
            = string.Empty;

        public decimal TotalPedido { get; set; }

        public decimal TotalPagado { get; set; }

        public decimal SaldoPendiente { get; set; }

        public int CantidadPagos { get; set; }
    }


    // =========================================================
    // COBRO INDIVIDUAL DEL DISTRIBUIDOR
    // =========================================================

    public class CobroDistribuidorDetalleDto
    {
        public int IdPago { get; set; }

        public int IdUsuario { get; set; }

        public string Usuario { get; set; }
            = string.Empty;

        public int IdTipoPago { get; set; }

        public string TipoPago { get; set; }
            = string.Empty;

        public DateTime FechaPago { get; set; }

        public decimal MontoPagado { get; set; }

        public decimal SaldoPendienteDespuesPago
        {
            get;
            set;
        }

        public string EstadoPago { get; set; }
            = string.Empty;
    }


    // =========================================================
    // LISTADO AGRUPADO DE COBROS POR PEDIDO
    // =========================================================

    public class CobrosPedidoDistribuidorDto
    {
        public int IdPedido { get; set; }

        public int IdCliente { get; set; }

        public string Cliente { get; set; }
            = string.Empty;

        public int IdSucursal { get; set; }

        public string Sucursal { get; set; }
            = string.Empty;

        public string Ubicacion { get; set; }
            = string.Empty;

        public DateTime FechaPedido { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public string EstadoPedido { get; set; }
            = string.Empty;

        public decimal TotalPedido { get; set; }

        public decimal TotalPagadoPedido
        {
            get;
            set;
        }

        public decimal SaldoPendiente { get; set; }

        public string EstadoDeuda { get; set; }
            = string.Empty;

        public int CantidadCobros { get; set; }

        public decimal TotalCobradoPorMi
        {
            get;
            set;
        }

        public DateTime? UltimoCobro { get; set; }

        public List<CobroDistribuidorDetalleDto>
            HistorialCobros
        {
            get;
            set;
        } = new();
    }
}
