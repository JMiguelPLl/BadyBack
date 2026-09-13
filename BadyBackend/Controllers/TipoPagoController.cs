using BadyBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TipoPagoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TipoPagoController(
            AppDbContext context)
        {
            _context = context;
        }

        /*
         * GET:
         * api/TipoPago/Activos
         */
        [HttpGet("Activos")]
        public async Task<IActionResult>
            ListarTiposPagoActivos()
        {
            var tiposPago =
                await _context.Tipo_Pagos
                    .AsNoTracking()
                    .Where(t =>
                        t.Estado == "Activo"
                    )
                    .OrderBy(t =>
                        t.Descripcion
                    )
                    .Select(t =>
                        new
                        {
                            id = t.Id,
                            descripcion =
                                t.Descripcion,
                            estado =
                                t.Estado
                        }
                    )
                    .ToListAsync();

            return Ok(tiposPago);
        }

        /*
         * GET:
         * api/TipoPago/Listar
         */
        [HttpGet("Listar")]
        public async Task<IActionResult>
            ListarTodos()
        {
            var tiposPago =
                await _context.Tipo_Pagos
                    .AsNoTracking()
                    .OrderBy(t =>
                        t.Descripcion
                    )
                    .Select(t =>
                        new
                        {
                            id = t.Id,
                            descripcion =
                                t.Descripcion,
                            estado =
                                t.Estado
                        }
                    )
                    .ToListAsync();

            return Ok(tiposPago);
        }
    }
}