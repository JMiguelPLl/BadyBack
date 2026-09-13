using BadyApi.Helpers;
using BadyBackend.Data;
using BadyBackend.DTOs;
using BadyBackend.DTOs;
using BadyBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class DashboardController
        : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(
            AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // RESUMEN GENERAL
        // =====================================================

        /*
         * GET:
         *
         * api/Dashboard/Resumen
         *
         * api/Dashboard/Resumen?anio=2026&mes=8
         */
        [HttpGet("Resumen")]
        public async Task<ActionResult<
            DashboardResumenDto>>
            ObtenerResumen(
                [FromQuery] int? anio,
                [FromQuery] int? mes)
        {
            var ahora =
                DateTime.UtcNow;

            var anioActual =
                anio ?? ahora.Year;

            var mesActual =
                mes ?? ahora.Month;

            if (
                mesActual < 1 ||
                mesActual > 12
            )
            {
                return BadRequest(new
                {
                    message =
                        "El mes debe estar entre 1 y 12."
                });
            }

            var inicioMes =
                new DateTime(
                    anioActual,
                    mesActual,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                );

            var inicioMesSiguiente =
                inicioMes.AddMonths(1);

            var inicioMesAnterior =
                inicioMes.AddMonths(-1);

            // =============================================
            // INGRESOS DEL MES
            // =============================================

            var ingresosMes =
                await _context.Pagos
                    .AsNoTracking()
                    .Where(p =>
                        p.Estado !=
                            EstadosPago.Anulado &&
                        p.Fecha >= inicioMes &&
                        p.Fecha <
                            inicioMesSiguiente
                    )
                    .SumAsync(p =>
                        (decimal?)
                        p.MontoPagado
                    ) ?? 0m;

            // =============================================
            // INGRESOS MES ANTERIOR
            // =============================================

            var ingresosMesAnterior =
                await _context.Pagos
                    .AsNoTracking()
                    .Where(p =>
                        p.Estado !=
                            EstadosPago.Anulado &&
                        p.Fecha >=
                            inicioMesAnterior &&
                        p.Fecha <
                            inicioMes
                    )
                    .SumAsync(p =>
                        (decimal?)
                        p.MontoPagado
                    ) ?? 0m;

            decimal variacionIngresos = 0m;

            if (ingresosMesAnterior > 0)
            {
                variacionIngresos =
                    (
                        (
                            ingresosMes -
                            ingresosMesAnterior
                        )
                        /
                        ingresosMesAnterior
                    )
                    * 100m;
            }
            else if (ingresosMes > 0)
            {
                variacionIngresos = 100m;
            }

            // =============================================
            // PEDIDOS CONSIDERADOS COBRABLES
            // =============================================

            var pedidosCobrables =
                await _context.Pedidos
                    .AsNoTracking()
                    .Where(p =>
                        p.Estado ==
                            EstadosPedido
                                .PorConfirmarEntrega ||
                        p.Estado ==
                            EstadosPedido
                                .Entregado
                    )
                    .Select(p =>
                        new
                        {
                            p.Id,
                            p.Total,
                            p.Fecha
                        }
                    )
                    .ToListAsync();

            var idsPedidosCobrables =
                pedidosCobrables
                    .Select(p => p.Id)
                    .ToList();

            var pagosPorPedido =
                await _context.Pagos
                    .AsNoTracking()
                    .Where(p =>
                        idsPedidosCobrables
                            .Contains(
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
                            )
                    })
                    .ToDictionaryAsync(
                        x => x.IdPedido,
                        x => x.TotalPagado
                    );

            decimal deudaPendienteTotal = 0m;
            decimal deudaPendienteMes = 0m;
            decimal totalVendidoEntregado = 0m;
            decimal totalCobrado = 0m;

            foreach (
                var pedido
                in pedidosCobrables
            )
            {
                var totalPagado =
                    pagosPorPedido
                        .TryGetValue(
                            pedido.Id,
                            out var pagado
                        )
                        ? pagado
                        : 0m;

                var saldo =
                    Math.Max(
                        0,
                        pedido.Total -
                        totalPagado
                    );

                deudaPendienteTotal += saldo;

                totalVendidoEntregado +=
                    pedido.Total;

                totalCobrado +=
                    Math.Min(
                        pedido.Total,
                        totalPagado
                    );

                if (
                    pedido.Fecha >=
                        inicioMes &&
                    pedido.Fecha <
                        inicioMesSiguiente
                )
                {
                    deudaPendienteMes +=
                        saldo;
                }
            }

            // =============================================
            // PEDIDOS DEL MES
            // =============================================

            var pedidosMes =
                await _context.Pedidos
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.Fecha >=
                            inicioMes &&
                        p.Fecha <
                            inicioMesSiguiente
                    );

            // =============================================
            // PENDIENTES DE CONFIRMACIÓN
            // =============================================

            var pedidosPendientesConfirmacion =
                await _context.Pedidos
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.Estado ==
                        EstadosPedido
                            .PorConfirmarEntrega
                    );

            // =============================================
            // STOCK BAJO
            // =============================================

            var productosStockBajo =
                await _context.Productos
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.Estado ==
                            "Activo" &&
                        p.Stock < 15
                    );

            // =============================================
            // PORCENTAJE DE COBRANZA
            // =============================================

            decimal porcentajeCobranza = 0m;

            if (
                totalVendidoEntregado > 0
            )
            {
                porcentajeCobranza =
                    (
                        totalCobrado /
                        totalVendidoEntregado
                    )
                    * 100m;
            }

            return Ok(
                new DashboardResumenDto
                {
                    Anio =
                        anioActual,

                    Mes =
                        mesActual,

                    IngresosMes =
                        ingresosMes,

                    IngresosMesAnterior =
                        ingresosMesAnterior,

                    VariacionIngresosPorcentaje =
                        Math.Round(
                            variacionIngresos,
                            2
                        ),

                    DeudaPendienteTotal =
                        deudaPendienteTotal,

                    DeudaPendienteMes =
                        deudaPendienteMes,

                    PedidosMes =
                        pedidosMes,

                    PedidosPendientesConfirmacion =
                        pedidosPendientesConfirmacion,

                    ProductosStockBajo =
                        productosStockBajo,

                    TotalVendidoEntregado =
                        totalVendidoEntregado,

                    TotalCobrado =
                        totalCobrado,

                    PorcentajeCobranza =
                        Math.Round(
                            porcentajeCobranza,
                            2
                        )
                }
            );
        }


        // =====================================================
        // INGRESOS MENSUALES
        // =====================================================

        /*
         * GET:
         *
         * api/Dashboard/Ingresos?anio=2026
         */
        [HttpGet("Ingresos")]
        public async Task<ActionResult<
            IEnumerable<IngresoMensualDto>>>
            ObtenerIngresosMensuales(
                [FromQuery] int? anio)
        {
            var anioSeleccionado =
                anio ??
                DateTime.UtcNow.Year;

            var inicioAnio =
                new DateTime(
                    anioSeleccionado,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                );

            var inicioAnioSiguiente =
                inicioAnio.AddYears(1);

            var ingresos =
                await _context.Pagos
                    .AsNoTracking()
                    .Where(p =>
                        p.Estado !=
                            EstadosPago.Anulado &&
                        p.Fecha >=
                            inicioAnio &&
                        p.Fecha <
                            inicioAnioSiguiente
                    )
                    .GroupBy(p =>
                        p.Fecha.Month
                    )
                    .Select(g =>
                        new
                        {
                            Mes =
                                g.Key,

                            Total =
                                g.Sum(x =>
                                    x.MontoPagado
                                )
                        }
                    )
                    .ToListAsync();

            var cultura =
                new CultureInfo(
                    "es-ES"
                );

            var resultado =
                Enumerable
                    .Range(1, 12)
                    .Select(numeroMes =>
                    {
                        var ingreso =
                            ingresos
                                .FirstOrDefault(
                                    x =>
                                        x.Mes ==
                                        numeroMes
                                );

                        var nombreMes =
                            cultura
                                .DateTimeFormat
                                .GetMonthName(
                                    numeroMes
                                );

                        return new
                            IngresoMensualDto
                        {
                            Mes =
                                    numeroMes,

                            NombreMes =
                                    char.ToUpper(
                                        nombreMes[0]
                                    )
                                    +
                                    nombreMes[1..],

                            Total =
                                    ingreso?.Total ??
                                    0m
                        };
                    })
                    .ToList();

            return Ok(resultado);
        }


        // =====================================================
        // ACTIVIDAD DEL DASHBOARD
        // =====================================================

        /*
         * GET:
         *
         * api/Dashboard/Actividad
         */
        [HttpGet("Actividad")]
        public async Task<ActionResult<
            DashboardActividadDto>>
            ObtenerActividad()
        {
            // =============================================
            // PEDIDOS POR ESTADO
            // =============================================

            var pedidosPorEstado =
                await _context.Pedidos
                    .AsNoTracking()
                    .GroupBy(p =>
                        p.Estado
                    )
                    .Select(g =>
                        new PedidosPorEstadoDto
                        {
                            Estado =
                                g.Key,

                            Cantidad =
                                g.Count()
                        }
                    )
                    .OrderByDescending(x =>
                        x.Cantidad
                    )
                    .ToListAsync();

            // =============================================
            // CLIENTES CON MAYOR DEUDA
            // =============================================

            var pedidosCobrables =
                await _context.Pedidos
                    .AsNoTracking()
                    .Include(p =>
                        p.Cliente
                    )
                    .Where(p =>
                        p.Estado ==
                            EstadosPedido
                                .PorConfirmarEntrega ||
                        p.Estado ==
                            EstadosPedido
                                .Entregado
                    )
                    .Select(p =>
                        new
                        {
                            p.Id,
                            p.Id_cliente,

                            Cliente =
                                p.Cliente
                                    .Nombre,

                            p.Total
                        }
                    )
                    .ToListAsync();

            var idsPedidos =
                pedidosCobrables
                    .Select(p =>
                        p.Id
                    )
                    .ToList();

            var pagosAgrupados =
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

            var clientesMayorDeuda =
                pedidosCobrables
                    .Select(p =>
                    {
                        var pagado =
                            pagosAgrupados
                                .TryGetValue(
                                    p.Id,
                                    out var total
                                )
                                ? total
                                : 0m;

                        return new
                        {
                            p.Id_cliente,
                            p.Cliente,

                            Saldo =
                                Math.Max(
                                    0,
                                    p.Total -
                                    pagado
                                )
                        };
                    })
                    .Where(x =>
                        x.Saldo > 0
                    )
                    .GroupBy(x =>
                        new
                        {
                            x.Id_cliente,
                            x.Cliente
                        }
                    )
                    .Select(g =>
                        new ClienteMayorDeudaDto
                        {
                            IdCliente =
                                g.Key.Id_cliente,

                            Cliente =
                                g.Key.Cliente,

                            DeudaPendiente =
                                g.Sum(x =>
                                    x.Saldo
                                ),

                            CantidadPedidosConDeuda =
                                g.Count()
                        }
                    )
                    .OrderByDescending(x =>
                        x.DeudaPendiente
                    )
                    .Take(5)
                    .ToList();

            // =============================================
            // PRODUCTOS CON STOCK BAJO
            // =============================================

            var productosStockBajo =
                await _context.Productos
                    .AsNoTracking()
                    .Where(p =>
                        p.Estado ==
                            "Activo" &&
                        p.Stock < 15
                    )
                    .OrderBy(p =>
                        p.Stock
                    )
                    .ThenBy(p =>
                        p.Nombre
                    )
                    .Take(10)
                    .Select(p =>
                        new ProductoStockBajoDto
                        {
                            IdProducto =
                                p.Id,

                            Producto =
                                p.Nombre,

                            Stock =
                                p.Stock,

                            Precio =
                                p.Precio,

                            Estado =
                                p.Estado
                        }
                    )
                    .ToListAsync();

            // =============================================
            // ÚLTIMOS PAGOS
            // =============================================

            var ultimosPagos =
                await _context.Pagos
                    .AsNoTracking()
                    .Where(p =>
                        p.Estado !=
                            EstadosPago.Anulado
                    )
                    .Include(p =>
                        p.Usuario
                    )
                    .Include(p =>
                        p.TipoPago
                    )
                    .Include(p =>
                        p.Pedido
                    )
                        .ThenInclude(p =>
                            p.Cliente
                        )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ThenByDescending(p =>
                        p.Id
                    )
                    .Take(5)
                    .Select(p =>
                        new UltimoPagoDto
                        {
                            IdPago =
                                p.Id,

                            IdPedido =
                                p.Id_pedido,

                            Cliente =
                                p.Pedido
                                    .Cliente
                                    .Nombre,

                            Usuario =
                                p.Usuario
                                    .Nombre,

                            TipoPago =
                                p.TipoPago
                                    .Descripcion,

                            FechaPago =
                                p.Fecha,

                            MontoPagado =
                                p.MontoPagado,

                            SaldoPendiente =
                                p.SaldoPendiente
                        }
                    )
                    .ToListAsync();

            // =============================================
            // ÚLTIMOS PEDIDOS
            // =============================================

            var ultimosPedidos =
                await _context.Pedidos
                    .AsNoTracking()
                    .Include(p =>
                        p.Cliente
                    )
                    .Include(p =>
                        p.Sucursal
                    )
                    .OrderByDescending(p =>
                        p.Fecha
                    )
                    .ThenByDescending(p =>
                        p.Id
                    )
                    .Take(5)
                    .Select(p =>
                        new UltimoPedidoDto
                        {
                            IdPedido =
                                p.Id,

                            Cliente =
                                p.Cliente
                                    .Nombre,

                            Sucursal =
                                p.Sucursal
                                    .Nombre,

                            FechaPedido =
                                p.Fecha,

                            Total =
                                p.Total,

                            Estado =
                                p.Estado
                        }
                    )
                    .ToListAsync();

            return Ok(
                new DashboardActividadDto
                {
                    PedidosPorEstado =
                        pedidosPorEstado,

                    ClientesMayorDeuda =
                        clientesMayorDeuda,

                    ProductosStockBajo =
                        productosStockBajo,

                    UltimosPagos =
                        ultimosPagos,

                    UltimosPedidos =
                        ultimosPedidos
                }
            );
        }
    }
}