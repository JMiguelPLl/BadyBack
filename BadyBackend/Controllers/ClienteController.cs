using BadyBackend.Models;
using BadyBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static BadyBackend.DTOs.ClienteDtos;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClienteController(AppDbContext context)
        {
            _context = context;
        }

        /*
         * Registro público.
         * No necesita token porque todavía no inició sesión.
         *
         * POST: api/Cliente/Registrar
         */
        [AllowAnonymous]
        [HttpPost("Registrar")]
        public async Task<IActionResult> RegistrarCliente(
            [FromBody] ClienteCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emailNormalizado = dto.Email
                .Trim()
                .ToLowerInvariant();

            var emailExiste = await _context.Clientes
                .AnyAsync(c => c.Email.ToLower() == emailNormalizado);

            if (emailExiste)
            {
                return Conflict(new
                {
                    message = "Ya existe un cliente registrado con ese correo."
                });
            }

            var cliente = new Cliente
            {
                Nombre = dto.Nombre.Trim(),
                Numero = dto.Numero.Trim(),
                Email = emailNormalizado,

                // Temporalmente se guarda directamente.
                // Más adelante debe guardarse usando hash.
                Contraseña = dto.Contrasena,

                Estado = "Activo"
            };

            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cliente registrado correctamente.",
                cliente = new ClienteResponseDto
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Numero = cliente.Numero,
                    Email = cliente.Email,
                    Estado = cliente.Estado
                }
            });
        }

        /*
         * Obtiene la información del cliente autenticado.
         *
         * GET: api/Cliente/MiPerfil
         */
       
        [HttpGet("MiPerfil")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var resultadoId = ObtenerClienteIdDelToken();

            if (!resultadoId.EsValido)
                return resultadoId.Error!;

            var cliente = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Id == resultadoId.Id)
                .Select(c => new ClienteResponseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Numero = c.Numero,
                    Email = c.Email,
                    Estado = c.Estado
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound(new
                {
                    message = "Cliente no encontrado."
                });
            }

            return Ok(cliente);
        }

        /*
         * El cliente modifica solamente su propia cuenta.
         *
         * PUT: api/Cliente/MiPerfil
         */
        [HttpPut("MiPerfil")]
        public async Task<IActionResult> ActualizarMiPerfil(
            [FromBody] ClienteUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultadoId = ObtenerClienteIdDelToken();

            if (!resultadoId.EsValido)
                return resultadoId.Error!;

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == resultadoId.Id);

            if (cliente == null)
            {
                return NotFound(new
                {
                    message = "Cliente no encontrado."
                });
            }

            if (cliente.Estado != "Activo")
            {
                return BadRequest(new
                {
                    message = "La cuenta del cliente se encuentra inactiva."
                });
            }

            var emailNormalizado = dto.Email
                .Trim()
                .ToLowerInvariant();

            var emailExiste = await _context.Clientes.AnyAsync(c =>
                c.Email.ToLower() == emailNormalizado &&
                c.Id != cliente.Id
            );

            if (emailExiste)
            {
                return Conflict(new
                {
                    message = "El correo ya está registrado por otro cliente."
                });
            }

            cliente.Nombre = dto.Nombre.Trim();
            cliente.Numero = dto.Numero.Trim();
            cliente.Email = emailNormalizado;

            /*
             * Si no manda contraseña o la manda vacía,
             * se mantiene la actual.
             */
            if (!string.IsNullOrWhiteSpace(dto.Contrasena))
            {
                cliente.Contraseña = dto.Contrasena;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Perfil actualizado correctamente.",
                cliente = new ClienteResponseDto
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Numero = cliente.Numero,
                    Email = cliente.Email,
                    Estado = cliente.Estado
                }
            });
        }

        private ResultadoClienteId ObtenerClienteIdDelToken()
        {
            var tipoCuenta = User.FindFirstValue("tipoCuenta");

            if (tipoCuenta != "Cliente")
            {
                return new ResultadoClienteId
                {
                    EsValido = false,
                    Error = StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        message = "La cuenta autenticada no corresponde a un cliente."
                    })
                };
            }

            var idTexto = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idTexto, out var clienteId))
            {
                return new ResultadoClienteId
                {
                    EsValido = false,
                    Error = Unauthorized(new
                    {
                        message = "El token no contiene un identificador válido."
                    })
                };
            }

            return new ResultadoClienteId
            {
                EsValido = true,
                Id = clienteId
            };
        }
        [HttpGet("Listar")]
        public async Task<IActionResult> ListarClientes([FromQuery] string? estado)
        {
            if (!string.IsNullOrWhiteSpace(estado))
            {
                estado = estado.Trim().ToLower() switch
                {
                    "activo" => "Activo",
                    "inactivo" => "Inactivo",
                    _ => estado
                };

                if (estado != "Activo" && estado != "Inactivo")
                {
                    return BadRequest(new
                    {
                        message = "El estado solamente puede ser Activo o Inactivo."
                    });
                }
            }

            var consulta = _context.Clientes
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                consulta = consulta.Where(c => c.Estado == estado);
            }

            var clientes = await consulta
                .OrderBy(c => c.Nombre)
                .Select(c => new ClienteResponseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Contraseña=c.Contraseña,
                    Numero = c.Numero,
                    Email = c.Email,
                    Estado = c.Estado
                })
                .ToListAsync();

            return Ok(clientes);
        }
        [HttpGet("Activos")]
        public async Task<IActionResult> ListarClientesActivos()
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Estado == "Activo")
                .OrderBy(c => c.Nombre)
                .Select(c => new ClienteResponseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Numero = c.Numero,
                    Email = c.Email,
                    Estado = c.Estado
                })
                .ToListAsync();

            return Ok(clientes);
        }
        [HttpGet("Inactivos")]
        public async Task<IActionResult> ListarClientesInactivos()
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Estado == "Inactivo")
                .OrderBy(c => c.Nombre)
                .Select(c => new ClienteResponseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Numero = c.Numero,
                    Email = c.Email,
                    Estado = c.Estado
                })
                .ToListAsync();

            return Ok(clientes);
        }
      
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstadoCliente(
            int id,
            [FromBody] CambiarEstadoClienteDto dto)
        {
            var estado = dto.Estado?.Trim().ToLower() switch
            {
                "activo" => "Activo",
                "inactivo" => "Inactivo",
                _ => dto.Estado
            };

            if (estado != "Activo" && estado != "Inactivo")
            {
                return BadRequest(new
                {
                    message = "El estado solamente puede ser Activo o Inactivo."
                });
            }

            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound(new
                {
                    message = "Cliente no encontrado."
                });
            }

            cliente.Estado = estado;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = estado == "Activo"
                    ? "Cliente activado correctamente."
                    : "Cliente desactivado correctamente.",
                estado = cliente.Estado
            });
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarCliente(
    int id,
    [FromBody] ClienteUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound(new
                {
                    message = "Cliente no encontrado."
                });
            }

            var emailNormalizado = dto.Email
                .Trim()
                .ToLowerInvariant();

            var emailExiste = await _context.Clientes.AnyAsync(c =>
                c.Email.ToLower() == emailNormalizado &&
                c.Id != id
            );

            if (emailExiste)
            {
                return Conflict(new
                {
                    message = "El correo ya está registrado por otro cliente."
                });
            }

            cliente.Nombre = dto.Nombre.Trim();
            cliente.Numero = dto.Numero.Trim();
            cliente.Email = emailNormalizado;

            if (!string.IsNullOrWhiteSpace(dto.Contrasena))
            {
                cliente.Contraseña = dto.Contrasena;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cliente actualizado correctamente.",
                cliente = new ClienteResponseDto
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Numero = cliente.Numero,
                    Email = cliente.Email,
                    Contraseña=cliente.Contraseña,
                    Estado = cliente.Estado
                }
            });
        }
        private class ResultadoClienteId
        {
            public bool EsValido { get; set; }
            public int Id { get; set; }
            public IActionResult? Error { get; set; }
        }
    }
}