using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    public static class DistribuidorPedidoDtos
    {
        // =====================================================
        // LISTADO DE PEDIDOS DEL DISTRIBUIDOR
        // =====================================================

        public class DistribuidorPedidoListaDto
        {
            public int IdAsignacionPedido { get; set; }

            public int IdPedido { get; set; }

            public int IdCliente { get; set; }

            public string Cliente { get; set; }
                = string.Empty;

            public int IdSucursal { get; set; }

            public string Sucursal { get; set; }
                = string.Empty;

            public string Ubicacion { get; set; }
                = string.Empty;

            public int IdVehiculo { get; set; }

            public string Vehiculo { get; set; }
                = string.Empty;

            public string? Placa { get; set; }

            public DateTime FechaPedido { get; set; }

            public DateTime FechaAsignacion { get; set; }

            public DateTime? FechaEntrega { get; set; }

            public decimal TotalPedido { get; set; }

            public int CantidadTotalProductos { get; set; }

            public string EstadoPedido { get; set; }
                = string.Empty;

            public string EstadoAsignacion { get; set; }
                = string.Empty;
        }


        // =====================================================
        // PRODUCTOS DEL PEDIDO
        // =====================================================

        public class DistribuidorProductoPedidoDto
        {
            public int IdDetalle { get; set; }

            public int IdProducto { get; set; }

            public string Producto { get; set; }
                = string.Empty;

            public string? Descripcion { get; set; }

            public int Cantidad { get; set; }

            public decimal PrecioUnitario { get; set; }

            public decimal Subtotal { get; set; }

            public string Estado { get; set; }
                = string.Empty;
        }


        // =====================================================
        // PERSONAL DEL VEHÍCULO
        // =====================================================

        public class PersonalVehiculoPedidoDto
        {
            public int IdUsuario { get; set; }

            public string Usuario { get; set; }
                = string.Empty;

            public string? Correo { get; set; }
        }


        // =====================================================
        // DETALLE COMPLETO PARA DISTRIBUIDOR
        // =====================================================

        public class DistribuidorPedidoDetalleDto
        {
            public int IdAsignacionPedido { get; set; }

            public int IdPedido { get; set; }

            public int IdCliente { get; set; }

            public string Cliente { get; set; }
                = string.Empty;


            // =================================================
            // SUCURSAL
            // =================================================

            public int IdSucursal { get; set; }

            public string Sucursal { get; set; }
                = string.Empty;

            public string Ubicacion { get; set; }
                = string.Empty;


            // =================================================
            // PEDIDO
            // =================================================

            public string? ObservacionPedido { get; set; }

            public DateTime FechaPedido { get; set; }

            public decimal TotalPedido { get; set; }

            public string EstadoPedido { get; set; }
                = string.Empty;


            // =================================================
            // VEHÍCULO
            // =================================================

            public int IdVehiculo { get; set; }

            public string Vehiculo { get; set; }
                = string.Empty;

            public string? Placa { get; set; }

            public string CantidadCarga { get; set; }
                = string.Empty;


            // =================================================
            // ASIGNACIÓN
            // =================================================

            public DateTime FechaAsignacion { get; set; }

            public DateTime? FechaEntrega { get; set; }

            public string EstadoAsignacion { get; set; }
                = string.Empty;


            // =================================================
            // PRODUCTOS
            // =================================================

            public int CantidadTotalProductos { get; set; }

            public List<DistribuidorProductoPedidoDto>
                Productos
            { get; set; }
                    = new();


            // =================================================
            // PERSONAL DEL VEHÍCULO
            // =================================================

            public List<PersonalVehiculoPedidoDto>
                PersonalVehiculo
            { get; set; }
                    = new();


            // =================================================
            // COBRANZA
            // =================================================

            public decimal TotalPagado { get; set; }

            public decimal SaldoPendiente { get; set; }

            public string EstadoDeuda { get; set; }
                = string.Empty;

            public bool PuedeRegistrarPago { get; set; }
        }


        // =====================================================
        // RESPUESTA AL CAMBIAR ESTADO
        // =====================================================

        public class CambioEstadoDistribuidorDto
        {
            public int IdAsignacionPedido { get; set; }

            public int IdPedido { get; set; }

            public string EstadoAsignacion { get; set; }
                = string.Empty;

            public string EstadoPedido { get; set; }
                = string.Empty;

            public DateTime? FechaEntrega { get; set; }

            public string Mensaje { get; set; }
                = string.Empty;
        }


        // =====================================================
        // NOTIFICACIÓN AL CLIENTE
        // =====================================================

        public class NotificacionEntregaClienteDto
        {
            public int IdCliente { get; set; }

            public int IdPedido { get; set; }

            public string Titulo { get; set; }
                = string.Empty;

            public string Mensaje { get; set; }
                = string.Empty;

            public string Tipo { get; set; }
                = string.Empty;

            public DateTime Fecha { get; set; }

            public bool RequiereConfirmacion { get; set; }
        }


        // =====================================================
        // CONFIRMACIÓN DEL CLIENTE
        // =====================================================

        public class ConfirmacionEntregaClienteDto
        {
            [Required]
            public int IdPedido { get; set; }

            [Required]
            public bool Confirmado { get; set; }
        }
    }
}