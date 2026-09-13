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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PedidoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PedidoController(AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // LISTAR TODOS
        // =====================================================
        //
        // GET:
        // api/Pedido/Listar
        //
        // Principalmente para administración.
        // =====================================================

        [HttpGet("Listar")]
        public async Task<IActionResult> ListarPedidos()
        {
            var pedidos =
                await ConstruirConsultaPedidos()
                    .OrderByDescending(p => p.Fecha)
                    .ToListAsync();

            var saldos =
                await ObtenerSaldosPendientesAsync(
                    pedidos
                );

            var respuesta =
                pedidos
                    .Select(p =>
                        MapearPedido(
                            p,
                            saldos.TryGetValue(
                                p.Id,
                                out var saldo
                            )
                                ? saldo
                                : null
                        )
                    )
                    .ToList();

            return Ok(respuesta);
        }


        // =====================================================
        // LISTAR POR ESTADO
        // =====================================================
        //
        // GET:
        // api/Pedido/ListarPorEstado/Pendiente
        //
        // =====================================================

        [HttpGet("ListarPorEstado/{estado}")]
        public async Task<ActionResult<
            IEnumerable<PedidoRespuestaDto>>>
            ListarPorEstado(
                string estado)
        {
            var estadoNormalizado =
                EstadosPedido.Normalizar(
                    estado
                );

            if (estadoNormalizado is null)
            {
                return BadRequest(new
                {
                    message =
                        "El estado enviado no es válido.",

                    estadosPermitidos =
                        EstadosPedido.Todos
                });
            }

            var pedidos =
                await ConstruirConsultaPedidos()
                    .Where(p =>
                        p.Estado ==
                        estadoNormalizado
                    )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ToListAsync();

            var saldos =
                await ObtenerSaldosPendientesAsync(
                    pedidos
                );

            var respuesta =
                pedidos
                    .Select(p =>
                        MapearPedido(
                            p,
                            saldos.TryGetValue(
                                p.Id,
                                out var saldo
                            )
                                ? saldo
                                : null
                        )
                    )
                    .ToList();

            return Ok(respuesta);
        }


        // =====================================================
        // LISTAR PEDIDOS DE UN CLIENTE
        // =====================================================
        //
        // GET:
        // api/Pedido/ListarPorCliente/1
        //
        // Si quien llama es Cliente:
        // solamente puede consultar sus propios pedidos.
        //
        // =====================================================

        [HttpGet("ListarPorCliente/{idCliente:int}")]
        public async Task<ActionResult<
            IEnumerable<PedidoRespuestaDto>>>
            ListarPorCliente(
                int idCliente)
        {
            var errorAcceso =
                ValidarAccesoCliente(
                    idCliente
                );

            if (errorAcceso != null)
            {
                return errorAcceso;
            }

            var clienteExiste =
                await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Id == idCliente
                    );

            if (!clienteExiste)
            {
                return NotFound(new
                {
                    message =
                        "El cliente indicado no existe."
                });
            }

            var pedidos =
                await ConstruirConsultaPedidos()
                    .Where(p =>
                        p.Id_cliente ==
                        idCliente
                    )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ToListAsync();

            var saldos =
                await ObtenerSaldosPendientesAsync(
                    pedidos
                );

            var respuesta =
                pedidos
                    .Select(p =>
                        MapearPedido(
                            p,
                            saldos.TryGetValue(
                                p.Id,
                                out var saldo
                            )
                                ? saldo
                                : null
                        )
                    )
                    .ToList();

            return Ok(respuesta);
        }


        // =====================================================
        // PEDIDOS PENDIENTES DE CONFIRMACIÓN DEL CLIENTE
        // =====================================================
        //
        // Este endpoint nos será MUY útil para el modal global
        // del frontend.
        //
        // No recibe idCliente.
        // Lo obtiene directamente del JWT.
        //
        // GET:
        // api/Pedido/MisPendientesConfirmacion
        //
        // =====================================================

        [HttpGet("MisPendientesConfirmacion")]
        public async Task<ActionResult<
            IEnumerable<PedidoRespuestaDto>>>
            ListarMisPendientesConfirmacion()
        {
            var resultadoCliente =
                ObtenerClienteIdAutenticado();

            if (!resultadoCliente.EsValido)
            {
                return resultadoCliente.Error!;
            }

            var pedidos =
                await ConstruirConsultaPedidos()
                    .Where(p =>
                        p.Id_cliente ==
                            resultadoCliente.Id
                        &&
                        p.Estado ==
                            EstadosPedido
                                .PorConfirmarEntrega
                    )
                    .OrderBy(p =>
                        p.Fecha
                    )
                    .ToListAsync();

            var respuesta =
                pedidos
                    .Select(p =>
                        MapearPedido(
                            p,
                            null
                        )
                    )
                    .ToList();

            return Ok(respuesta);
        }


        // =====================================================
        // OBTENER PEDIDO POR ID
        // =====================================================
        //
        // GET:
        // api/Pedido/1
        //
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<ActionResult<
            PedidoRespuestaDto>>
            ObtenerPorId(
                int id)
        {
            var pedido =
                await ConstruirConsultaPedidos()
                    .FirstOrDefaultAsync(p =>
                        p.Id == id
                    );

            if (pedido is null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido no existe."
                });
            }

            /*
             * Si la cuenta autenticada es Cliente,
             * verificamos propiedad.
             */
            var errorAcceso =
                ValidarAccesoCliente(
                    pedido.Id_cliente
                );

            if (errorAcceso != null)
            {
                return errorAcceso;
            }

            decimal? saldoPendiente =
                null;

            if (
                EsPedidoConDeudaVisible(
                    pedido.Estado
                )
            )
            {
                saldoPendiente =
                    await CalcularSaldoPendienteAsync(
                        pedido
                    );
            }

            return Ok(
                MapearPedido(
                    pedido,
                    saldoPendiente
                )
            );
        }


        // =====================================================
        // CREAR PEDIDO
        // =====================================================
        //
        // POST:
        // api/Pedido/Crear
        //
        // Estado automático:
        // Pendiente
        //
        // =====================================================

        [HttpPost("Crear")]
        public async Task<ActionResult>
            Crear(
                [FromBody]
                CrearPedidoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(
                    ModelState
                );
            }

            /*
             * Si el que crea es Cliente,
             * no permitimos que mande el ID
             * de otro cliente.
             */
            var errorAcceso =
                ValidarAccesoCliente(
                    dto.IdCliente
                );

            if (errorAcceso != null)
            {
                return errorAcceso;
            }

            var errorValidacion =
                await ValidarPedidoAsync(
                    dto.IdCliente,
                    dto.IdSucursal,
                    dto.Detalles
                );

            if (errorValidacion is not null)
            {
                return BadRequest(new
                {
                    message =
                        errorValidacion
                });
            }

            var idsProductos =
                dto.Detalles
                    .Select(d =>
                        d.IdProducto
                    )
                    .Distinct()
                    .ToList();

            var productos =
                await _context.Productos
                    .Where(p =>
                        idsProductos
                            .Contains(p.Id)
                    )
                    .ToDictionaryAsync(
                        p => p.Id
                    );

            await using var transaccion =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var pedido =
                    new Pedido
                    {
                        Id_cliente =
                            dto.IdCliente,

                        Id_sucursal =
                            dto.IdSucursal,

                        Fecha =
                            DateTime.UtcNow,

                        Observacion =
                            dto.Observacion
                                ?.Trim(),

                        Estado =
                            EstadosPedido
                                .Pendiente,

                        Total = 0,

                        Detalle_Pedidos =
                            new List<
                                Detalle_Pedido
                            >()
                    };

                foreach (
                    var detalleDto
                    in dto.Detalles
                )
                {
                    var producto =
                        productos[
                            detalleDto
                                .IdProducto
                        ];

                    var subtotal =
                        producto.Precio *
                        detalleDto.Cantidad;

                    pedido.Detalle_Pedidos
                        .Add(
                            new Detalle_Pedido
                            {
                                Id_producto =
                                    producto.Id,

                                Cantidad =
                                    detalleDto
                                        .Cantidad,

                                Subtotal =
                                    subtotal,

                                Estado =
                                    EstadosDetallePedido
                                        .PendienteEntrega
                            }
                        );

                    pedido.Total +=
                        subtotal;
                }

                _context.Pedidos.Add(
                    pedido
                );

                await _context
                    .SaveChangesAsync();

                await transaccion
                    .CommitAsync();

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new
                    {
                        id = pedido.Id
                    },
                    new
                    {
                        message =
                            "Pedido registrado correctamente.",

                        idPedido =
                            pedido.Id,

                        estado =
                            pedido.Estado,

                        total =
                            pedido.Total,

                        cantidadDetalles =
                            pedido
                                .Detalle_Pedidos
                                .Count
                    }
                );
            }
            catch
            {
                await transaccion
                    .RollbackAsync();

                throw;
            }
        }


        // =====================================================
        // ACTUALIZAR PEDIDO
        // =====================================================
        //
        // Solamente puede modificarse:
        //
        // Pedido = Pendiente
        //
        // PUT:
        // api/Pedido/1
        //
        // =====================================================

        [HttpPut("{id:int}")]
        public async Task<ActionResult>
            Actualizar(
                int id,
                [FromBody]
                ActualizarPedidoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(
                    ModelState
                );
            }

            var pedido =
                await _context.Pedidos
                    .Include(p =>
                        p.Detalle_Pedidos
                    )
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (pedido is null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido no existe."
                });
            }

            var errorAcceso =
                ValidarAccesoCliente(
                    pedido.Id_cliente
                );

            if (errorAcceso != null)
            {
                return errorAcceso;
            }

            /*
             * Si cliente está modificando,
             * tampoco puede cambiar el pedido
             * y poner otro IdCliente.
             */
            if (
                EsCuentaCliente() &&
                dto.IdCliente !=
                    pedido.Id_cliente
            )
            {
                return StatusCode(
                    StatusCodes
                        .Status403Forbidden,
                    new
                    {
                        message =
                            "No puedes cambiar el propietario del pedido."
                    }
                );
            }

            if (
                !string.Equals(
                    pedido.Estado,
                    EstadosPedido.Pendiente,
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Solo se pueden editar pedidos con estado Pendiente."
                });
            }

            var errorValidacion =
                await ValidarPedidoAsync(
                    dto.IdCliente,
                    dto.IdSucursal,
                    dto.Detalles
                );

            if (errorValidacion is not null)
            {
                return BadRequest(new
                {
                    message =
                        errorValidacion
                });
            }

            var idsProductos =
                dto.Detalles
                    .Select(d =>
                        d.IdProducto
                    )
                    .Distinct()
                    .ToList();

            var productos =
                await _context.Productos
                    .Where(p =>
                        idsProductos
                            .Contains(p.Id)
                    )
                    .ToDictionaryAsync(
                        p => p.Id
                    );

            await using var transaccion =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                pedido.Id_cliente =
                    dto.IdCliente;

                pedido.Id_sucursal =
                    dto.IdSucursal;

                pedido.Observacion =
                    dto.Observacion
                        ?.Trim();

                if (!string.IsNullOrWhiteSpace(dto.MotivoEdicion))
                {
                    pedido.Motivo_Edicion =
                        dto.MotivoEdicion.Trim();
                }

                pedido.Total = 0;

                _context
                    .Detalle_Pedidos
                    .RemoveRange(
                        pedido
                            .Detalle_Pedidos
                    );

                pedido.Detalle_Pedidos
                    .Clear();

                foreach (
                    var detalleDto
                    in dto.Detalles
                )
                {
                    var producto =
                        productos[
                            detalleDto
                                .IdProducto
                        ];

                    var subtotal =
                        producto.Precio *
                        detalleDto.Cantidad;

                    pedido.Detalle_Pedidos
                        .Add(
                            new Detalle_Pedido
                            {
                                Id_producto =
                                    producto.Id,

                                Cantidad =
                                    detalleDto
                                        .Cantidad,

                                Subtotal =
                                    subtotal,

                                Estado =
                                    EstadosDetallePedido
                                        .PendienteEntrega
                            }
                        );

                    pedido.Total +=
                        subtotal;
                }

                await _context
                    .SaveChangesAsync();

                await transaccion
                    .CommitAsync();

                return Ok(new
                {
                    message =
                        "Pedido actualizado correctamente.",

                    idPedido =
                        pedido.Id,

                    total =
                        pedido.Total,

                    motivoEdicion =
                        pedido.Motivo_Edicion,

                    cantidadDetalles =
                        pedido
                            .Detalle_Pedidos
                            .Count
                });
            }
            catch
            {
                await transaccion
                    .RollbackAsync();

                throw;
            }
        }


        // =====================================================
        // CAMBIAR ESTADO GENERAL
        // =====================================================
        //
        // Este endpoint queda para ADMINISTRACIÓN.
        //
        // IMPORTANTE:
        //
        // Ya NO crea registros Pago.
        //
        // PorConfirmarEntrega y Entregado se manejan
        // mediante el flujo especializado:
        //
        // DistribuidorPedidoController:
        //      EnCamino -> PorConfirmarEntrega
        //
        // PedidoController/ConfirmarEntrega:
        //      PorConfirmarEntrega -> Entregado
        //
        // =====================================================

        [Authorize(Roles = "Administrador")]
        [HttpPatch("{id:int}/estado")]
        public async Task<ActionResult>
            CambiarEstado(
                int id,
                [FromBody]
                CambiarEstadoPedidoDto dto)
        {
            var estadoNormalizado =
                EstadosPedido.Normalizar(
                    dto.Estado
                );

            if (estadoNormalizado is null)
            {
                return BadRequest(new
                {
                    message =
                        "El estado enviado no es válido.",

                    estadosPermitidos =
                        EstadosPedido.Todos
                });
            }

            /*
             * Estas dos transiciones NO deben
             * realizarse manualmente desde
             * administración.
             */
            if (
                estadoNormalizado ==
                    EstadosPedido
                        .PorConfirmarEntrega ||
                estadoNormalizado ==
                    EstadosPedido
                        .Entregado
            )
            {
                return BadRequest(new
                {
                    message =
                        "Los estados PorConfirmarEntrega y Entregado se gestionan mediante el flujo de entrega del distribuidor y confirmación del cliente."
                });
            }

            var pedido =
                await _context.Pedidos
                    .Include(p =>
                        p.Detalle_Pedidos
                    )
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (pedido is null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido no existe."
                });
            }

            var errorTransicion =
                ValidarTransicionAdministrativa(
                    pedido.Estado,
                    estadoNormalizado
                );

            if (errorTransicion is not null)
            {
                return BadRequest(new
                {
                    message =
                        errorTransicion
                });
            }

            pedido.Estado =
                estadoNormalizado;

            if (
                estadoNormalizado ==
                EstadosPedido.Devuelto
            )
            {
                foreach (
                    var detalle
                    in pedido.Detalle_Pedidos
                )
                {
                    detalle.Estado =
                        EstadosDetallePedido
                            .Devuelto;
                }
            }

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Estado del pedido actualizado correctamente.",

                idPedido =
                    pedido.Id,

                estado =
                    pedido.Estado,

                total =
                    pedido.Total
            });
        }


        // =====================================================
        // CANCELAR
        // =====================================================
        //
        // Solamente Pedido = Pendiente.
        //
        // PATCH:
        // api/Pedido/1/cancelar
        //
        // =====================================================

        [HttpPatch("{id:int}/cancelar")]
        public async Task<ActionResult>
            Cancelar(
                int id)
        {
            var pedido =
                await _context.Pedidos
                    .Include(p =>
                        p.Detalle_Pedidos
                    )
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (pedido is null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido no existe."
                });
            }

            var errorAcceso =
                ValidarAccesoCliente(
                    pedido.Id_cliente
                );

            if (errorAcceso != null)
            {
                return errorAcceso;
            }

            if (
                string.Equals(
                    pedido.Estado,
                    EstadosPedido.Cancelado,
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "El pedido ya se encuentra cancelado."
                });
            }

            if (
                !string.Equals(
                    pedido.Estado,
                    EstadosPedido.Pendiente,
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "Solo se pueden cancelar pedidos pendientes."
                });
            }

            pedido.Estado =
                EstadosPedido.Cancelado;

            foreach (
                var detalle
                in pedido.Detalle_Pedidos
            )
            {
                detalle.Estado =
                    EstadosDetallePedido
                        .Cancelado;
            }

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Pedido cancelado correctamente.",

                idPedido =
                    pedido.Id,

                estado =
                    pedido.Estado
            });
        }


        // =====================================================
        // CLIENTE CONFIRMA ENTREGA
        // =====================================================
        //
        // SOLO CUENTA CLIENTE.
        //
        // PorConfirmarEntrega
        //         ↓
        // Entregado
        //
        // PATCH:
        // api/Pedido/15/ConfirmarEntrega
        //
        // =====================================================

        [Authorize(Roles = "Cliente")]
        [HttpPatch(
            "{id:int}/ConfirmarEntrega"
        )]
        public async Task<IActionResult>
            ConfirmarEntrega(
                int id)
        {
            // =============================================
            // CLIENTE DEL JWT
            // =============================================

            var resultadoCliente =
                ObtenerClienteIdAutenticado();

            if (!resultadoCliente.EsValido)
            {
                return resultadoCliente.Error!;
            }

            // =============================================
            // PEDIDO
            // =============================================

            var pedido =
                await _context.Pedidos
                    .Include(p =>
                        p.Detalle_Pedidos
                    )
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (pedido == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido no existe."
                });
            }

            // =============================================
            // DEBE PERTENECER AL CLIENTE LOGUEADO
            // =============================================

            if (
                pedido.Id_cliente !=
                resultadoCliente.Id
            )
            {
                return StatusCode(
                    StatusCodes
                        .Status403Forbidden,
                    new
                    {
                        message =
                            "No puedes confirmar un pedido que no te pertenece."
                    }
                );
            }

            // =============================================
            // DEBE ESTAR ESPERANDO CONFIRMACIÓN
            // =============================================

            if (
                pedido.Estado !=
                EstadosPedido
                    .PorConfirmarEntrega
            )
            {
                return BadRequest(new
                {
                    message =
                        "Este pedido no está pendiente de confirmación de entrega."
                });
            }

            // =============================================
            // COMPROBAR ENTREGA DEL DISTRIBUIDOR
            // =============================================

            var distribuidorMarcoEntregado =
                await _context
                    .Asignacion_Pedidos
                    .AsNoTracking()
                    .AnyAsync(a =>
                        a.id_pedido == id
                        &&
                        a.Estado ==
                            EstadosAsignacionPedido
                                .Entregado
                        &&
                        a.Fecha_Entrega !=
                            null
                    );

            if (!distribuidorMarcoEntregado)
            {
                return BadRequest(new
                {
                    message =
                        "El distribuidor todavía no ha registrado correctamente la entrega del pedido."
                });
            }

            // =============================================
            // CONFIRMACIÓN FINAL
            // =============================================

            pedido.Estado =
                EstadosPedido.Entregado;

            /*
             * Los detalles también quedan
             * definitivamente entregados.
             */
            foreach (
                var detalle
                in pedido.Detalle_Pedidos
            )
            {
                detalle.Estado =
                    EstadosDetallePedido
                        .Entregado;
            }

            /*
             * MUY IMPORTANTE:
             *
             * NO creamos ningún Pago aquí.
             *
             * Los pagos se gestionan únicamente
             * mediante PagoController.
             */

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Pedido confirmado como recibido correctamente.",

                idPedido =
                    pedido.Id,

                estado =
                    pedido.Estado
            });
        }


        // =====================================================
        // CLIENTE INDICA QUE NO RECIBIÓ
        // =====================================================
        //
        // PorConfirmarEntrega
        //         ↓
        // Devuelto
        //
        // PATCH:
        // api/Pedido/15/NoConfirmarEntrega
        //
        // =====================================================

        [Authorize(Roles = "Cliente")]
        [HttpPatch("{id:int}/NoConfirmarEntrega")]
        [HttpPatch("{id:int}/RechazarEntrega")]
        public async Task<IActionResult>
            NoConfirmarEntrega(
                int id,
                [FromBody]
                RechazarEntregaPedidoDto? dto)
        {
            var resultadoCliente =
                ObtenerClienteIdAutenticado();

            if (!resultadoCliente.EsValido)
            {
                return resultadoCliente.Error!;
            }

            var pedido =
                await _context.Pedidos
                    .Include(p =>
                        p.Detalle_Pedidos
                    )
                    .FirstOrDefaultAsync(
                        p => p.Id == id
                    );

            if (pedido == null)
            {
                return NotFound(new
                {
                    message =
                        "El pedido no existe."
                });
            }

            // =============================================
            // PROPIEDAD
            // =============================================

            if (
                pedido.Id_cliente !=
                resultadoCliente.Id
            )
            {
                return StatusCode(
                    StatusCodes
                        .Status403Forbidden,
                    new
                    {
                        message =
                            "No puedes modificar un pedido que no te pertenece."
                    }
                );
            }

            if (
                pedido.Estado !=
                EstadosPedido
                    .PorConfirmarEntrega
            )
            {
                return BadRequest(new
                {
                    message =
                        "Este pedido no se encuentra pendiente de confirmación."
                });
            }

            /*
             * Con tus estados actuales utilizamos
             * Devuelto para representar que el
             * cliente rechazó la confirmación.
             */
            pedido.Estado =
                EstadosPedido.Devuelto;

            pedido.Motivo_Devolucion =
                !string.IsNullOrWhiteSpace(dto?.Motivo)
                    ? dto.Motivo.Trim()
                    : "Reportado como no recibido por el cliente.";

            pedido.Fecha_Devolucion =
                DateTime.UtcNow;

            foreach (
                var detalle
                in pedido.Detalle_Pedidos
            )
            {
                detalle.Estado =
                    EstadosDetallePedido
                        .Devuelto;
            }

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                message =
                    "El pedido fue reportado como no recibido y marcado como devuelto.",

                idPedido =
                    pedido.Id,

                estado =
                    pedido.Estado,

                motivoDevolucion =
                    pedido.Motivo_Devolucion,

                fechaDevolucion =
                    pedido.Fecha_Devolucion
            });
        }


        // =====================================================
        // CONSULTA BASE PEDIDOS
        // =====================================================

        private IQueryable<Pedido>
            ConstruirConsultaPedidos()
        {
            return _context.Pedidos
                .AsNoTracking()
                .AsSplitQuery()

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
                    );
        }


        // =====================================================
        // CALCULAR SALDOS DE PEDIDOS
        // =====================================================
        //
        // El saldo nunca se confía al frontend.
        //
        // Total pedido - pagos activos
        //
        // =====================================================

        private async Task<
            Dictionary<int, decimal>>
            ObtenerSaldosPendientesAsync(
                List<Pedido> pedidos)
        {
            var pedidosConDeuda =
                pedidos
                    .Where(p =>
                        EsPedidoConDeudaVisible(
                            p.Estado
                        )
                    )
                    .ToList();

            if (
                pedidosConDeuda.Count ==
                0
            )
            {
                return new Dictionary<
                    int,
                    decimal
                >();
            }

            var idsPedidos =
                pedidosConDeuda
                    .Select(p =>
                        p.Id
                    )
                    .ToList();

            var pagos =
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

                    .Select(g =>
                        new
                        {
                            IdPedido =
                                g.Key,

                            TotalPagado =
                                g.Sum(x =>
                                    x.MontoPagado
                                )
                        }
                    )

                    .ToDictionaryAsync(
                        x => x.IdPedido,
                        x => x.TotalPagado
                    );

            var resultado =
                new Dictionary<
                    int,
                    decimal
                >();

            foreach (
                var pedido
                in pedidosConDeuda
            )
            {
                var totalPagado =
                    pagos.TryGetValue(
                        pedido.Id,
                        out var pagado
                    )
                        ? pagado
                        : 0m;

                resultado[pedido.Id] =
                    Math.Max(
                        0,
                        pedido.Total -
                        totalPagado
                    );
            }

            return resultado;
        }


        // =====================================================
        // CALCULAR SALDO INDIVIDUAL
        // =====================================================

        private async Task<decimal>
            CalcularSaldoPendienteAsync(
                Pedido pedido)
        {
            var totalPagado =
                await _context.Pagos
                    .AsNoTracking()

                    .Where(p =>
                        p.Id_pedido ==
                            pedido.Id
                        &&
                        p.Estado !=
                            EstadosPago.Anulado
                    )

                    .SumAsync(p =>
                        (decimal?)
                            p.MontoPagado
                    )
                ?? 0m;

            return Math.Max(
                0,
                pedido.Total -
                totalPagado
            );
        }


        // =====================================================
        // MAPEO PEDIDO
        // =====================================================

        private static
            PedidoRespuestaDto
            MapearPedido(
                Pedido pedido,
                decimal?
                    saldoPendiente)
        {
            return new PedidoRespuestaDto
            {
                Id =
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

                FechaPedido =
                    pedido.Fecha,

                Observacion =
                    pedido.Observacion,

                MotivoEdicion =
                    pedido.Motivo_Edicion,

                MotivoDevolucion =
                    pedido.Motivo_Devolucion,

                FechaDevolucion =
                    pedido.Fecha_Devolucion,

                Total =
                    pedido.Total,

                Estado =
                    pedido.Estado,

                /*
                 * Solo mostramos deuda una vez
                 * que la entrega fue realizada.
                 */
                SaldoPendiente =
                    EsPedidoConDeudaVisible(
                        pedido.Estado
                    )
                        ? saldoPendiente
                            ?? pedido.Total
                        : null,

                Detalles =
                    pedido
                        .Detalle_Pedidos

                        .OrderBy(d =>
                            d.Id
                        )

                        .Select(d =>
                            new
                            DetallePedidoRespuestaDto
                            {
                                Id =
                                    d.Id,

                                IdProducto =
                                    d.Id_producto,

                                Producto =
                                    d.Producto
                                        ?.Nombre
                                    ??
                                    string.Empty,

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

                        .ToList()
            };
        }


        // =====================================================
        // PEDIDO CON DEUDA VISIBLE
        // =====================================================
        //
        // El distribuidor ya entregó físicamente:
        //
        // PorConfirmarEntrega
        //
        // o el cliente ya confirmó:
        //
        // Entregado
        //
        // =====================================================

        private static bool
            EsPedidoConDeudaVisible(
                string? estado)
        {
            return string.Equals(
                       estado,
                       EstadosPedido
                           .PorConfirmarEntrega,
                       StringComparison
                           .OrdinalIgnoreCase
                   )
                   ||
                   string.Equals(
                       estado,
                       EstadosPedido
                           .Entregado,
                       StringComparison
                           .OrdinalIgnoreCase
                   );
        }


        // =====================================================
        // TRANSICIONES ADMINISTRATIVAS
        // =====================================================

        private static string?
            ValidarTransicionAdministrativa(
                string estadoActual,
                string nuevoEstado)
        {
            if (
                string.Equals(
                    estadoActual,
                    nuevoEstado,
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return
                    "El pedido ya se encuentra en ese estado.";
            }

            /*
             * Estados finales.
             */
            if (
                string.Equals(
                    estadoActual,
                    EstadosPedido
                        .Cancelado,
                    StringComparison
                        .OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    estadoActual,
                    EstadosPedido
                        .Entregado,
                    StringComparison
                        .OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    estadoActual,
                    EstadosPedido
                        .Devuelto,
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return
                    "Un pedido finalizado no puede cambiar nuevamente de estado.";
            }

            /*
             * PorConfirmarEntrega solamente
             * puede ser resuelto por el cliente.
             */
            if (
                estadoActual ==
                EstadosPedido
                    .PorConfirmarEntrega
            )
            {
                return
                    "El pedido está esperando confirmación del cliente.";
            }

            /*
             * En la práctica:
             *
             * Pendiente -> Asignado
             *
             * El resto del flujo se maneja
             * desde DistribuidorPedidoController.
             */
            var transicionValida =
                estadoActual ==
                    EstadosPedido
                        .Pendiente
                &&
                nuevoEstado ==
                    EstadosPedido
                        .Asignado;

            return transicionValida
                ? null
                :
                $"No se permite cambiar el pedido de {estadoActual} a {nuevoEstado}.";
        }


        // =====================================================
        // VALIDAR PEDIDO
        // =====================================================

        private async Task<string?>
            ValidarPedidoAsync(
                int idCliente,
                int idSucursal,
                List<
                    CrearDetallePedidoDto
                >? detalles)
        {
            var cliente =
                await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id ==
                            idCliente
                    );

            if (cliente is null)
            {
                return
                    "El cliente seleccionado no existe.";
            }

            if (
                !string.Equals(
                    cliente.Estado,
                    "Activo",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return
                    "El cliente seleccionado se encuentra inactivo.";
            }

            var sucursal =
                await _context.Sucursales
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        s =>
                            s.Id ==
                            idSucursal
                    );

            if (sucursal is null)
            {
                return
                    "La sucursal seleccionada no existe.";
            }

            if (
                sucursal.Id_cliente !=
                idCliente
            )
            {
                return
                    "La sucursal seleccionada no pertenece al cliente indicado.";
            }

            if (
                !string.Equals(
                    sucursal.Estado,
                    "Activo",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return
                    "La sucursal seleccionada se encuentra inactiva.";
            }

            if (
                detalles is null ||
                detalles.Count == 0
            )
            {
                return
                    "Debe agregar al menos un producto al pedido.";
            }

            if (
                detalles.Any(d =>
                    d.Cantidad <= 0
                )
            )
            {
                return
                    "Todas las cantidades deben ser mayores a cero.";
            }

            if (
                detalles
                    .GroupBy(d =>
                        d.IdProducto
                    )
                    .Any(g =>
                        g.Count() > 1
                    )
            )
            {
                return
                    "No debe repetir un producto. Modifique su cantidad en una sola fila.";
            }

            var idsProductos =
                detalles
                    .Select(d =>
                        d.IdProducto
                    )
                    .Distinct()
                    .ToList();

            var productos =
                await _context.Productos
                    .AsNoTracking()
                    .Where(p =>
                        idsProductos
                            .Contains(p.Id)
                    )
                    .ToListAsync();

            if (
                productos.Count !=
                idsProductos.Count
            )
            {
                return
                    "Uno o más productos seleccionados no existen.";
            }

            foreach (
                var detalle
                in detalles
            )
            {
                var producto =
                    productos.First(
                        p =>
                            p.Id ==
                            detalle.IdProducto
                    );

                if (
                    !string.Equals(
                        producto.Estado,
                        "Activo",
                        StringComparison
                            .OrdinalIgnoreCase
                    )
                )
                {
                    return
                        $"El producto {producto.Nombre} se encuentra inactivo.";
                }

                if (
                    producto.Stock <
                    detalle.Cantidad
                )
                {
                    return
                        $"Stock insuficiente para {producto.Nombre}. Disponible: {producto.Stock}.";
                }
            }

            return null;
        }


        // =====================================================
        // SEGURIDAD CLIENTE
        // =====================================================

        private bool EsCuentaCliente()
        {
            return string.Equals(
                User.FindFirstValue(
                    "tipoCuenta"
                ),
                "Cliente",
                StringComparison
                    .OrdinalIgnoreCase
            );
        }


        private ActionResult?
            ValidarAccesoCliente(
                int idCliente)
        {
            /*
             * Si NO es cuenta Cliente,
             * no aplicamos esta comprobación.
             *
             * Administrador/otros roles autorizados
             * continúan normalmente.
             */
            if (!EsCuentaCliente())
            {
                return null;
            }

            var resultado =
                ObtenerClienteIdAutenticado();

            if (!resultado.EsValido)
            {
                return resultado.Error;
            }

            if (
                resultado.Id !=
                idCliente
            )
            {
                return StatusCode(
                    StatusCodes
                        .Status403Forbidden,
                    new
                    {
                        message =
                            "No tienes acceso a los pedidos de otro cliente."
                    }
                );
            }

            return null;
        }


        private ResultadoClienteId
            ObtenerClienteIdAutenticado()
        {
            var tipoCuenta =
                User.FindFirstValue(
                    "tipoCuenta"
                );

            if (
                !string.Equals(
                    tipoCuenta,
                    "Cliente",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return new
                    ResultadoClienteId
                {
                    EsValido =
                            false,

                    Error =
                            StatusCode(
                                StatusCodes
                                    .Status403Forbidden,
                                new
                                {
                                    message =
                                        "La cuenta autenticada no corresponde a un cliente."
                                }
                            )
                };
            }

            var idTexto =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
                )
                ??
                User.FindFirstValue(
                    "sub"
                );

            if (
                !int.TryParse(
                    idTexto,
                    out var clienteId
                )
            )
            {
                return new
                    ResultadoClienteId
                {
                    EsValido =
                            false,

                    Error =
                            Unauthorized(
                                new
                                {
                                    message =
                                        "El token no contiene un identificador de cliente válido."
                                }
                            )
                };
            }

            return new
                ResultadoClienteId
            {
                EsValido =
                        true,

                Id =
                        clienteId
            };
        }


        // =====================================================
        // RESULTADO INTERNO CLIENTE
        // =====================================================

        private class ResultadoClienteId
        {
            public bool EsValido
            {
                get;
                set;
            }

            public int Id
            {
                get;
                set;
            }

            public ActionResult? Error
            {
                get;
                set;
            }
        }
    }
}