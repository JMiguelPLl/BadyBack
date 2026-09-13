using System.Security.Claims;
using BadyApi.Helpers;
using BadyBackend.Data;
using BadyBackend.DTOs;
using BadyBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PagoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PagoController(
            AppDbContext context)
        {
            _context = context;
        }



        // =====================================================
        // USUARIO AUTENTICADO
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

            return int.TryParse(
                claim,
                out var idUsuario
            )
                ? idUsuario
                : null;
        }


        // =====================================================
        // LISTADO ADMINISTRATIVO DE DEUDAS
        // =====================================================

        /*
         * Ejemplos:
         *
         * GET:
         * api/Pago/Deudas
         *
         * api/Pago/Deudas?estado=Pendiente
         *
         * api/Pago/Deudas?estado=Pagado
         *
         * api/Pago/Deudas?cliente=Juan
         *
         * api/Pago/Deudas?fechaDesde=2026-08-01
         *                &fechaHasta=2026-08-31
         */
        [Authorize(Roles = "Administrador")]
        [HttpGet("Deudas")]
        public async Task<ActionResult<
            IEnumerable<DeudaPedidoDto>>>
            ListarDeudas(
                [FromQuery] string? estado,
                [FromQuery] string? cliente,
                [FromQuery] DateTime? fechaDesde,
                [FromQuery] DateTime? fechaHasta)
        {
            var estadoNormalizado =
                string.IsNullOrWhiteSpace(estado)
                    ? null
                    : EstadosDeuda
                        .Normalizar(estado);

            if (
                !string.IsNullOrWhiteSpace(estado) &&
                estadoNormalizado == null
            )
            {
                return BadRequest(new
                {
                    message =
                        "El estado de deuda solamente puede ser Pendiente o Pagado."
                });
            }


            /*
             * Consideramos cuentas cobrables cuando el pedido
             * ya fue físicamente entregado o confirmado.
             *
             * PorConfirmarEntrega:
             * distribuidor entregó, cliente aún no confirmó.
             *
             * Entregado:
             * cliente confirmó.
             */
            var consultaPedidos =
                _context.Pedidos
                    .AsNoTracking()
                    .Include(p => p.Cliente)
                    .Include(p => p.Sucursal)
                    .Where(p =>
                        p.Estado ==
                            EstadosPedido
                                .PorConfirmarEntrega ||
                        p.Estado ==
                            EstadosPedido
                                .Entregado
                    );


            if (
                !string.IsNullOrWhiteSpace(cliente)
            )
            {
                var busqueda =
                    cliente.Trim().ToLower();

                consultaPedidos =
                    consultaPedidos.Where(p =>
                        p.Cliente.Nombre
                            .ToLower()
                            .Contains(busqueda)
                    );
            }


            if (fechaDesde.HasValue)
            {
                var desde =
                    fechaDesde.Value.Date;

                consultaPedidos =
                    consultaPedidos.Where(p =>
                        p.Fecha >= desde
                    );
            }


            if (fechaHasta.HasValue)
            {
                var hasta =
                    fechaHasta.Value.Date
                        .AddDays(1);

                consultaPedidos =
                    consultaPedidos.Where(p =>
                        p.Fecha < hasta
                    );
            }


            var pedidos =
                await consultaPedidos
                    .OrderByDescending(
                        p => p.Fecha
                    )
                    .ToListAsync();


            var idsPedidos =
                pedidos
                    .Select(p => p.Id)
                    .ToList();


            /*
             * No confiamos en SaldoPendiente almacenado
             * para calcular la deuda actual.
             *
             * Sumamos pagos válidos directamente.
             */
            var resumenPagos =
                await _context.Pagos
                    .AsNoTracking()
                    .Where(p =>
                        idsPedidos.Contains(
                            p.Id_pedido
                        ) &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )
                    .GroupBy(p =>
                        p.Id_pedido
                    )
                    .Select(g => new
                    {
                        IdPedido = g.Key,

                        TotalPagado =
                            g.Sum(x =>
                                x.MontoPagado
                            ),

                        CantidadPagos =
                            g.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.IdPedido
                    );


            var resultado =
                new List<DeudaPedidoDto>();


            foreach (var pedido in pedidos)
            {
                resumenPagos.TryGetValue(
                    pedido.Id,
                    out var resumen
                );


                var totalPagado =
                    resumen?.TotalPagado ?? 0m;


                var saldo =
                    Math.Max(
                        0,
                        pedido.Total -
                        totalPagado
                    );


                var estadoDeuda =
                    saldo <= 0
                        ? EstadosDeuda.Pagado
                        : EstadosDeuda.Pendiente;


                if (
                    estadoNormalizado != null &&
                    estadoDeuda !=
                    estadoNormalizado
                )
                {
                    continue;
                }


                resultado.Add(
                    new DeudaPedidoDto
                    {
                        IdPedido =
                            pedido.Id,

                        IdCliente =
                            pedido.Id_cliente,

                        Cliente =
                            pedido.Cliente
                                ?.Nombre ??
                            string.Empty,

                        IdSucursal =
                            pedido.Id_sucursal,

                        Sucursal =
                            pedido.Sucursal
                                ?.Nombre ??
                            string.Empty,

                        FechaPedido =
                            pedido.Fecha,

                        EstadoPedido =
                            pedido.Estado,

                        TotalPedido =
                            pedido.Total,

                        TotalPagado =
                            totalPagado,

                        SaldoPendiente =
                            saldo,

                        EstadoDeuda =
                            estadoDeuda,

                        CantidadPagos =
                            resumen
                                ?.CantidadPagos ??
                            0
                    }
                );
            }


            return Ok(resultado);
        }


        // =====================================================
        // DETALLE ADMINISTRATIVO DE UNA DEUDA
        // =====================================================

        /*
         * Incluye:
         *
         * - pedido
         * - cliente
         * - sucursal
         * - productos
         * - vehículo
         * - personal asignado al vehículo
         * - fecha asignación
         * - fecha entrega
         * - historial completo de pagos
         * - total pagado
         * - saldo calculado
         *
         * GET:
         * api/Pago/Pedido/5/DetalleAdministrativo
         */
        [Authorize(Roles = "Administrador")]
        [HttpGet(
            "Pedido/{idPedido:int}/DetalleAdministrativo"
        )]
        public async Task<ActionResult<
            DetalleAdministrativoDeudaDto>>
            ObtenerDetalleAdministrativo(
                int idPedido)
        {
            var pedido =
                await _context.Pedidos
                    .AsNoTracking()
                    .Include(p =>
                        p.Cliente
                    )
                    .Include(p =>
                        p.Sucursal
                    )
                    .Include(p =>
                        p.Detalle_Pedidos
                    )
                        .ThenInclude(d =>
                            d.Producto
                        )
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            idPedido
                    );


            if (pedido == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido indicado no existe."
                });
            }


            var pagos =
                await ConstruirConsultaPagos()
                    .Where(p =>
                        p.Id_pedido ==
                        idPedido
                    )
                    .OrderBy(p =>
                        p.Fecha
                    )
                    .ThenBy(p =>
                        p.Id
                    )
                    .ToListAsync();


            var pagosValidos =
                pagos
                    .Where(p =>
                        p.Estado !=
                        EstadosPago.Anulado
                    )
                    .ToList();


            var totalPagado =
                pagosValidos.Sum(
                    p => p.MontoPagado
                );


            var saldoPendiente =
                Math.Max(
                    0,
                    pedido.Total -
                    totalPagado
                );


            /*
             * Última asignación del pedido.
             */
            var asignacionPedido =
                await _context
                    .Asignacion_Pedidos
                    .AsNoTracking()
                    .Include(a =>
                        a.Asignacion_vehiculo
                    )
                        .ThenInclude(av =>
                            av.Vehiculo
                        )
                    .Where(a =>
                        a.id_pedido ==
                        idPedido
                    )
                    .OrderByDescending(a =>
                        a.Fecha_Asignacion
                    )
                    .ThenByDescending(a =>
                        a.Id
                    )
                    .FirstOrDefaultAsync();


            EntregaPedidoDetalleDto?
                entregaDto = null;


            if (
                asignacionPedido != null &&
                asignacionPedido
                    .Asignacion_vehiculo !=
                null
            )
            {
                var idVehiculo =
                    asignacionPedido
                        .Asignacion_vehiculo
                        .id_vehiculo;


                /*
                 * Como un vehículo puede tener varias
                 * personas activas, mostramos todo su
                 * personal activo.
                 */
                var personal =
                    await _context
                        .Asignacion_Vehiculos
                        .AsNoTracking()
                        .Include(a =>
                            a.Usuario
                        )
                        .Where(a =>
                            a.id_vehiculo ==
                                idVehiculo &&
                            a.Estado ==
                                "Activo"
                        )
                        .OrderBy(a =>
                            a.Usuario.Nombre
                        )
                        .Select(a =>
                            new PersonalEntregaDto
                            {
                                IdUsuario =
                                    a.id_usuario,

                                Usuario =
                                    a.Usuario
                                        .Nombre,

                                Correo =
                                    a.Usuario
                                        .Email
                            }
                        )
                        .ToListAsync();


                entregaDto =
                    new EntregaPedidoDetalleDto
                    {
                        IdAsignacionPedido =
                            asignacionPedido
                                .Id,

                        IdVehiculo =
                            idVehiculo,

                        Vehiculo =
                            asignacionPedido
                                .Asignacion_vehiculo
                                .Vehiculo
                                ?.Marca,

                        Placa =
                            asignacionPedido
                                .Asignacion_vehiculo
                                .Vehiculo
                                ?.Placa,

                        FechaAsignacion =
                            asignacionPedido
                                .Fecha_Asignacion,

                        FechaEntrega =
                            asignacionPedido
                                .Fecha_Entrega,

                        EstadoAsignacion =
                            asignacionPedido
                                .Estado,

                        ConfirmadoPorCliente =
                            pedido.Estado ==
                            EstadosPedido
                                .Entregado,

                        Personal =
                            personal
                    };
            }


            var productos =
                pedido.Detalle_Pedidos
                    .Select(d =>
                        new ProductoDeudaDetalleDto
                        {
                            IdDetalle =
                                d.Id,

                            IdProducto =
                                d.Id_producto,

                            Producto =
                                d.Producto
                                    ?.Nombre ??
                                string.Empty,

                            Cantidad =
                                d.Cantidad,

                            PrecioUnitario =
                                d.Cantidad > 0
                                    ? d.Subtotal /
                                      d.Cantidad
                                    : 0,

                            Subtotal =
                                d.Subtotal,

                            Estado =
                                d.Estado
                        }
                    )
                    .ToList();


            var historialPagos =
                pagos
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ThenByDescending(p =>
                        p.Id
                    )
                    .Select(p =>
                        new PagoHistorialAdminDto
                        {
                            Id =
                                p.Id,

                            IdUsuario =
                                p.Id_usuario,

                            Usuario =
                                p.Usuario
                                    ?.Nombre ??
                                string.Empty,

                            CorreoUsuario =
                                p.Usuario
                                    ?.Email,

                            IdTipoPago =
                                p.Id_tipoPago,

                            TipoPago =
                                p.TipoPago
                                    ?.Descripcion ??
                                string.Empty,

                            FechaPago =
                                p.Fecha,

                            MontoPagado =
                                p.MontoPagado,

                            SaldoPendiente =
                                p.SaldoPendiente,

                            Estado =
                                p.Estado
                        }
                    )
                    .ToList();


            return Ok(
                new DetalleAdministrativoDeudaDto
                {
                    IdPedido =
                        pedido.Id,

                    IdCliente =
                        pedido.Id_cliente,

                    Cliente =
                        pedido.Cliente
                            ?.Nombre ??
                        string.Empty,

                    IdSucursal =
                        pedido.Id_sucursal,

                    Sucursal =
                        pedido.Sucursal
                            ?.Nombre ??
                        string.Empty,

                    FechaPedido =
                        pedido.Fecha,

                    EstadoPedido =
                        pedido.Estado,

                    Observacion =
                        pedido.Observacion,

                    TotalPedido =
                        pedido.Total,

                    TotalPagado =
                        totalPagado,

                    SaldoPendiente =
                        saldoPendiente,

                    EstadoDeuda =
                        saldoPendiente <= 0
                            ? EstadosDeuda
                                .Pagado
                            : EstadosDeuda
                                .Pendiente,

                    CantidadPagos =
                        pagosValidos.Count,

                    Entrega =
                        entregaDto,

                    Productos =
                        productos,

                    Pagos =
                        historialPagos
                }
            );
        }


        // =====================================================
        // CREAR PAGO
        // =====================================================

        /*
         * Administrador y Distribuidor pueden crear.
         *
         * El frontend solamente envía:
         *
         * IdPedido
         * IdUsuario
         * IdTipoPago
         * MontoPagado
         *
         * El BACKEND calcula el saldo.
         */
        [Authorize(
            Roles =
                "Administrador,Distribuidor"
        )]
        [HttpPost("Agregar")]
        public async Task<IActionResult>
            AgregarPago(
                [FromBody]
                PagoCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ModelState
                );
            }


            var pedido =
                await _context.Pedidos
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            dto.IdPedido
                    );


            if (pedido == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido indicado no existe."
                });
            }


            /*
             * El pedido debe haber sido
             * físicamente entregado.
             */
            if (
                pedido.Estado !=
                    EstadosPedido
                        .PorConfirmarEntrega &&
                pedido.Estado !=
                    EstadosPedido
                        .Entregado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Solo se pueden registrar pagos de pedidos entregados."
                });
            }


            var usuario =
                await _context.Usuarios
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            dto.IdUsuario
                    );


            if (usuario == null)
            {
                return NotFound(new
                {
                    message =
                        "El usuario que registra el pago no existe."
                });
            }


            var tipoPago =
                await _context
                    .Tipo_Pagos
                    .FirstOrDefaultAsync(
                        t =>
                            t.Id ==
                            dto.IdTipoPago
                    );


            if (tipoPago == null)
            {
                return NotFound(new
                {
                    message =
                        "El tipo de pago seleccionado no existe."
                });
            }


            if (
                tipoPago.Estado !=
                "Activo"
            )
            {
                return BadRequest(new
                {
                    message =
                        "El tipo de pago seleccionado está inactivo."
                });
            }


            /*
             * SUMA REAL DE PAGOS.
             *
             * No usamos el saldo que mande
             * el frontend.
             */
            var totalPagadoActual =
                await _context.Pagos
                    .Where(p =>
                        p.Id_pedido ==
                            pedido.Id &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )
                    .SumAsync(p =>
                        (decimal?)
                            p.MontoPagado
                    ) ?? 0m;


            var saldoActual =
                Math.Max(
                    0,
                    pedido.Total -
                    totalPagadoActual
                );


            if (saldoActual <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Este pedido ya se encuentra completamente pagado."
                });
            }


            if (
                dto.MontoPagado >
                saldoActual
            )
            {
                return BadRequest(new
                {
                    message =
                        $"El monto pagado no puede superar el saldo pendiente de Bs {saldoActual:N2}."
                });
            }


            var nuevoSaldo =
                saldoActual -
                dto.MontoPagado;


            var pago =
                new Pago
                {
                    Id_usuario =
                        dto.IdUsuario,

                    Id_pedido =
                        dto.IdPedido,

                    Id_tipoPago =
                        dto.IdTipoPago,

                    Fecha =
                        DateTime.UtcNow,

                    MontoPagado =
                        dto.MontoPagado,

                    /*
                     * Este valor sí lo guardamos
                     * como fotografía histórica
                     * del saldo después del pago.
                     */
                    SaldoPendiente =
                        Math.Max(
                            0,
                            nuevoSaldo
                        ),

                    Estado =
                        EstadosPago.Activo
                };


            await _context.Pagos
                .AddAsync(pago);


            await _context
                .SaveChangesAsync();


            // =================================================
            // GESTIÓN AUTOMÁTICA DE CIERRE DE CAJA
            // =================================================
            //
            // 1. Si el usuario no tiene una caja abierta,
            //    se abre automáticamente.
            // 2. Se registra el detalle del pago en la caja.
            // 3. Se actualizan los totales de Efectivo, QR y Recaudado.
            // =================================================
            var cajaAbierta =
                await _context.Cierre_Cajas
                    .FirstOrDefaultAsync(c =>
                        c.Id_usuario == dto.IdUsuario &&
                        c.Estado == EstadosCierreCaja.Abierta
                    );

            if (cajaAbierta == null)
            {
                cajaAbierta = new Cierre_Caja
                {
                    Id_usuario = dto.IdUsuario,
                    Fecha_Apertura = DateTime.UtcNow,
                    Estado = EstadosCierreCaja.Abierta,
                    Total_Efectivo = 0m,
                    Total_QR = 0m,
                    Total_Recaudado = 0m,
                    Observacion = "Apertura automática por registro de pago"
                };

                await _context.Cierre_Cajas
                    .AddAsync(cajaAbierta);

                await _context
                    .SaveChangesAsync();
            }

            var detalleCierre = new Cierre_Caja_Detalle
            {
                Id_cierre_caja = cajaAbierta.Id,
                Id_pago = pago.Id,
                Fecha = pago.Fecha
            };

            await _context.Cierre_Caja_Detalles
                .AddAsync(detalleCierre);

            var descTipo =
                tipoPago.Descripcion?.ToLower() ?? string.Empty;

            if (dto.IdTipoPago == 1 || descTipo.Contains("efectivo"))
            {
                cajaAbierta.Total_Efectivo += dto.MontoPagado;
            }
            else if (dto.IdTipoPago == 2 || descTipo.Contains("qr"))
            {
                cajaAbierta.Total_QR += dto.MontoPagado;
            }
            else
            {
                cajaAbierta.Total_Efectivo += dto.MontoPagado;
            }

            cajaAbierta.Total_Recaudado =
                cajaAbierta.Total_Efectivo + cajaAbierta.Total_QR;

            await _context
                .SaveChangesAsync();


            return Ok(new
            {
                message =
                    "Pago registrado correctamente.",

                pago = new
                {
                    id =
                        pago.Id,

                    idPedido =
                        pago.Id_pedido,

                    idUsuario =
                        pago.Id_usuario,

                    usuario =
                        usuario.Nombre,

                    idTipoPago =
                        pago.Id_tipoPago,

                    tipoPago =
                        tipoPago.Descripcion,

                    fecha =
                        pago.Fecha,

                    montoPagado =
                        pago.MontoPagado,

                    saldoPendiente =
                        pago.SaldoPendiente,

                    estadoDeuda =
                        pago.SaldoPendiente <= 0
                            ? EstadosDeuda
                                .Pagado
                            : EstadosDeuda
                                .Pendiente,

                    idCierreCaja =
                        cajaAbierta.Id
                }
            });
        }


        // =====================================================
        // EDITAR PAGO
        // =====================================================

        /*
         * SOLO ADMINISTRADOR.
         *
         * Puede corregir:
         * - monto
         * - tipo de pago
         *
         * NO cambia:
         * - usuario que lo registró
         * - fecha original
         *
         * Después recalculamos TODOS los saldos
         * históricos del pedido y los totales de la caja abierta.
         */
        [Authorize(
            Roles = "Administrador"
        )]
        [HttpPut("{idPago:int}")]
        public async Task<IActionResult>
            EditarPago(
                int idPago,
                [FromBody]
                PagoUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ModelState
                );
            }


            var pago =
                await _context.Pagos
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            idPago
                    );


            if (pago == null)
            {
                return NotFound(new
                {
                    message =
                        "El pago indicado no existe."
                });
            }


            if (
                pago.Estado ==
                EstadosPago.Anulado
            )
            {
                return BadRequest(new
                {
                    message =
                        "No se puede editar un pago anulado."
                });
            }


            // Validar si la caja a la que pertenece ya está cerrada
            var detalleCierre =
                await _context.Cierre_Caja_Detalles
                    .Include(d => d.Cierre_Caja)
                    .FirstOrDefaultAsync(d =>
                        d.Id_pago == idPago
                    );

            if (
                detalleCierre?.Cierre_Caja != null &&
                detalleCierre.Cierre_Caja.Estado ==
                    EstadosCierreCaja.Cerrada
            )
            {
                return BadRequest(new
                {
                    message =
                        "No se puede modificar un pago que pertenece a una caja que ya fue cerrada."
                });
            }


            var pedido =
                await _context.Pedidos
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            pago.Id_pedido
                    );


            if (pedido == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido asociado no existe."
                });
            }


            var tipoPago =
                await _context
                    .Tipo_Pagos
                    .FirstOrDefaultAsync(
                        t =>
                            t.Id ==
                            dto.IdTipoPago
                    );


            if (tipoPago == null)
            {
                return NotFound(new
                {
                    message =
                        "El tipo de pago seleccionado no existe."
                });
            }


            if (
                tipoPago.Estado !=
                "Activo"
            )
            {
                return BadRequest(new
                {
                    message =
                        "El tipo de pago seleccionado está inactivo."
                });
            }


            /*
             * Sumamos todos los demás pagos.
             */
            var totalOtrosPagos =
                await _context.Pagos
                    .Where(p =>
                        p.Id_pedido ==
                            pago.Id_pedido &&
                        p.Id !=
                            idPago &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )
                    .SumAsync(p =>
                        (decimal?)
                            p.MontoPagado
                    ) ?? 0m;


            var maximoPermitido =
                pedido.Total -
                totalOtrosPagos;


            if (maximoPermitido <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Los demás pagos ya cubren el total del pedido."
                });
            }


            if (
                dto.MontoPagado >
                maximoPermitido
            )
            {
                return BadRequest(new
                {
                    message =
                        $"El monto máximo permitido para este pago es Bs {maximoPermitido:N2}."
                });
            }


            pago.Id_tipoPago =
                dto.IdTipoPago;

            pago.MontoPagado =
                dto.MontoPagado;


            await _context
                .SaveChangesAsync();


            /*
             * Como modificamos un pago histórico,
             * hay que reconstruir todos los
             * SaldoPendiente posteriores.
             */
            await RecalcularSaldosPedidoAsync(
                pago.Id_pedido
            );


            /*
             * Recalcular totales de la caja abierta si corresponde.
             */
            if (detalleCierre != null)
            {
                await RecalcularTotalesCajaAsync(
                    detalleCierre.Id_cierre_caja
                );
            }


            return Ok(new
            {
                message =
                    "Pago actualizado correctamente."
            });
        }


        // =====================================================
        // ANULAR PAGO
        // =====================================================

        [Authorize(
            Roles = "Administrador"
        )]
        [HttpPatch("{idPago:int}/Anular")]
        public async Task<IActionResult>
            AnularPago(int idPago)
        {
            var pago =
                await _context.Pagos
                    .FirstOrDefaultAsync(p =>
                        p.Id == idPago
                    );

            if (pago == null)
            {
                return NotFound(new
                {
                    message =
                        "El pago indicado no existe."
                });
            }

            if (pago.Estado == EstadosPago.Anulado)
            {
                return BadRequest(new
                {
                    message =
                        "El pago ya se encuentra anulado."
                });
            }

            var detalleCierre =
                await _context.Cierre_Caja_Detalles
                    .Include(d => d.Cierre_Caja)
                    .FirstOrDefaultAsync(d =>
                        d.Id_pago == idPago
                    );

            if (
                detalleCierre?.Cierre_Caja != null &&
                detalleCierre.Cierre_Caja.Estado ==
                    EstadosCierreCaja.Cerrada
            )
            {
                return BadRequest(new
                {
                    message =
                        "No se puede anular un pago que pertenece a una caja que ya fue cerrada."
                });
            }

            pago.Estado = EstadosPago.Anulado;

            await _context
                .SaveChangesAsync();

            await RecalcularSaldosPedidoAsync(
                pago.Id_pedido
            );

            if (detalleCierre != null)
            {
                await RecalcularTotalesCajaAsync(
                    detalleCierre.Id_cierre_caja
                );
            }

            return Ok(new
            {
                message =
                    "Pago anulado correctamente."
            });
        }


        // =====================================================
        // PAGOS POR PEDIDO
        // =====================================================

        [HttpGet("Pedido/{idPedido:int}")]
        public async Task<ActionResult<
            PagosPorPedidoDto>>
            ListarPagosPorPedido(
                int idPedido)
        {
            var pedido =
                await _context.Pedidos
                    .AsNoTracking()
                    .Include(p =>
                        p.Cliente
                    )
                    .Include(p =>
                        p.Sucursal
                    )
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            idPedido
                    );


            if (pedido == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido indicado no existe."
                });
            }


            var pagos =
                await ConstruirConsultaPagos()
                    .Where(p =>
                        p.Id_pedido ==
                        idPedido
                    )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ThenByDescending(p =>
                        p.Id
                    )
                    .ToListAsync();


            var pagosDto =
                pagos
                    .Select(
                        MapearPagoRespuesta
                    )
                    .ToList();


            var pagosValidos =
                pagos.Where(p =>
                    p.Estado !=
                    EstadosPago.Anulado
                );


            var totalPagado =
                pagosValidos.Sum(
                    p => p.MontoPagado
                );


            var saldoPendienteActual =
                Math.Max(
                    0,
                    pedido.Total -
                    totalPagado
                );


            return Ok(
                new PagosPorPedidoDto
                {
                    IdPedido =
                        pedido.Id,

                    IdCliente =
                        pedido.Id_cliente,

                    Cliente =
                        pedido.Cliente
                            ?.Nombre ??
                        string.Empty,

                    IdSucursal =
                        pedido.Id_sucursal,

                    Sucursal =
                        pedido.Sucursal
                            ?.Nombre ??
                        string.Empty,

                    FechaPedido =
                        pedido.Fecha,

                    EstadoPedido =
                        pedido.Estado,

                    TotalPedido =
                        pedido.Total,

                    TotalPagado =
                        totalPagado,

                    SaldoPendienteActual =
                        saldoPendienteActual,

                    CantidadPagos =
                        pagosDto.Count,

                    Pagos =
                        pagosDto
                }
            );
        }


        // =====================================================
        // PAGOS REGISTRADOS POR USUARIO
        // =====================================================

        [HttpGet(
            "Usuario/{idUsuario:int}"
        )]
        public async Task<ActionResult<
            IEnumerable<PagoPedidoUsuarioDto>>>
            ListarPagosRegistradosPorUsuario(
                int idUsuario)
        {
            var usuarioExiste =
                await _context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.Id ==
                        idUsuario
                    );


            if (!usuarioExiste)
            {
                return NotFound(new
                {
                    message =
                        "El usuario indicado no existe."
                });
            }


            var pagos =
                await ConstruirConsultaPagos()
                    .Where(p =>
                        p.Id_usuario ==
                        idUsuario
                    )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ThenByDescending(p =>
                        p.Id
                    )
                    .ToListAsync();


            return Ok(
                pagos
                    .Select(
                        MapearPagoPedidoUsuario
                    )
                    .ToList()
            );
        }


        // =====================================================
        // PAGOS DE UN CLIENTE
        // =====================================================

        [HttpGet(
            "Cliente/{idCliente:int}"
        )]
        public async Task<ActionResult<
            IEnumerable<PagoPedidoUsuarioDto>>>
            ListarPagosDePedidosPorCliente(
                int idCliente)
        {
            var clienteExiste =
                await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Id ==
                        idCliente
                    );


            if (!clienteExiste)
            {
                return NotFound(new
                {
                    message =
                        "El cliente indicado no existe."
                });
            }


            var pagos =
                await ConstruirConsultaPagos()
                    .Where(p =>
                        p.Pedido
                            .Id_cliente ==
                        idCliente
                    )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ThenByDescending(p =>
                        p.Id
                    )
                    .ToListAsync();


            return Ok(
                pagos
                    .Select(
                        MapearPagoPedidoUsuario
                    )
                    .ToList()
            );
        }


        // =====================================================
        // PAGO POR ID
        // =====================================================

        [HttpGet("{idPago:int}")]
        public async Task<ActionResult<
            PagoDetalleDto>>
            ObtenerPagoPorId(
                int idPago)
        {
            var pago =
                await ConstruirConsultaPagos()
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            idPago
                    );


            if (pago == null)
            {
                return NotFound(new
                {
                    message =
                        "El pago indicado no existe."
                });
            }


            return Ok(
                MapearPagoDetalle(
                    pago
                )
            );
        }


        // =====================================================
        // DETALLE PAGOS PEDIDO
        // =====================================================

        [HttpGet(
            "Pedido/{idPedido:int}/Detalle"
        )]
        public async Task<ActionResult<
            IEnumerable<PagoDetalleDto>>>
            ObtenerDetallePagosDelPedido(
                int idPedido)
        {
            var pedidoExiste =
                await _context.Pedidos
                    .AsNoTracking()
                    .AnyAsync(p =>
                        p.Id ==
                        idPedido
                    );


            if (!pedidoExiste)
            {
                return NotFound(new
                {
                    message =
                        "El pedido indicado no existe."
                });
            }


            var pagos =
                await ConstruirConsultaPagos()
                    .Where(p =>
                        p.Id_pedido ==
                        idPedido
                    )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ThenByDescending(p =>
                        p.Id
                    )
                    .ToListAsync();


            return Ok(
                pagos
                    .Select(
                        MapearPagoDetalle
                    )
                    .ToList()
            );
        }



        // =====================================================
        // DISTRIBUIDOR - PEDIDOS DISPONIBLES PARA COBRAR
        // =====================================================
        //
        // GET:
        // api/Pago/Distribuidor/PedidosCobrables
        //
        // Devuelve UNA FILA POR PEDIDO.
        //
        // Reglas:
        // - Debe pertenecer históricamente al distribuidor.
        // - Pedido físicamente entregado:
        //   PorConfirmarEntrega o Entregado.
        // - Saldo pendiente > 0.
        // - Incluye sucursal, ubicación y fecha real de entrega.
        // =====================================================

        [Authorize(Roles = "Distribuidor")]
        [HttpGet("Distribuidor/PedidosCobrables")]
        public async Task<ActionResult<
            IEnumerable<PedidoCobrableDistribuidorDto>>>
            ListarPedidosCobrablesDistribuidor()
        {
            var idUsuario =
                ObtenerIdUsuarioActual();

            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "No se pudo identificar al distribuidor autenticado."
                });
            }


            // =================================================
            // ASIGNACIONES HISTÓRICAS DEL DISTRIBUIDOR
            // =================================================

            var asignaciones =
                await _context
                    .Asignacion_Pedidos
                    .AsNoTracking()
                    .AsSplitQuery()

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

                    .Where(a =>
                        a.Asignacion_Pedido_Usuarios
                            .Any(h =>
                                h.AsignacionVehiculo
                                    .id_usuario ==
                                idUsuario.Value
                            )
                        &&
                        (
                            a.Pedido.Estado ==
                                EstadosPedido
                                    .PorConfirmarEntrega
                            ||
                            a.Pedido.Estado ==
                                EstadosPedido
                                    .Entregado
                        )
                    )

                    .OrderByDescending(a =>
                        a.Fecha_Entrega ??
                        a.Fecha_Asignacion
                    )

                    .ThenByDescending(a =>
                        a.Id
                    )

                    .ToListAsync();


            /*
             * Si por alguna razón existieran varias
             * Asignacion_Pedido para el mismo pedido,
             * nos quedamos con la más reciente.
             */
            var asignacionesPorPedido =
                asignaciones
                    .GroupBy(a =>
                        a.id_pedido
                    )
                    .Select(g =>
                        g.First()
                    )
                    .ToList();


            var idsPedidos =
                asignacionesPorPedido
                    .Select(a =>
                        a.id_pedido
                    )
                    .Distinct()
                    .ToList();


            var pagosPorPedido =
                await _context.Pagos
                    .AsNoTracking()

                    .Where(p =>
                        idsPedidos.Contains(
                            p.Id_pedido
                        )
                        &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )

                    .GroupBy(p =>
                        p.Id_pedido
                    )

                    .Select(g => new
                    {
                        IdPedido =
                            g.Key,

                        TotalPagado =
                            g.Sum(x =>
                                x.MontoPagado
                            ),

                        CantidadPagos =
                            g.Count()
                    })

                    .ToDictionaryAsync(
                        x =>
                            x.IdPedido
                    );


            var resultado =
                new List<
                    PedidoCobrableDistribuidorDto
                >();


            foreach (
                var asignacion
                in asignacionesPorPedido
            )
            {
                var pedido =
                    asignacion.Pedido;


                pagosPorPedido.TryGetValue(
                    pedido.Id,
                    out var resumenPago
                );


                var totalPagado =
                    resumenPago
                        ?.TotalPagado
                    ?? 0m;


                var saldoPendiente =
                    Math.Max(
                        0,
                        pedido.Total -
                        totalPagado
                    );


                // YA ESTÁ COMPLETAMENTE PAGADO:
                // NO DEBE SALIR EN EL MODAL.
                if (saldoPendiente <= 0)
                {
                    continue;
                }


                resultado.Add(
                    new PedidoCobrableDistribuidorDto
                    {
                        IdPedido =
                            pedido.Id,

                        IdCliente =
                            pedido.Id_cliente,

                        Cliente =
                            pedido.Cliente
                                ?.Nombre
                            ??
                            string.Empty,

                        IdSucursal =
                            pedido.Id_sucursal,

                        Sucursal =
                            pedido.Sucursal
                                ?.Nombre
                            ??
                            string.Empty,

                        Ubicacion =
                            pedido.Sucursal
                                ?.Ubicacion
                            ??
                            string.Empty,

                        FechaPedido =
                            pedido.Fecha,

                        FechaEntrega =
                            asignacion
                                .Fecha_Entrega,

                        EstadoPedido =
                            pedido.Estado,

                        TotalPedido =
                            pedido.Total,

                        TotalPagado =
                            totalPagado,

                        SaldoPendiente =
                            saldoPendiente,

                        CantidadPagos =
                            resumenPago
                                ?.CantidadPagos
                            ??
                            0
                    }
                );
            }


            return Ok(
                resultado
                    .OrderByDescending(x =>
                        x.FechaEntrega ??
                        x.FechaPedido
                    )
                    .ThenByDescending(x =>
                        x.IdPedido
                    )
                    .ToList()
            );
        }


        // =====================================================
        // DISTRIBUIDOR - MIS COBROS AGRUPADOS POR PEDIDO
        // =====================================================
        //
        // GET:
        // api/Pago/Distribuidor/MisCobrosPorPedido
        //
        // IMPORTANTE:
        //
        // Antes el frontend recibía UNA FILA POR PAGO,
        // por eso Pedido #5 aparecía varias veces.
        //
        // Ahora devolvemos UNA FILA POR PEDIDO
        // y dentro viene la lista HistorialCobros.
        // =====================================================

        [Authorize(Roles = "Distribuidor")]
        [HttpGet("Distribuidor/MisCobrosPorPedido")]
        public async Task<ActionResult<
            IEnumerable<CobrosPedidoDistribuidorDto>>>
            ListarMisCobrosAgrupadosPorPedido()
        {
            var idUsuario =
                ObtenerIdUsuarioActual();

            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "No se pudo identificar al distribuidor autenticado."
                });
            }


            // =================================================
            // PAGOS REGISTRADOS POR ESTE DISTRIBUIDOR
            // =================================================

            var misPagos =
                await ConstruirConsultaPagos()

                    .Where(p =>
                        p.Id_usuario ==
                            idUsuario.Value
                        &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )

                    .OrderByDescending(p =>
                        p.Fecha
                    )

                    .ThenByDescending(p =>
                        p.Id
                    )

                    .ToListAsync();


            if (misPagos.Count == 0)
            {
                return Ok(
                    new List<
                        CobrosPedidoDistribuidorDto
                    >()
                );
            }


            var idsPedidos =
                misPagos
                    .Select(p =>
                        p.Id_pedido
                    )
                    .Distinct()
                    .ToList();


            // =================================================
            // TOTAL REAL PAGADO DEL PEDIDO
            // =================================================
            //
            // Se consideran TODOS los pagos válidos del pedido,
            // aunque otro usuario también haya registrado uno.
            // Así el saldo mostrado siempre es el saldo REAL.
            // =================================================

            var totalesReales =
                await _context.Pagos
                    .AsNoTracking()

                    .Where(p =>
                        idsPedidos.Contains(
                            p.Id_pedido
                        )
                        &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )

                    .GroupBy(p =>
                        p.Id_pedido
                    )

                    .Select(g => new
                    {
                        IdPedido =
                            g.Key,

                        TotalPagado =
                            g.Sum(x =>
                                x.MontoPagado
                            )
                    })

                    .ToDictionaryAsync(
                        x =>
                            x.IdPedido
                    );


            // =================================================
            // FECHA DE ENTREGA DEL PEDIDO
            // =================================================

            var fechasEntrega =
                await _context
                    .Asignacion_Pedidos
                    .AsNoTracking()

                    .Where(a =>
                        idsPedidos.Contains(
                            a.id_pedido
                        )
                    )

                    .GroupBy(a =>
                        a.id_pedido
                    )

                    .Select(g => new
                    {
                        IdPedido =
                            g.Key,

                        FechaEntrega =
                            g.Max(x =>
                                x.Fecha_Entrega
                            )
                    })

                    .ToDictionaryAsync(
                        x =>
                            x.IdPedido
                    );


            var resultado =
                new List<
                    CobrosPedidoDistribuidorDto
                >();


            foreach (
                var grupo
                in misPagos.GroupBy(
                    p =>
                        p.Id_pedido
                )
            )
            {
                var pagoReferencia =
                    grupo
                        .OrderByDescending(
                            p =>
                                p.Fecha
                        )
                        .ThenByDescending(
                            p =>
                                p.Id
                        )
                        .First();


                var pedido =
                    pagoReferencia.Pedido;


                totalesReales.TryGetValue(
                    grupo.Key,
                    out var totalReal
                );


                var totalPagadoPedido =
                    totalReal
                        ?.TotalPagado
                    ?? 0m;


                var saldoPendiente =
                    Math.Max(
                        0,
                        pedido.Total -
                        totalPagadoPedido
                    );


                fechasEntrega.TryGetValue(
                    grupo.Key,
                    out var entrega
                );


                var historialCobros =
                    grupo
                        .OrderByDescending(
                            p =>
                                p.Fecha
                        )
                        .ThenByDescending(
                            p =>
                                p.Id
                        )
                        .Select(p =>
                            new CobroDistribuidorDetalleDto
                            {
                                IdPago =
                                    p.Id,

                                IdUsuario =
                                    p.Id_usuario,

                                Usuario =
                                    p.Usuario
                                        ?.Nombre
                                    ??
                                    string.Empty,

                                IdTipoPago =
                                    p.Id_tipoPago,

                                TipoPago =
                                    p.TipoPago
                                        ?.Descripcion
                                    ??
                                    string.Empty,

                                FechaPago =
                                    p.Fecha,

                                MontoPagado =
                                    p.MontoPagado,

                                SaldoPendienteDespuesPago =
                                    p.SaldoPendiente,

                                EstadoPago =
                                    p.Estado
                            }
                        )
                        .ToList();


                resultado.Add(
                    new CobrosPedidoDistribuidorDto
                    {
                        IdPedido =
                            pedido.Id,

                        IdCliente =
                            pedido.Id_cliente,

                        Cliente =
                            pedido.Cliente
                                ?.Nombre
                            ??
                            string.Empty,

                        IdSucursal =
                            pedido.Id_sucursal,

                        Sucursal =
                            pedido.Sucursal
                                ?.Nombre
                            ??
                            string.Empty,

                        Ubicacion =
                            pedido.Sucursal
                                ?.Ubicacion
                            ??
                            string.Empty,

                        FechaPedido =
                            pedido.Fecha,

                        FechaEntrega =
                            entrega
                                ?.FechaEntrega,

                        EstadoPedido =
                            pedido.Estado,

                        TotalPedido =
                            pedido.Total,

                        TotalPagadoPedido =
                            totalPagadoPedido,

                        SaldoPendiente =
                            saldoPendiente,

                        EstadoDeuda =
                            saldoPendiente <= 0
                                ?
                                EstadosDeuda
                                    .Pagado
                                :
                                EstadosDeuda
                                    .Pendiente,

                        CantidadCobros =
                            historialCobros.Count,

                        TotalCobradoPorMi =
                            historialCobros.Sum(
                                x =>
                                    x.MontoPagado
                            ),

                        UltimoCobro =
                            historialCobros
                                .FirstOrDefault()
                                ?.FechaPago,

                        HistorialCobros =
                            historialCobros
                    }
                );
            }


            return Ok(
                resultado
                    .OrderByDescending(x =>
                        x.UltimoCobro
                    )
                    .ThenByDescending(x =>
                        x.IdPedido
                    )
                    .ToList()
            );
        }


        // =====================================================
        // RECALCULAR SALDOS HISTÓRICOS
        // =====================================================

        private async Task
            RecalcularSaldosPedidoAsync(
                int idPedido)
        {
            var pedido =
                await _context.Pedidos
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            idPedido
                    );


            if (pedido == null)
            {
                throw new InvalidOperationException(
                    "El pedido no existe."
                );
            }


            var pagos =
                await _context.Pagos
                    .Where(p =>
                        p.Id_pedido ==
                            idPedido &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )
                    .OrderBy(p =>
                        p.Fecha
                    )
                    .ThenBy(p =>
                        p.Id
                    )
                    .ToListAsync();


            decimal saldo =
                pedido.Total;


            foreach (var pago in pagos)
            {
                saldo -=
                    pago.MontoPagado;


                pago.SaldoPendiente =
                    Math.Max(
                        0,
                        saldo
                    );
            }


            await _context
                .SaveChangesAsync();
        }


        // =====================================================
        // RECALCULAR TOTALES DE CAJA
        // =====================================================

        private async Task
            RecalcularTotalesCajaAsync(
                int idCierreCaja)
        {
            var caja =
                await _context.Cierre_Cajas
                    .FirstOrDefaultAsync(c =>
                        c.Id == idCierreCaja
                    );

            if (caja == null) return;

            var pagosCaja =
                await _context.Cierre_Caja_Detalles
                    .Where(d =>
                        d.Id_cierre_caja == idCierreCaja
                    )
                    .Include(d => d.Pago)
                        .ThenInclude(p => p.TipoPago)
                    .Select(d => d.Pago)
                    .Where(p =>
                        p.Estado != EstadosPago.Anulado
                    )
                    .ToListAsync();

            decimal totalEfectivo = 0m;
            decimal totalQR = 0m;

            foreach (var p in pagosCaja)
            {
                var desc =
                    p.TipoPago?.Descripcion?.ToLower() ?? string.Empty;

                if (p.Id_tipoPago == 1 || desc.Contains("efectivo"))
                {
                    totalEfectivo += p.MontoPagado;
                }
                else if (p.Id_tipoPago == 2 || desc.Contains("qr"))
                {
                    totalQR += p.MontoPagado;
                }
                else
                {
                    totalEfectivo += p.MontoPagado;
                }
            }

            caja.Total_Efectivo = totalEfectivo;
            caja.Total_QR = totalQR;
            caja.Total_Recaudado = totalEfectivo + totalQR;

            await _context
                .SaveChangesAsync();
        }


        // =====================================================
        // CONSULTA BASE
        // =====================================================

        private IQueryable<Pago>
            ConstruirConsultaPagos()
        {
            return _context.Pagos
                .AsNoTracking()
                .AsSplitQuery()

                .Include(p =>
                    p.Usuario
                )

                .Include(p =>
                    p.TipoPago
                )

                .Include(p =>
                    p.Pedido
                )
                    .ThenInclude(
                        pedido =>
                            pedido.Cliente
                    )

                .Include(p =>
                    p.Pedido
                )
                    .ThenInclude(
                        pedido =>
                            pedido.Sucursal
                    );
        }


        // =====================================================
        // MAPEOS EXISTENTES
        // =====================================================

        private static PagoRespuestaDto
            MapearPagoRespuesta(
                Pago pago)
        {
            return new PagoRespuestaDto
            {
                Id =
                    pago.Id,

                IdPedido =
                    pago.Id_pedido,

                IdUsuario =
                    pago.Id_usuario,

                Usuario =
                    pago.Usuario
                        ?.Nombre ??
                    string.Empty,

                IdTipoPago =
                    pago.Id_tipoPago,

                TipoPago =
                    pago.TipoPago
                        ?.Descripcion ??
                    string.Empty,

                FechaPago =
                    pago.Fecha,

                MontoPagado =
                    pago.MontoPagado,

                SaldoPendiente =
                    pago.SaldoPendiente,

                Estado =
                    pago.Estado
            };
        }


        private static PagoDetalleDto
            MapearPagoDetalle(
                Pago pago)
        {
            return new PagoDetalleDto
            {
                Id =
                    pago.Id,

                IdPedido =
                    pago.Id_pedido,

                IdUsuario =
                    pago.Id_usuario,

                Usuario =
                    pago.Usuario
                        ?.Nombre ??
                    string.Empty,

                CorreoUsuario =
                    pago.Usuario
                        ?.Email,

                IdTipoPago =
                    pago.Id_tipoPago,

                TipoPago =
                    pago.TipoPago
                        ?.Descripcion ??
                    string.Empty,

                FechaPago =
                    pago.Fecha,

                MontoPagado =
                    pago.MontoPagado,

                SaldoPendiente =
                    pago.SaldoPendiente,

                Estado =
                    pago.Estado
            };
        }


        private static
            PagoPedidoUsuarioDto
            MapearPagoPedidoUsuario(
                Pago pago)
        {
            return new PagoPedidoUsuarioDto
            {
                IdPago =
                    pago.Id,

                IdPedido =
                    pago.Id_pedido,

                IdCliente =
                    pago.Pedido
                        ?.Id_cliente ??
                    0,

                Cliente =
                    pago.Pedido
                        ?.Cliente
                        ?.Nombre ??
                    string.Empty,

                Sucursal =
                    pago.Pedido
                        ?.Sucursal
                        ?.Nombre ??
                    string.Empty,

                TotalPedido =
                    pago.Pedido
                        ?.Total ??
                    0,

                EstadoPedido =
                    pago.Pedido
                        ?.Estado ??
                    string.Empty,

                IdUsuario =
                    pago.Id_usuario,

                Usuario =
                    pago.Usuario
                        ?.Nombre ??
                    string.Empty,

                FechaPago =
                    pago.Fecha,

                TipoPago =
                    pago.TipoPago
                        ?.Descripcion ??
                    string.Empty,

                MontoPagado =
                    pago.MontoPagado,

                SaldoPendiente =
                    pago.SaldoPendiente,

                EstadoPago =
                    pago.Estado
            };
        }
    }
}