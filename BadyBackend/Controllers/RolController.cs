using BadyBackend.Models;
using BadyBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BadyBackend.DTOs.RolDtos;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RolController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRol()
        {
            var resultado = await _context.Rols
                .AsNoTracking()
                .Select(r => new
                {
                    r.Id,
                    r.Descripcion,
                    r.Estado
                })
                .ToListAsync();

            return Ok(resultado);
        }

        [HttpGet("RolesActivos")]
        public async Task<IActionResult> GetRolesActivos()
        {
            var resultado = await _context.Rols
                .AsNoTracking()
                .Where(r => r.Estado == "Activo")
                .Select(r => new
                {
                    r.Id,
                    r.Descripcion,
                    r.Estado
                })
                .ToListAsync();

            return Ok(resultado);
        }

        [HttpPost("Agregar")]
        public async Task<IActionResult> CrearRol([FromBody] RolCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rol = new Rol
            {
                Descripcion = dto.Descripcion,
                Estado = "Activo"
            };

            await _context.Rols.AddAsync(rol);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Rol creado correctamente"
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarRol(
            int id,
            [FromBody] RolUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rol = await _context.Rols.FindAsync(id);

            if (rol == null)
                return NotFound("Rol no encontrado.");

            rol.Descripcion = dto.Descripcion;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Rol actualizado correctamente"
            });
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstadoRol(int id)
        {
            var rol = await _context.Rols.FindAsync(id);

            if (rol == null)
                return NotFound("Rol no encontrado.");

            rol.Estado = rol.Estado == "Activo"
                ? "Inactivo"
                : "Activo";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = rol.Estado == "Activo"
                    ? "Rol activado correctamente."
                    : "Rol desactivado correctamente.",

                estado = rol.Estado
            });
        }
    }
}