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
    public class CierreCajaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CierreCajaController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // OBTENER ID DEL USUARIO AUTENTICADO DESDE JWT
        // =====================================================
        private int? ObtenerIdUsuarioActual()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(claim))
                return null;

            if (!int.TryParse(claim, out var idUsuario))
                return null;

            return idUsuario;
        }

        private bool EsAdministrador()
        {
            return User.IsInRole("Administrador");
        }


        // =====================================================
        // OBTENER CAJA ACTUAL ABIERTA DEL USUARIO
        // =====================================================
        /*
         * GET: api/CierreCaja/CajaActual
         */
        [HttpGet("CajaActual")]
        public async Task<ActionResult<CierreCajaDetalleCompletoDto>> ObtenerCajaActual()
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message = "No se pudo identificar al usuario autenticado."
                });
            }

            var caja = await _context.Cierre_Cajas
                .AsNoTracking()
                .Include(c => c.Usuario)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.TipoPago)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.Pedido)
                            .ThenInclude(ped => ped.Cliente)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.Pedido)
                            .ThenInclude(ped => ped.Sucursal)
                .FirstOrDefaultAsync(c =>
                    c.Id_usuario == idUsuario.Value &&
                    c.Estado == EstadosCierreCaja.Abierta
                );

            if (caja == null)
            {
                return NotFound(new
                {
                    message = "No tienes una caja abierta en este momento."
                });
            }

            return Ok(MapearDetalleCompleto(caja));
        }


        // =====================================================
        // APERTURA MANUAL DE CAJA
        // =====================================================
        /*
         * POST: api/CierreCaja/Apertura
         */
        [HttpPost("Apertura")]
        public async Task<IActionResult> AbrirCaja([FromBody] AperturaCajaDto? dto)
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message = "No se pudo identificar al usuario autenticado."
                });
            }

            var cajaAbiertaExistente = await _context.Cierre_Cajas
                .FirstOrDefaultAsync(c =>
                    c.Id_usuario == idUsuario.Value &&
                    c.Estado == EstadosCierreCaja.Abierta
                );

            if (cajaAbiertaExistente != null)
            {
                return BadRequest(new
                {
                    message = "Ya tienes una caja abierta actualmente.",
                    idCierreCaja = cajaAbiertaExistente.Id
                });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == idUsuario.Value);

            if (usuario == null)
            {
                return NotFound(new
                {
                    message = "El usuario autenticado no existe en la base de datos."
                });
            }

            var nuevaCaja = new Cierre_Caja
            {
                Id_usuario = idUsuario.Value,
                Fecha_Apertura = DateTime.UtcNow,
                Fecha_Cierre = null,
                Total_Efectivo = 0m,
                Total_QR = 0m,
                Total_Recaudado = 0m,
                Estado = EstadosCierreCaja.Abierta,
                Observacion = dto?.Observacion
            };

            await _context.Cierre_Cajas.AddAsync(nuevaCaja);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Caja abierta correctamente.",
                caja = new CierreCajaRespuestaDto
                {
                    Id = nuevaCaja.Id,
                    IdUsuario = nuevaCaja.Id_usuario,
                    Usuario = usuario.Nombre,
                    CorreoUsuario = usuario.Email,
                    FechaApertura = nuevaCaja.Fecha_Apertura,
                    FechaCierre = nuevaCaja.Fecha_Cierre,
                    TotalEfectivo = nuevaCaja.Total_Efectivo,
                    TotalQR = nuevaCaja.Total_QR,
                    TotalRecaudado = nuevaCaja.Total_Recaudado,
                    Estado = nuevaCaja.Estado,
                    Observacion = nuevaCaja.Observacion,
                    CantidadPagos = 0
                }
            });
        }


        // =====================================================
        // CERRAR CAJA ACTUAL DEL USUARIO
        // =====================================================
        /*
         * POST: api/CierreCaja/CerrarActual
         */
        [HttpPost("CerrarActual")]
        public async Task<IActionResult> CerrarCajaActual([FromBody] CerrarCajaDto? dto)
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message = "No se pudo identificar al usuario autenticado."
                });
            }

            var caja = await _context.Cierre_Cajas
                .Include(c => c.Usuario)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.TipoPago)
                .FirstOrDefaultAsync(c =>
                    c.Id_usuario == idUsuario.Value &&
                    c.Estado == EstadosCierreCaja.Abierta
                );

            if (caja == null)
            {
                return NotFound(new
                {
                    message = "No tienes ninguna caja abierta para cerrar."
                });
            }

            return await EjecutarCierreCaja(caja, dto?.Observacion);
        }


        // =====================================================
        // CERRAR CAJA POR ID
        // =====================================================
        /*
         * POST: api/CierreCaja/{id:int}/Cerrar
         */
        [HttpPost("{id:int}/Cerrar")]
        public async Task<IActionResult> CerrarCajaPorId(int id, [FromBody] CerrarCajaDto? dto)
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message = "No se pudo identificar al usuario autenticado."
                });
            }

            var caja = await _context.Cierre_Cajas
                .Include(c => c.Usuario)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.TipoPago)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (caja == null)
            {
                return NotFound(new
                {
                    message = "La caja indicada no existe."
                });
            }

            if (caja.Estado != EstadosCierreCaja.Abierta)
            {
                return BadRequest(new
                {
                    message = $"La caja ya se encuentra en estado '{caja.Estado}'."
                });
            }

            // Solo el dueño de la caja o un Administrador pueden cerrarla
            if (caja.Id_usuario != idUsuario.Value && !EsAdministrador())
            {
                return Forbid();
            }

            return await EjecutarCierreCaja(caja, dto?.Observacion);
        }


        // =====================================================
        // DETALLE DE UN CIERRE DE CAJA POR ID
        // =====================================================
        /*
         * GET: api/CierreCaja/{id:int}
         */
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CierreCajaDetalleCompletoDto>> ObtenerPorId(int id)
        {
            var idUsuario = ObtenerIdUsuarioActual();
            if (!idUsuario.HasValue)
            {
                return Unauthorized(new
                {
                    message = "No se pudo identificar al usuario autenticado."
                });
            }

            var caja = await _context.Cierre_Cajas
                .AsNoTracking()
                .Include(c => c.Usuario)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.TipoPago)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.Pedido)
                            .ThenInclude(ped => ped.Cliente)
                .Include(c => c.Cierre_Caja_Detalles)
                    .ThenInclude(d => d.Pago)
                        .ThenInclude(p => p.Pedido)
                            .ThenInclude(ped => ped.Sucursal)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (caja == null)
            {
                return NotFound(new
                {
                    message = "El registro de cierre de caja no existe."
                });
            }

            if (caja.Id_usuario != idUsuario.Value && !EsAdministrador())
            {
                return Forbid();
            }

            return Ok(MapearDetalleCompleto(caja));
        }


        // =====================================================
        // HISTORIAL DE CIERRES DE CAJA
        // =====================================================
        /*
         * GET:
         * api/CierreCaja/Historial
         * api/CierreCaja/Historial?estado=Cerrada
         * api/CierreCaja/Historial?idUsuario=3
         * api/CierreCaja/Historial?fechaDesde=2026-08-01&fechaHasta=2026-08-31
         */
        [HttpGet("Historial")]
        public async Task<ActionResult<IEnumerable<CierreCajaRespuestaDto>>> ListarHistorial(
            [FromQuery] string? estado,
            [FromQuery] int? idUsuario,
            [FromQuery] DateTime? fechaDesde,
            [FromQuery] DateTime? fechaHasta)
        {
            var idUsuarioActual = ObtenerIdUsuarioActual();
            if (!idUsuarioActual.HasValue)
            {
                return Unauthorized(new
                {
                    message = "No se pudo identificar al usuario autenticado."
                });
            }

            var consulta = _context.Cierre_Cajas
                .AsNoTracking()
                .Include(c => c.Usuario)
                .Include(c => c.Cierre_Caja_Detalles)
                .AsQueryable();

            // Si no es Administrador, solo puede consultar sus propios cierres
            if (!EsAdministrador())
            {
                consulta = consulta.Where(c => c.Id_usuario == idUsuarioActual.Value);
            }
            else if (idUsuario.HasValue)
            {
                consulta = consulta.Where(c => c.Id_usuario == idUsuario.Value);
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estadoNormalizado = EstadosCierreCaja.Normalizar(estado);
                if (estadoNormalizado == null)
                {
                    return BadRequest(new
                    {
                        message = "El estado indicado no es válido. Opciones: Abierta, Cerrada, Anulada."
                    });
                }

                consulta = consulta.Where(c => c.Estado == estadoNormalizado);
            }

            if (fechaDesde.HasValue)
            {
                var desde = fechaDesde.Value.Date;
                consulta = consulta.Where(c => c.Fecha_Apertura >= desde);
            }

            if (fechaHasta.HasValue)
            {
                var hasta = fechaHasta.Value.Date.AddDays(1);
                consulta = consulta.Where(c => c.Fecha_Apertura < hasta);
            }

            var cierres = await consulta
                .OrderByDescending(c => c.Fecha_Apertura)
                .ThenByDescending(c => c.Id)
                .Select(c => new CierreCajaRespuestaDto
                {
                    Id = c.Id,
                    IdUsuario = c.Id_usuario,
                    Usuario = c.Usuario.Nombre,
                    CorreoUsuario = c.Usuario.Email,
                    FechaApertura = c.Fecha_Apertura,
                    FechaCierre = c.Fecha_Cierre,
                    TotalEfectivo = c.Total_Efectivo,
                    TotalQR = c.Total_QR,
                    TotalRecaudado = c.Total_Recaudado,
                    Estado = c.Estado,
                    Observacion = c.Observacion,
                    CantidadPagos = c.Cierre_Caja_Detalles.Count
                })
                .ToListAsync();

            return Ok(cierres);
        }


        // =====================================================
        // RESUMEN GENERAL DE CAJAS
        // =====================================================
        /*
         * GET: api/CierreCaja/Resumen
         */
        [HttpGet("Resumen")]
        public async Task<ActionResult<CierreCajaResumenGeneralDto>> ObtenerResumen()
        {
            var idUsuarioActual = ObtenerIdUsuarioActual();
            if (!idUsuarioActual.HasValue)
            {
                return Unauthorized(new
                {
                    message = "No se pudo identificar al usuario autenticado."
                });
            }

            var consulta = _context.Cierre_Cajas.AsNoTracking();

            if (!EsAdministrador())
            {
                consulta = consulta.Where(c => c.Id_usuario == idUsuarioActual.Value);
            }

            var cajas = await consulta.ToListAsync();

            var cerradas = cajas.Where(c => c.Estado == EstadosCierreCaja.Cerrada).ToList();
            var abiertas = cajas.Where(c => c.Estado == EstadosCierreCaja.Abierta).ToList();

            var resumen = new CierreCajaResumenGeneralDto
            {
                TotalCajasCerradas = cerradas.Count,
                TotalCajasAbiertas = abiertas.Count,
                TotalRecaudadoCerradas = cerradas.Sum(c => c.Total_Recaudado),
                TotalRecaudadoAbiertas = abiertas.Sum(c => c.Total_Recaudado),
                TotalEfectivoGeneral = cajas.Sum(c => c.Total_Efectivo),
                TotalQRGeneral = cajas.Sum(c => c.Total_QR),
                GranTotalRecaudado = cajas.Sum(c => c.Total_Recaudado)
            };

            return Ok(resumen);
        }


        // =====================================================
        // MÉTODOS PRIVADOS AUXILIARES
        // =====================================================

        private async Task<IActionResult> EjecutarCierreCaja(Cierre_Caja caja, string? observacion)
        {
            // Recalcular totales con pagos válidos para máxima consistencia
            var pagos = caja.Cierre_Caja_Detalles
                .Select(d => d.Pago)
                .Where(p => p != null && p.Estado != EstadosPago.Anulado)
                .ToList();

            decimal totalEfectivo = 0m;
            decimal totalQR = 0m;

            foreach (var p in pagos)
            {
                var desc = p.TipoPago?.Descripcion?.ToLower() ?? string.Empty;

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
            caja.Fecha_Cierre = DateTime.UtcNow;
            caja.Estado = EstadosCierreCaja.Cerrada;

            if (!string.IsNullOrWhiteSpace(observacion))
            {
                caja.Observacion = string.IsNullOrWhiteSpace(caja.Observacion)
                    ? observacion.Trim()
                    : $"{caja.Observacion} | Cierre: {observacion.Trim()}";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Caja cerrada correctamente.",
                cierreCaja = new CierreCajaRespuestaDto
                {
                    Id = caja.Id,
                    IdUsuario = caja.Id_usuario,
                    Usuario = caja.Usuario?.Nombre ?? string.Empty,
                    CorreoUsuario = caja.Usuario?.Email,
                    FechaApertura = caja.Fecha_Apertura,
                    FechaCierre = caja.Fecha_Cierre,
                    TotalEfectivo = caja.Total_Efectivo,
                    TotalQR = caja.Total_QR,
                    TotalRecaudado = caja.Total_Recaudado,
                    Estado = caja.Estado,
                    Observacion = caja.Observacion,
                    CantidadPagos = pagos.Count
                }
            });
        }

        private static CierreCajaDetalleCompletoDto MapearDetalleCompleto(Cierre_Caja caja)
        {
            var pagosDto = caja.Cierre_Caja_Detalles
                .OrderByDescending(d => d.Pago != null ? d.Pago.Fecha : d.Fecha)
                .Select(d => new CierreCajaDetalleItemDto
                {
                    Id = d.Id,
                    IdPago = d.Id_pago,
                    IdPedido = d.Pago?.Id_pedido ?? 0,
                    IdCliente = d.Pago?.Pedido?.Id_cliente ?? 0,
                    Cliente = d.Pago?.Pedido?.Cliente?.Nombre ?? string.Empty,
                    IdSucursal = d.Pago?.Pedido?.Id_sucursal ?? 0,
                    Sucursal = d.Pago?.Pedido?.Sucursal?.Nombre ?? string.Empty,
                    IdTipoPago = d.Pago?.Id_tipoPago ?? 0,
                    TipoPago = d.Pago?.TipoPago?.Descripcion ?? string.Empty,
                    MontoPagado = d.Pago?.MontoPagado ?? 0m,
                    FechaPago = d.Pago?.Fecha ?? d.Fecha,
                    EstadoPago = d.Pago?.Estado ?? string.Empty
                })
                .ToList();

            return new CierreCajaDetalleCompletoDto
            {
                Id = caja.Id,
                IdUsuario = caja.Id_usuario,
                Usuario = caja.Usuario?.Nombre ?? string.Empty,
                CorreoUsuario = caja.Usuario?.Email,
                FechaApertura = caja.Fecha_Apertura,
                FechaCierre = caja.Fecha_Cierre,
                TotalEfectivo = caja.Total_Efectivo,
                TotalQR = caja.Total_QR,
                TotalRecaudado = caja.Total_Recaudado,
                Estado = caja.Estado,
                Observacion = caja.Observacion,
                CantidadPagos = pagosDto.Count,
                Pagos = pagosDto
            };
        }
    }
}
