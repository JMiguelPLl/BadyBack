using System.Security.Claims;
using BadyApi.Helpers;
using BadyBackend.Data;
using BadyBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static BadyBackend.DTOs.DistribuidorPedidoDtos;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Distribuidor")]
    public class DistribuidorPedidoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DistribuidorPedidoController(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // OBTENER USUARIO LOGUEADO DESDE JWT
        // =====================================================

        private int? ObtenerIdUsuarioActual()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (string.IsNullOrWhiteSpace(claim))
            {
                return null;
            }

            if (!int.TryParse(
                    claim,
                    out var idUsuario))
            {
                return null;
            }

            return idUsuario;
        }


        // =====================================================
        // LISTAR MIS PEDIDOS
        // =====================================================
        //
        // GET:
        // api/DistribuidorPedido/MisPedidos
        //
        // FILTROS:
        //
        // api/DistribuidorPedido/MisPedidos?estado=Asignado
        // api/DistribuidorPedido/MisPedidos?estado=EnCamino
        // api/DistribuidorPedido/MisPedidos?estado=PorConfirmarEntrega
        // api/DistribuidorPedido/MisPedidos?estado=Entregado
        //
        // IMPORTANTE:
        //
        // Ya NO se buscan los vehículos que el distribuidor
        // tiene activos actualmente.
        //
        // Se consulta Asignacion_Pedido_Usuarios para conocer
        // los pedidos en los que realmente participó.
        //
        // Así el historial nunca cambia cuando mañana el
        // distribuidor sea asignado a otro vehículo.
        // =====================================================

        [HttpGet("MisPedidos")]
        public async Task<ActionResult<
            IEnumerable<DistribuidorPedidoListaDto>>>
            ListarMisPedidos(
                [FromQuery] string? estado)
        {
            var idUsuario =
                ObtenerIdUsuarioActual();

            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "No se pudo identificar al usuario autenticado."
                });
            }


            // =================================================
            // CONSULTA HISTÓRICA DEL DISTRIBUIDOR
            // =================================================
            //
            // El pedido pertenece al distribuidor si existe una
            // fila histórica en Asignacion_Pedido_Usuarios cuyo
            // AsignacionVehiculo apunta al usuario autenticado.
            // =================================================

            var consulta =
                _context
                    .Asignacion_Pedidos
                    .AsNoTracking()
                    .AsSplitQuery()

                    .Include(a =>
                        a.Asignacion_vehiculo
                    )
                        .ThenInclude(av =>
                            av.Vehiculo
                        )

                    .Include(a =>
                        a.Pedido
                    )
                        .ThenInclude(p =>
                            p.Cliente
                        )

                    .Include(a =>
                        a.Pedido
                    )
                        .ThenInclude(p =>
                            p.Sucursal
                        )

                    .Include(a =>
                        a.Pedido
                    )
                        .ThenInclude(p =>
                            p.Detalle_Pedidos
                        )

                    .Where(a =>
                        a.Asignacion_Pedido_Usuarios
                            .Any(h =>
                                h.AsignacionVehiculo
                                    .id_usuario ==
                                idUsuario.Value
                            )
                    );


            // =================================================
            // FILTRAR POR ESTADO DEL PEDIDO
            // =================================================

            if (
                !string.IsNullOrWhiteSpace(
                    estado
                )
            )
            {
                var estadoNormalizado =
                    EstadosPedido
                        .Normalizar(
                            estado
                        );

                if (estadoNormalizado == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "El estado indicado no es válido."
                    });
                }

                consulta =
                    consulta.Where(a =>
                        a.Pedido.Estado ==
                            estadoNormalizado
                    );
            }


            var asignaciones =
                await consulta

                    .OrderByDescending(a =>
                        a.Fecha_Asignacion
                    )

                    .ThenByDescending(a =>
                        a.Id
                    )

                    .ToListAsync();


            var resultado =
                asignaciones

                    .Select(a =>
                        new DistribuidorPedidoListaDto
                        {
                            IdAsignacionPedido =
                                a.Id,

                            IdPedido =
                                a.id_pedido,

                            IdCliente =
                                a.Pedido
                                    .Id_cliente,

                            Cliente =
                                a.Pedido
                                    .Cliente
                                    ?.Nombre
                                ??
                                string.Empty,

                            IdSucursal =
                                a.Pedido
                                    .Id_sucursal,

                            Sucursal =
                                a.Pedido
                                    .Sucursal
                                    ?.Nombre
                                ??
                                string.Empty,

                            Ubicacion =
                                a.Pedido
                                    .Sucursal
                                    ?.Ubicacion
                                ??
                                string.Empty,

                            IdVehiculo =
                                a.Asignacion_vehiculo
                                    .id_vehiculo,

                            Vehiculo =
                                a.Asignacion_vehiculo
                                    .Vehiculo
                                    ?.Marca
                                ??
                                string.Empty,

                            Placa =
                                a.Asignacion_vehiculo
                                    .Vehiculo
                                    ?.Placa,

                            FechaPedido =
                                a.Pedido.Fecha,

                            FechaAsignacion =
                                a.Fecha_Asignacion,

                            FechaEntrega =
                                a.Fecha_Entrega,

                            TotalPedido =
                                a.Pedido.Total,

                            CantidadTotalProductos =
                                a.Pedido
                                    .Detalle_Pedidos
                                    .Sum(d =>
                                        d.Cantidad
                                    ),

                            EstadoPedido =
                                a.Pedido.Estado,

                            EstadoAsignacion =
                                a.Estado
                        }
                    )

                    .ToList();


            return Ok(resultado);
        }


        // =====================================================
        // DETALLE COMPLETO DEL PEDIDO
        // =====================================================
        //
        // GET:
        // api/DistribuidorPedido/5/Detalle
        //
        // 5 = IdAsignacionPedido
        //
        // IMPORTANTE:
        //
        // El personal mostrado es el PERSONAL HISTÓRICO
        // guardado cuando se asignó el pedido.
        //
        // NO se consulta el personal activo actual del vehículo.
        // =====================================================

        [HttpGet("{idAsignacionPedido:int}/Detalle")]
        public async Task<ActionResult<
            DistribuidorPedidoDetalleDto>>
            ObtenerDetalle(
                int idAsignacionPedido)
        {
            var idUsuario =
                ObtenerIdUsuarioActual();

            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "No se pudo identificar al usuario autenticado."
                });
            }


            // =================================================
            // BUSCAR ASIGNACIÓN
            // =================================================
            //
            // La seguridad se basa en el historial:
            // el usuario debe estar registrado en
            // Asignacion_Pedido_Usuarios para este pedido.
            // =================================================

            var asignacion =
                await _context
                    .Asignacion_Pedidos
                    .AsNoTracking()
                    .AsSplitQuery()

                    .Include(a =>
                        a.Asignacion_vehiculo
                    )
                        .ThenInclude(av =>
                            av.Vehiculo
                        )

                    .Include(a =>
                        a.Asignacion_Pedido_Usuarios
                    )
                        .ThenInclude(h =>
                            h.AsignacionVehiculo
                        )
                            .ThenInclude(av =>
                                av.Usuario
                            )

                    .Include(a =>
                        a.Pedido
                    )
                        .ThenInclude(p =>
                            p.Cliente
                        )

                    .Include(a =>
                        a.Pedido
                    )
                        .ThenInclude(p =>
                            p.Sucursal
                        )

                    .Include(a =>
                        a.Pedido
                    )
                        .ThenInclude(p =>
                            p.Detalle_Pedidos
                        )
                            .ThenInclude(d =>
                                d.Producto
                            )

                    .FirstOrDefaultAsync(a =>
                        a.Id ==
                            idAsignacionPedido
                        &&
                        a.Asignacion_Pedido_Usuarios
                            .Any(h =>
                                h.AsignacionVehiculo
                                    .id_usuario ==
                                idUsuario.Value
                            )
                    );


            if (asignacion == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido no existe o no fue asignado a este distribuidor."
                });
            }


            var idVehiculo =
                asignacion
                    .Asignacion_vehiculo
                    .id_vehiculo;


            // =================================================
            // PERSONAL HISTÓRICO DEL PEDIDO
            // =================================================
            //
            // ANTES:
            //
            // Asignacion_Vehiculos
            //   WHERE id_vehiculo = ...
            //   AND Estado = Activo
            //
            // Eso mostraba el personal de HOY.
            //
            // AHORA:
            //
            // Asignacion_Pedido_Usuarios
            //   -> AsignacionVehiculo
            //   -> Usuario
            //
            // Esto muestra exactamente quién estaba asignado
            // cuando se creó esta asignación de pedido.
            // =================================================

            var personal =
                asignacion
                    .Asignacion_Pedido_Usuarios

                    .Where(h =>
                        string.Equals(
                            h.Estado,
                            "Activo",
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                    )

                    .OrderBy(h =>
                        h.AsignacionVehiculo
                            .Usuario
                            .Nombre
                    )

                    .Select(h =>
                        new PersonalVehiculoPedidoDto
                        {
                            IdUsuario =
                                h.AsignacionVehiculo
                                    .id_usuario,

                            Usuario =
                                h.AsignacionVehiculo
                                    .Usuario
                                    .Nombre,

                            Correo =
                                h.AsignacionVehiculo
                                    .Usuario
                                    .Email
                        }
                    )

                    .ToList();


            // =================================================
            // TOTAL PAGADO
            // =================================================

            var totalPagado =
                await _context.Pagos
                    .AsNoTracking()

                    .Where(p =>
                        p.Id_pedido ==
                            asignacion.id_pedido
                        &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )

                    .SumAsync(p =>
                        (decimal?)
                            p.MontoPagado
                    )
                ?? 0m;


            // =================================================
            // SALDO REAL CALCULADO EN BACKEND
            // =================================================

            var saldoPendiente =
                Math.Max(
                    0,
                    asignacion.Pedido.Total -
                    totalPagado
                );


            // =================================================
            // PRODUCTOS
            // =================================================

            var productos =
                asignacion
                    .Pedido
                    .Detalle_Pedidos

                    .Select(d =>
                        new DistribuidorProductoPedidoDto
                        {
                            IdDetalle =
                                d.Id,

                            IdProducto =
                                d.Id_producto,

                            Producto =
                                d.Producto
                                    ?.Nombre
                                ??
                                string.Empty,

                            Descripcion =
                                d.Producto
                                    ?.Descripcion,

                            Cantidad =
                                d.Cantidad,

                            PrecioUnitario =
                                d.Cantidad > 0
                                    ?
                                    d.Subtotal /
                                    d.Cantidad
                                    :
                                    0,

                            Subtotal =
                                d.Subtotal,

                            Estado =
                                d.Estado
                        }
                    )

                    .ToList();


            return Ok(
                new DistribuidorPedidoDetalleDto
                {
                    IdAsignacionPedido =
                        asignacion.Id,

                    IdPedido =
                        asignacion.id_pedido,

                    IdCliente =
                        asignacion
                            .Pedido
                            .Id_cliente,

                    Cliente =
                        asignacion
                            .Pedido
                            .Cliente
                            ?.Nombre
                        ??
                        string.Empty,


                    IdSucursal =
                        asignacion
                            .Pedido
                            .Id_sucursal,

                    Sucursal =
                        asignacion
                            .Pedido
                            .Sucursal
                            ?.Nombre
                        ??
                        string.Empty,

                    Ubicacion =
                        asignacion
                            .Pedido
                            .Sucursal
                            ?.Ubicacion
                        ??
                        string.Empty,


                    ObservacionPedido =
                        asignacion
                            .Pedido
                            .Observacion,

                    FechaPedido =
                        asignacion
                            .Pedido
                            .Fecha,

                    TotalPedido =
                        asignacion
                            .Pedido
                            .Total,

                    EstadoPedido =
                        asignacion
                            .Pedido
                            .Estado,


                    IdVehiculo =
                        idVehiculo,

                    Vehiculo =
                        asignacion
                            .Asignacion_vehiculo
                            .Vehiculo
                            ?.Marca
                        ??
                        string.Empty,

                    Placa =
                        asignacion
                            .Asignacion_vehiculo
                            .Vehiculo
                            ?.Placa,

                    CantidadCarga =
                        asignacion
                            .Asignacion_vehiculo
                            .Vehiculo
                            ?.Cantidad_Carga
                        ??
                        string.Empty,


                    FechaAsignacion =
                        asignacion
                            .Fecha_Asignacion,

                    FechaEntrega =
                        asignacion
                            .Fecha_Entrega,

                    EstadoAsignacion =
                        asignacion
                            .Estado,


                    CantidadTotalProductos =
                        productos.Sum(p =>
                            p.Cantidad
                        ),


                    TotalPagado =
                        totalPagado,

                    SaldoPendiente =
                        saldoPendiente,

                    EstadoDeuda =
                        saldoPendiente <= 0
                            ? "Pagado"
                            : "Pendiente",

                    PuedeRegistrarPago =
                        asignacion.Pedido.Estado ==
                            EstadosPedido
                                .PorConfirmarEntrega
                        ||
                        asignacion.Pedido.Estado ==
                            EstadosPedido
                                .Entregado,


                    Productos =
                        productos,

                    PersonalVehiculo =
                        personal
                }
            );
        }


        // =====================================================
        // CAMBIAR A EN CAMINO
        // =====================================================
        //
        // Pedido:
        //
        // Asignado
        //     ↓
        // EnCamino
        //
        // Asignacion_Pedido sigue:
        // Asignado
        //
        // PATCH:
        // api/DistribuidorPedido/5/EnCamino
        //
        // =====================================================

        [HttpPatch("{idAsignacionPedido:int}/EnCamino")]
        public async Task<IActionResult>
            MarcarEnCamino(
                int idAsignacionPedido)
        {
            var idUsuario =
                ObtenerIdUsuarioActual();

            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "No se pudo identificar al usuario autenticado."
                });
            }


            var asignacion =
                await ObtenerAsignacionDelDistribuidor(
                    idAsignacionPedido,
                    idUsuario.Value
                );


            if (asignacion == null)
            {
                return NotFound(new
                {
                    message =
                        "La asignación no existe o no pertenece a este distribuidor."
                });
            }


            // =================================================
            // VALIDAR ASIGNACIÓN
            // =================================================

            if (
                asignacion.Estado !=
                EstadosAsignacionPedido
                    .Asignado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Esta asignación ya no se encuentra disponible para reparto."
                });
            }


            // =================================================
            // YA ESTÁ EN CAMINO
            // =================================================

            if (
                asignacion.Pedido.Estado ==
                EstadosPedido.EnCamino
            )
            {
                return BadRequest(new
                {
                    message =
                        "El pedido ya se encuentra en camino."
                });
            }


            // =================================================
            // SOLO ASIGNADO PUEDE PASAR A EN CAMINO
            // =================================================

            if (
                asignacion.Pedido.Estado !=
                EstadosPedido.Asignado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Solo un pedido en estado Asignado puede pasar a EnCamino."
                });
            }


            // =================================================
            // CAMBIO
            // =================================================

            asignacion.Pedido.Estado =
                EstadosPedido.EnCamino;


            await _context
                .SaveChangesAsync();


            return Ok(
                new CambioEstadoDistribuidorDto
                {
                    IdAsignacionPedido =
                        asignacion.Id,

                    IdPedido =
                        asignacion.id_pedido,

                    EstadoAsignacion =
                        asignacion.Estado,

                    EstadoPedido =
                        asignacion
                            .Pedido
                            .Estado,

                    FechaEntrega =
                        asignacion
                            .Fecha_Entrega,

                    Mensaje =
                        "El pedido fue marcado como En camino."
                }
            );
        }


        // =====================================================
        // MARCAR COMO ENTREGADO
        // =====================================================
        //
        // DISTRIBUIDOR:
        //
        // Pedido:
        // EnCamino
        //      ↓
        // PorConfirmarEntrega
        //
        //
        // Asignacion_Pedido:
        // Asignado
        //      ↓
        // Entregado
        //
        //
        // FechaEntrega:
        // DateTime.UtcNow
        //
        //
        // EL CLIENTE TODAVÍA NO CONFIRMÓ.
        //
        // =====================================================

        [HttpPatch("{idAsignacionPedido:int}/Entregar")]
        public async Task<IActionResult>
            MarcarEntregado(
                int idAsignacionPedido)
        {
            var idUsuario =
                ObtenerIdUsuarioActual();

            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "No se pudo identificar al usuario autenticado."
                });
            }


            var asignacion =
                await ObtenerAsignacionDelDistribuidor(
                    idAsignacionPedido,
                    idUsuario.Value
                );


            if (asignacion == null)
            {
                return NotFound(new
                {
                    message =
                        "La asignación no existe o no pertenece a este distribuidor."
                });
            }


            // =================================================
            // YA ENTREGADO
            // =================================================

            if (
                asignacion.Estado ==
                EstadosAsignacionPedido
                    .Entregado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Este pedido ya fue marcado como entregado."
                });
            }


            // =================================================
            // ASIGNACIÓN DEBE ESTAR ACTIVA
            // =================================================

            if (
                asignacion.Estado !=
                EstadosAsignacionPedido
                    .Asignado
            )
            {
                return BadRequest(new
                {
                    message =
                        "La asignación no se encuentra en estado Asignado."
                });
            }


            // =================================================
            // DEBE ESTAR EN CAMINO
            // =================================================

            if (
                asignacion.Pedido.Estado !=
                EstadosPedido.EnCamino
            )
            {
                return BadRequest(new
                {
                    message =
                        "Primero debes marcar el pedido como En camino."
                });
            }


            // =================================================
            // ASIGNACIÓN = ENTREGADO
            // =================================================

            asignacion.Estado =
                EstadosAsignacionPedido
                    .Entregado;


            asignacion.Fecha_Entrega =
                DateTime.UtcNow;


            // =================================================
            // PEDIDO = POR CONFIRMAR ENTREGA
            // =================================================
            //
            // MUY IMPORTANTE:
            //
            // El distribuidor NO lo pone Entregado.
            //
            // El cliente debe confirmarlo.
            //
            // =================================================

            asignacion.Pedido.Estado =
                EstadosPedido
                    .PorConfirmarEntrega;


            await _context
                .SaveChangesAsync();


            return Ok(
                new CambioEstadoDistribuidorDto
                {
                    IdAsignacionPedido =
                        asignacion.Id,

                    IdPedido =
                        asignacion.id_pedido,

                    EstadoAsignacion =
                        asignacion.Estado,

                    EstadoPedido =
                        asignacion
                            .Pedido
                            .Estado,

                    FechaEntrega =
                        asignacion
                            .Fecha_Entrega,

                    Mensaje =
                        "Pedido marcado como entregado. Ahora está pendiente de confirmación por parte del cliente."
                }
            );
        }


        // =====================================================
        // CONSULTA INTERNA SEGURA
        // =====================================================
        //
        // ANTES:
        //
        // se comprobaba el vehículo ACTIVO actual del usuario.
        //
        // Eso rompía el historial si mañana el distribuidor
        // era movido a otro vehículo.
        //
        // AHORA:
        //
        // se comprueba que el usuario figure en el PERSONAL
        // HISTÓRICO de esta asignación de pedido.
        //
        // =====================================================

        private async Task<Asignacion_Pedido?>
            ObtenerAsignacionDelDistribuidor(
                int idAsignacionPedido,
                int idUsuario)
        {
            return await _context
                .Asignacion_Pedidos

                .Include(a =>
                    a.Pedido
                )

                .Include(a =>
                    a.Asignacion_vehiculo
                )
                    .ThenInclude(av =>
                        av.Vehiculo
                    )

                .Include(a =>
                    a.Asignacion_Pedido_Usuarios
                )
                    .ThenInclude(h =>
                        h.AsignacionVehiculo
                    )

                .FirstOrDefaultAsync(a =>
                    a.Id ==
                        idAsignacionPedido
                    &&
                    a.Asignacion_Pedido_Usuarios
                        .Any(h =>
                            h.AsignacionVehiculo
                                .id_usuario ==
                            idUsuario
                        )
                );
        }
    }
}