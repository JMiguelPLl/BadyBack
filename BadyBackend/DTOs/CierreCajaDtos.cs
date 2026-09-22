using System.ComponentModel.DataAnnotations;

namespace BadyBackend.DTOs
{
    public class AperturaCajaDto
    {
        [MaxLength(500)]
        public string? Observacion { get; set; }
    }

    public class CerrarCajaDto
    {
        [MaxLength(500)]
        public string? Observacion { get; set; }
    }

    public class CierreCajaRespuestaDto
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? CorreoUsuario { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalQR { get; set; }
        public decimal TotalRecaudado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Observacion { get; set; }
        public int CantidadPagos { get; set; }
    }

    public class CierreCajaDetalleItemDto
    {
        public int Id { get; set; }
        public int IdPago { get; set; }
        public int IdPedido { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public int IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public int IdTipoPago { get; set; }
        public string TipoPago { get; set; } = string.Empty;
        public decimal MontoPagado { get; set; }
        public DateTime FechaPago { get; set; }
        public string EstadoPago { get; set; } = string.Empty;
    }

    public class CierreCajaDetalleCompletoDto
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? CorreoUsuario { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalQR { get; set; }
        public decimal TotalRecaudado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Observacion { get; set; }
        public int CantidadPagos { get; set; }
        public List<CierreCajaDetalleItemDto> Pagos { get; set; } = new();
    }

    public class CierreCajaResumenGeneralDto
    {
        public int TotalCajasCerradas { get; set; }
        public int TotalCajasAbiertas { get; set; }
        public decimal TotalRecaudadoCerradas { get; set; }
        public decimal TotalRecaudadoAbiertas { get; set; }
        public decimal TotalEfectivoGeneral { get; set; }
        public decimal TotalQRGeneral { get; set; }
        public decimal GranTotalRecaudado { get; set; }
    }

    public class EstadoCajaDistribuidorDto
    {
        public int IdUsuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool TieneCajaAbierta { get; set; }
        public int? IdCierreCajaAbierta { get; set; }
        public DateTime? FechaApertura { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalQR { get; set; }
        public decimal TotalRecaudado { get; set; }
        public int CantidadPagos { get; set; }
    }

    public class CierreMasivoRespuestaDto
    {
        public string Mensaje { get; set; } = string.Empty;
        public int TotalProcesados { get; set; }
        public int CajasAfectadas { get; set; }
        public List<CierreCajaRespuestaDto> DetalleCajas { get; set; } = new();
    }
}
