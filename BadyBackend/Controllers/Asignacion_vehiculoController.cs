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
    public class Asignacion_vehiculoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public Asignacion_vehiculoController(
            AppDbContext context)
        {
            _context = context;
        }

        /*
         * Lista todas las asignaciones.
         *
         * GET: api/AsignacionVehiculo/Listar
         */
        [HttpGet("Listar")]
        public async Task<ActionResult<
            IEnumerable<AsignacionVehiculoResponseDto>>> Listar()
        {
            var asignaciones = await ConstruirConsultaAsignaciones()
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var respuesta = asignaciones
                .Select(MapearAsignacion)
                .ToList();

            return Ok(respuesta);
        }

        /*
         * Lista solamente asignaciones activas.
         *
         * GET: api/AsignacionVehiculo/Activas
         */
        [HttpGet("Activas")]
        public async Task<ActionResult<
            IEnumerable<AsignacionVehiculoResponseDto>>> ListarActivas()
        {
            var asignaciones = await ConstruirConsultaAsignaciones()
                .Where(a => a.Estado == "Activo")
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var respuesta = asignaciones
                .Select(MapearAsignacion)
                .ToList();

            return Ok(respuesta);
        }

        /*
         * Obtiene una asignación por su identificador.
         *
         * GET: api/AsignacionVehiculo/5
         */
        [HttpGet("{id:int}")]
        public async Task<
            ActionResult<AsignacionVehiculoResponseDto>>
            ObtenerPorId(int id)
        {
            var asignacion =
                await ConstruirConsultaAsignaciones()
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (asignacion == null)
            {
                return NotFound(new
                {
                    message = "La asignación no existe."
                });
            }

            return Ok(MapearAsignacion(asignacion));
        }

        /*
         * Lista todos los usuarios asignados a un vehículo.
         *
         * Por defecto devuelve asignaciones activas e inactivas
         * para conservar el historial.
         *
         * GET: api/AsignacionVehiculo/Vehiculo/2
         */
        [HttpGet("Vehiculo/{idVehiculo:int}")]
        public async Task<ActionResult<DetalleVehiculoAsignadoDto>>
            ObtenerUsuariosPorVehiculo(int idVehiculo)
        {
            var vehiculo = await _context.Vehiculos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == idVehiculo);

            if (vehiculo == null)
            {
                return NotFound(new
                {
                    message = "El vehículo no existe."
                });
            }

            var asignaciones = await _context.Asignacion_Vehiculos
                .AsNoTracking()
                .Include(a => a.Usuario)
                .Where(a => a.id_vehiculo == idVehiculo)
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var usuariosAsignados = asignaciones
                 .Where(a => a.Estado == "Activo")
                .Select(a => new UsuarioAsignadoVehiculoDto
                {
                    IdAsignacion = a.Id,
                    IdUsuario = a.id_usuario,
                    Usuario =
                        a.Usuario?.Nombre ?? string.Empty,
                    Correo = a.Usuario?.Email,
                    FechaAsignacion = a.Fecha,
                    Estado = a.Estado
                })
                .ToList();
          
            var respuesta =
                new DetalleVehiculoAsignadoDto
                {
                    IdVehiculo = vehiculo.Id,
                    Marca = vehiculo.Marca,
                    Placa = vehiculo.Placa,
                    CantidadCarga =
                        vehiculo.Cantidad_Carga,
                    EstadoVehiculo = vehiculo.Estado,

                    CantidadUsuariosAsignados =
                        usuariosAsignados.Count(u =>
                            u.Estado == "Activo"),

                    UsuariosAsignados =
                        usuariosAsignados
                };

            return Ok(respuesta);
        }

        /*
         * Lista solamente usuarios activos de un vehículo.
         *
         * GET:
         * api/AsignacionVehiculo/Vehiculo/2/UsuariosActivos
         */
        [HttpGet(
            "Vehiculo/{idVehiculo:int}/UsuariosActivos")]
        public async Task<ActionResult<
            IEnumerable<UsuarioAsignadoVehiculoDto>>>
            ObtenerUsuariosActivosPorVehiculo(
                int idVehiculo)
        {
            var vehiculoExiste = await _context.Vehiculos
                .AsNoTracking()
                .AnyAsync(v => v.Id == idVehiculo);

            if (!vehiculoExiste)
            {
                return NotFound(new
                {
                    message = "El vehículo no existe."
                });
            }

            var asignaciones =
                await _context.Asignacion_Vehiculos
                    .AsNoTracking()
                    .Include(a => a.Usuario)
                    .Where(a =>
                        a.id_vehiculo == idVehiculo &&
                        a.Estado == "Activo")
                    .OrderBy(a => a.Usuario.Nombre)
                    .ToListAsync();

            var respuesta = asignaciones
                .Select(a =>
                    new UsuarioAsignadoVehiculoDto
                    {
                        IdAsignacion = a.Id,
                        IdUsuario = a.id_usuario,
                        Usuario =
                            a.Usuario?.Nombre ??
                            string.Empty,
                        Correo = a.Usuario?.Email,
                        FechaAsignacion = a.Fecha,
                        Estado = a.Estado
                    })
                .ToList();

            return Ok(respuesta);
        }

        /*
         * Crea una asignación.
         *
         * La fecha y el estado se generan automáticamente.
         *
         * POST: api/AsignacionVehiculo/Agregar
         */
        [HttpPost("Agregar")]
        public async Task<ActionResult> Agregar(
            [FromBody]
            AsignacionVehiculoCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var error = await ValidarAsignacionAsync(
                dto.IdUsuario,
                dto.IdVehiculo);

            if (error != null)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            var asignacion =
                new Asignacion_Vehiculo
                {
                    id_usuario = dto.IdUsuario,
                    id_vehiculo = dto.IdVehiculo,

                    // Se genera automáticamente.
                    Fecha = DateTime.UtcNow,

                    // Toda asignación nueva inicia activa.
                    Estado = "Activo"
                };

            await _context.Asignacion_Vehiculos
                .AddAsync(asignacion);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Usuario asignado al vehículo correctamente.",

                asignacion = new
                {
                    id = asignacion.Id,
                    idUsuario =
                        asignacion.id_usuario,
                    idVehiculo =
                        asignacion.id_vehiculo,
                    fecha = asignacion.Fecha,
                    estado = asignacion.Estado
                }
            });
        }

        /*
         * Edita el usuario o vehículo de una asignación.
         *
         * Solo permite editar asignaciones activas.
         *
         * PUT: api/AsignacionVehiculo/5
         */
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Actualizar(
            int id,
            [FromBody]
            AsignacionVehiculoUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var asignacion =
                await _context.Asignacion_Vehiculos
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (asignacion == null)
            {
                return NotFound(new
                {
                    message = "La asignación no existe."
                });
            }

            if (asignacion.Estado != "Activo")
            {
                return BadRequest(new
                {
                    message =
                        "Solo se pueden editar asignaciones activas."
                });
            }

            var error = await ValidarAsignacionAsync(
                dto.IdUsuario,
                dto.IdVehiculo,
                id);

            if (error != null)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            asignacion.id_usuario = dto.IdUsuario;
            asignacion.id_vehiculo = dto.IdVehiculo;

            /*
             * No modificamos la fecha porque representa
             * cuándo fue creada la asignación.
             */

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Asignación actualizada correctamente."
            });
        }

        /*
         * Quita un usuario del vehículo.
         *
         * No elimina el registro; lo cambia a Inactivo
         * para conservar el historial.
         *
         * PATCH:
         * api/AsignacionVehiculo/5/QuitarUsuario
         */
        [HttpPatch("{id:int}/QuitarUsuario")]
        public async Task<ActionResult> QuitarUsuario(
            int id)
        {
            var asignacion =
                await _context.Asignacion_Vehiculos
                    .Include(a => a.Usuario)
                    .Include(a => a.Vehiculo)
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (asignacion == null)
            {
                return NotFound(new
                {
                    message = "La asignación no existe."
                });
            }

            if (asignacion.Estado == "Inactivo")
            {
                return BadRequest(new
                {
                    message =
                        "El usuario ya fue retirado del vehículo."
                });
            }

            asignacion.Estado = "Inactivo";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    $"{asignacion.Usuario?.Nombre ?? "El usuario"} " +
                    $"fue retirado del vehículo " +
                    $"{asignacion.Vehiculo?.Marca ?? ""} correctamente.",

                idAsignacion = asignacion.Id,
                estado = asignacion.Estado
            });
        }

        /*
         * Activa o inactiva una asignación.
         *
         * PATCH: api/AsignacionVehiculo/5/estado
         */
        [HttpPatch("{id:int}/estado")]
        public async Task<ActionResult> CambiarEstado(
            int id,
            [FromBody]
            CambiarEstadoAsignacionVehiculoDto dto)
        {
            var estado = NormalizarEstado(dto.Estado);

            if (estado == null)
            {
                return BadRequest(new
                {
                    message =
                        "El estado solamente puede ser Activo o Inactivo."
                });
            }

            var asignacion =
                await _context.Asignacion_Vehiculos
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (asignacion == null)
            {
                return NotFound(new
                {
                    message = "La asignación no existe."
                });
            }

            if (asignacion.Estado == estado)
            {
                return BadRequest(new
                {
                    message =
                        $"La asignación ya se encuentra {estado}."
                });
            }

            /*
             * Si se quiere reactivar, volvemos a verificar
             * que el usuario no esté asignado actualmente
             * a otro vehículo.
             */
            if (estado == "Activo")
            {
                var error =
                    await ValidarAsignacionAsync(
                        asignacion.id_usuario,
                        asignacion.id_vehiculo,
                        asignacion.Id);

                if (error != null)
                {
                    return BadRequest(new
                    {
                        message = error
                    });
                }
            }

            asignacion.Estado = estado;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = estado == "Activo"
                    ? "Asignación activada correctamente."
                    : "Asignación desactivada correctamente.",

                estado = asignacion.Estado
            });
        }

        private IQueryable<Asignacion_Vehiculo>
            ConstruirConsultaAsignaciones()
        {
            return _context.Asignacion_Vehiculos
                .AsNoTracking()
                .Include(a => a.Usuario)
                .Include(a => a.Vehiculo);
        }

        private static AsignacionVehiculoResponseDto
            MapearAsignacion(
                Asignacion_Vehiculo asignacion)
        {
            return new AsignacionVehiculoResponseDto
            {
                Id = asignacion.Id,

                IdUsuario = asignacion.id_usuario,

                Usuario =
                    asignacion.Usuario?.Nombre ??
                    string.Empty,

                CorreoUsuario =
                    asignacion.Usuario?.Email,

                IdVehiculo =
                    asignacion.id_vehiculo,

                Vehiculo =
                    asignacion.Vehiculo?.Marca ??
                    string.Empty,

                Placa =
                    asignacion.Vehiculo?.Placa,

                CantidadCarga =
                    asignacion.Vehiculo
                        ?.Cantidad_Carga ??
                    string.Empty,

                Fecha = asignacion.Fecha,

                Estado = asignacion.Estado
            };
        }

        /*
         * Regla:
         *
         * Un usuario solo puede estar asignado a
         * un vehículo activo.
         *
         * Un vehículo sí puede tener varios
         * usuarios activos.
         */
        private async Task<string?>
            ValidarAsignacionAsync(
                int idUsuario,
                int idVehiculo,
                int? idAsignacionExcluir = null)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Id == idUsuario);

            if (usuario == null)
            {
                return
                    "El usuario seleccionado no existe.";
            }

            if (!string.Equals(
                    usuario.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "El usuario seleccionado se encuentra inactivo.";
            }

            var vehiculo = await _context.Vehiculos
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    v => v.Id == idVehiculo);

            if (vehiculo == null)
            {
                return
                    "El vehículo seleccionado no existe.";
            }

            if (!string.Equals(
                    vehiculo.Estado,
                    "Activo",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "El vehículo seleccionado se encuentra inactivo.";
            }

            /*
             * Esta validación permanece:
             * un usuario no puede tener dos vehículos activos.
             */
            var usuarioYaAsignado =
                await _context.Asignacion_Vehiculos
                    .AnyAsync(a =>
                        a.id_usuario == idUsuario &&
                        a.Estado == "Activo" &&
                        (
                            !idAsignacionExcluir.HasValue ||
                            a.Id !=
                            idAsignacionExcluir.Value
                        ));

            if (usuarioYaAsignado)
            {
                return
                    "El usuario ya está asignado a otro vehículo activo. " +
                    "Primero debes quitarlo de su asignación actual.";
            }

            /*
             * No validamos si el vehículo ya tiene usuarios.
             *
             * Un vehículo puede tener varios usuarios activos.
             */

            return null;
        }

        private static string? NormalizarEstado(
            string? estado)
        {
            return estado?.Trim().ToLowerInvariant()
                switch
            {
                "activo" => "Activo",
                "inactivo" => "Inactivo",
                _ => null
            };
        }
    }
}