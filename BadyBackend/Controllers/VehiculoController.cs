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
  
    public class VehiculoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehiculoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("Listar")]
        public async Task<ActionResult<IEnumerable<VehiculoResponseDto>>> Listar()
        {
            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .OrderBy(v => v.Marca)
                .ThenBy(v => v.Placa)
                .Select(v => new VehiculoResponseDto
                {
                    Id = v.Id,
                    Marca = v.Marca,
                    Placa = v.Placa,
                    CantidadCarga = v.Cantidad_Carga,
                    Estado = v.Estado
                })
                .ToListAsync();

            return Ok(vehiculos);
        }

        [HttpGet("Activos")]
        public async Task<ActionResult<IEnumerable<VehiculoResponseDto>>> ListarActivos()
        {
            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Estado == "Activo")
                .OrderBy(v => v.Marca)
                .ThenBy(v => v.Placa)
                .Select(v => new VehiculoResponseDto
                {
                    Id = v.Id,
                    Marca = v.Marca,
                    Placa = v.Placa,
                    CantidadCarga = v.Cantidad_Carga,
                    Estado = v.Estado
                })
                .ToListAsync();

            return Ok(vehiculos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<VehiculoResponseDto>> ObtenerPorId(int id)
        {
            var vehiculo = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Id == id)
                .Select(v => new VehiculoResponseDto
                {
                    Id = v.Id,
                    Marca = v.Marca,
                    Placa = v.Placa,
                    CantidadCarga = v.Cantidad_Carga,
                    Estado = v.Estado
                })
                .FirstOrDefaultAsync();

            if (vehiculo is null)
                return NotFound(new { message = "El vehículo no existe." });

            return Ok(vehiculo);
        }

        [HttpPost("Agregar")]
        public async Task<ActionResult> Agregar([FromBody] VehiculoCreateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var placa = dto.Placa.Trim().ToUpperInvariant();

            var existe = await _context.Vehiculos.AnyAsync(v =>
                v.Placa != null && v.Placa.ToUpper() == placa);

            if (existe)
                return Conflict(new { message = $"Ya existe un vehículo registrado con la placa '{placa}'." });

            var vehiculo = new Vehiculo
            {
                Marca = dto.Marca.Trim(),
                Placa = placa,
                Cantidad_Carga = dto.CantidadCarga.Trim(),
                Estado = "Activo"
            };

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Vehículo registrado correctamente.",
                vehiculo = new VehiculoResponseDto
                {
                    Id = vehiculo.Id,
                    Marca = vehiculo.Marca,
                    Placa = vehiculo.Placa,
                    CantidadCarga = vehiculo.Cantidad_Carga,
                    Estado = vehiculo.Estado
                }
            });
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Actualizar(int id, [FromBody] VehiculoUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.Id == id);

            if (vehiculo is null)
                return NotFound(new { message = "El vehículo no existe." });

            var placa = dto.Placa.Trim().ToUpperInvariant();

            var existe = await _context.Vehiculos.AnyAsync(v =>
                v.Id != id && v.Placa != null && v.Placa.ToUpper() == placa);

            if (existe)
                return Conflict(new { message = $"Ya existe otro vehículo registrado con la placa '{placa}'." });

            vehiculo.Marca = dto.Marca.Trim();
            vehiculo.Placa = placa;
            vehiculo.Cantidad_Carga = dto.CantidadCarga.Trim();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Vehículo actualizado correctamente." });
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<ActionResult> CambiarEstado(
            int id,
            [FromBody] CambiarEstadoVehiculoDto dto)
        {
            var estado = NormalizarEstado(dto.Estado);

            if (estado is null)
                return BadRequest(new { message = "El estado solamente puede ser Activo o Inactivo." });

            var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.Id == id);

            if (vehiculo is null)
                return NotFound(new { message = "El vehículo no existe." });

            if (vehiculo.Estado == estado)
                return BadRequest(new { message = $"El vehículo ya se encuentra {estado}." });

            if (estado == "Inactivo")
            {
                var asignacionActiva = await _context.Asignacion_Vehiculos.AnyAsync(a =>
                    a.id_vehiculo == id && a.Estado == "Activo");

                if (asignacionActiva)
                    return BadRequest(new
                    {
                        message = "No se puede desactivar el vehículo porque tiene una asignación activa."
                    });
            }

            vehiculo.Estado = estado;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = estado == "Activo"
                    ? "Vehículo activado correctamente."
                    : "Vehículo desactivado correctamente.",
                estado = vehiculo.Estado
            });
        }

        private static string? NormalizarEstado(string? estado)
        {
            return estado?.Trim().ToLowerInvariant() switch
            {
                "activo" => "Activo",
                "inactivo" => "Inactivo",
                _ => null
            };
        }
    }
}
