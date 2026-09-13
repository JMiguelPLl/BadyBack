using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
   
        public class CrearPedidoDto
        {
            [Range(1, int.MaxValue, ErrorMessage = "El cliente es obligatorio.")]
            public int IdCliente { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "La sucursal es obligatoria.")]
            public int IdSucursal { get; set; }

            [StringLength(200)]
            public string? Observacion { get; set; }

            [Required]
            [MinLength(1, ErrorMessage = "Debe agregar al menos un producto.")]
            public List<CrearDetallePedidoDto> Detalles { get; set; } = new();
        }
    public class PedidoRespuestaDto
    {
        public int Id { get; set; }

        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;

        public int IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;

        public DateTime FechaPedido { get; set; }
        public string? Observacion { get; set; }
        public string? MotivoEdicion { get; set; }
        public string? MotivoDevolucion { get; set; }
        public DateTime? FechaDevolucion { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } = string.Empty;

        // Null mientras el pedido no esté Entregado.
        public decimal? SaldoPendiente { get; set; }

        public List<DetallePedidoRespuestaDto> Detalles { get; set; } = new();
    }
    public class CrearDetallePedidoDto
        {
            [Range(1, int.MaxValue, ErrorMessage = "El producto es obligatorio.")]
            public int IdProducto { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
            public int Cantidad { get; set; }
        }

        public class DetallePedidoRespuestaDto
        {
            public int Id { get; set; }
            public int IdProducto { get; set; }
            public string Producto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Subtotal { get; set; }
            public string Estado { get; set; } = string.Empty;
        }
        public class ActualizarPedidoDto
        {
            [Range(1, int.MaxValue, ErrorMessage = "El cliente es obligatorio.")]
            public int IdCliente { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "La sucursal es obligatoria.")]
            public int IdSucursal { get; set; }

            [StringLength(200)]
            public string? Observacion { get; set; }

            [StringLength(500)]
            public string? MotivoEdicion { get; set; }

            [Required]
            [MinLength(1, ErrorMessage = "Debe agregar al menos un producto.")]
            public List<CrearDetallePedidoDto> Detalles { get; set; } = new();
        }
    public class CambiarEstadoPedidoDto
    {
        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string Estado { get; set; } = string.Empty;
    }
    public class EntregaPedidoDetalleDto
    {
        public int? IdAsignacionPedido { get; set; }

        public int? IdVehiculo { get; set; }

        public string? Vehiculo { get; set; }

        public string? Placa { get; set; }

        public DateTime? FechaAsignacion { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public string? EstadoAsignacion { get; set; }

        public bool ConfirmadoPorCliente { get; set; }

        /*
         * No tenemos FechaConfirmacion porque decidimos
         * no almacenarla en Pedido.
         */

        public List<PersonalEntregaDto> Personal
        {
            get;
            set;
        } = new();
    }

    public class RechazarEntregaPedidoDto
    {
        [Required(ErrorMessage = "El motivo de rechazo o devolución es obligatorio.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "El motivo debe tener entre 3 y 500 caracteres.")]
        public string Motivo { get; set; } = string.Empty;
    }
}

