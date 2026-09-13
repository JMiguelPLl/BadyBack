using BadyBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BadyBackend.Models;
using static BadyBackend.DTOs.PedidoDtos;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SucursalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SucursalController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("Listar")]
        public async Task<IActionResult> ListarSucursales(
            [FromQuery] string? estado)
        {
            if (!string.IsNullOrWhiteSpace(estado))
            {
                estado = NormalizarEstado(estado);

                if (estado != "Activo" && estado != "Inactivo")
                {
                    return BadRequest(new
                    {
                        message = "El estado solamente puede ser Activo o Inactivo."
                    });
                }
            }

            var consulta = _context.Sucursales
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                consulta = consulta.Where(s => s.Estado == estado);
            }

            var sucursales = await consulta
                .OrderBy(s => s.Nombre)
                .Select(s => new SucursalResponseDto
                {
                    Id = s.Id,
                    IdCliente = s.Id_cliente,
                    Cliente = s.Cliente.Nombre,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    Ubicacion = s.Ubicacion,
                    Estado = s.Estado
                })
                .ToListAsync();

            return Ok(sucursales);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerSucursal(int id)
        {
            var sucursal = await _context.Sucursales
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SucursalResponseDto
                {
                    Id = s.Id,
                    IdCliente = s.Id_cliente,
                    Cliente = s.Cliente.Nombre,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    Ubicacion = s.Ubicacion,
                    Estado = s.Estado
                })
                .FirstOrDefaultAsync();

            if (sucursal == null)
            {
                return NotFound(new
                {
                    message = "Sucursal no encontrada."
                });
            }

            return Ok(sucursal);
        }

        [HttpGet("Cliente/{idCliente:int}")]
        public async Task<IActionResult> ListarSucursalesPorCliente(
            int idCliente,
            [FromQuery] string? estado)
        {
            var clienteExiste = await _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Id == idCliente);

            if (!clienteExiste)
            {
                return NotFound(new
                {
                    message = "Cliente no encontrado."
                });
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                estado = NormalizarEstado(estado);

                if (estado != "Activo" && estado != "Inactivo")
                {
                    return BadRequest(new
                    {
                        message = "El estado solamente puede ser Activo o Inactivo."
                    });
                }
            }

            var consulta = _context.Sucursales
                .AsNoTracking()
                .Where(s => s.Id_cliente == idCliente);

            if (!string.IsNullOrWhiteSpace(estado))
            {
                consulta = consulta.Where(s => s.Estado == estado);
            }

            var sucursales = await consulta
                .OrderBy(s => s.Nombre)
                .Select(s => new SucursalResponseDto
                {
                    Id = s.Id,
                    IdCliente = s.Id_cliente,
                    Cliente = s.Cliente.Nombre,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    Ubicacion = s.Ubicacion,
                    Estado = s.Estado
                })
                .ToListAsync();

            return Ok(sucursales);
        }

        [HttpPost("Agregar")]
        public async Task<IActionResult> CrearSucursal(
            [FromBody] SucursalCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == dto.IdCliente);

            if (cliente == null)
            {
                return NotFound(new
                {
                    message = "El cliente seleccionado no existe."
                });
            }

            if (cliente.Estado != "Activo")
            {
                return BadRequest(new
                {
                    message = "No se puede registrar una sucursal para un cliente inactivo."
                });
            }

            var nombre = dto.Nombre.Trim();

            var sucursalExiste = await _context.Sucursales.AnyAsync(s =>
                s.Id_cliente == dto.IdCliente &&
                s.Nombre.ToLower() == nombre.ToLower()
            );

            if (sucursalExiste)
            {
                return Conflict(new
                {
                    message = "El cliente ya tiene una sucursal con ese nombre."
                });
            }

            var sucursal = new Sucursal
            {
                Id_cliente = dto.IdCliente,
                Nombre = nombre,
                Descripcion = dto.Descripcion.Trim(),
                Ubicacion = dto.Ubicacion.Trim(),
                Estado = "Activo"
            };

            await _context.Sucursales.AddAsync(sucursal);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Sucursal registrada correctamente.",
                sucursal = new SucursalResponseDto
                {
                    Id = sucursal.Id,
                    IdCliente = sucursal.Id_cliente,
                    Cliente = cliente.Nombre,
                    Nombre = sucursal.Nombre,
                    Descripcion = sucursal.Descripcion,
                    Ubicacion = sucursal.Ubicacion,
                    Estado = sucursal.Estado
                }
            });
        }

        [HttpPut("Modificar/{id:int}")]
        public async Task<IActionResult> ActualizarSucursal(
            int id,
            [FromBody] SucursalUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var sucursal = await _context.Sucursales.FindAsync(id);

            if (sucursal == null)
            {
                return NotFound(new
                {
                    message = "Sucursal no encontrada."
                });
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == dto.IdCliente);

            if (cliente == null)
            {
                return NotFound(new
                {
                    message = "El cliente seleccionado no existe."
                });
            }

            if (cliente.Estado != "Activo")
            {
                return BadRequest(new
                {
                    message = "No se puede asignar la sucursal a un cliente inactivo."
                });
            }

            var nombre = dto.Nombre.Trim();

            var nombreExiste = await _context.Sucursales.AnyAsync(s =>
                s.Id_cliente == dto.IdCliente &&
                s.Nombre.ToLower() == nombre.ToLower() &&
                s.Id != id
            );

            if (nombreExiste)
            {
                return Conflict(new
                {
                    message = "El cliente ya tiene otra sucursal con ese nombre."
                });
            }

            sucursal.Id_cliente = dto.IdCliente;
            sucursal.Nombre = nombre;
            sucursal.Descripcion = dto.Descripcion.Trim();
            sucursal.Ubicacion = dto.Ubicacion.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Sucursal actualizada correctamente.",
                sucursal = new SucursalResponseDto
                {
                    Id = sucursal.Id,
                    IdCliente = sucursal.Id_cliente,
                    Cliente = cliente.Nombre,
                    Nombre = sucursal.Nombre,
                    Descripcion = sucursal.Descripcion,
                    Ubicacion = sucursal.Ubicacion,
                    Estado = sucursal.Estado
                }
            });
        }

    

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstadoSucursal(
            int id,
            [FromBody] CambiarEstadoSucursalDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var estado = NormalizarEstado(dto.Estado);

            if (estado != "Activo" && estado != "Inactivo")
            {
                return BadRequest(new
                {
                    message = "El estado solamente puede ser Activo o Inactivo."
                });
            }

            var sucursal = await _context.Sucursales.FindAsync(id);

            if (sucursal == null)
            {
                return NotFound(new
                {
                    message = "Sucursal no encontrada."
                });
            }

            if (estado == "Activo")
            {
                var clienteActivo = await _context.Clientes
                    .AnyAsync(c =>
                        c.Id == sucursal.Id_cliente &&
                        c.Estado == "Activo"
                    );

                if (!clienteActivo)
                {
                    return BadRequest(new
                    {
                        message = "No se puede activar la sucursal porque su cliente está inactivo."
                    });
                }
            }

            sucursal.Estado = estado;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = estado == "Activo"
                    ? "Sucursal activada correctamente."
                    : "Sucursal desactivada correctamente.",

                estado = sucursal.Estado
            });
        }

        private static string NormalizarEstado(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return string.Empty;

            return estado.Trim().ToLowerInvariant() switch
            {
                "activo" => "Activo",
                "inactivo" => "Inactivo",
                _ => estado.Trim()
            };
        }
    }
}