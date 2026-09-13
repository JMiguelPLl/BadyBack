using BadyBackend.Models;
using BadyBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BadyBackend.Models.UsuarioDtos;


[Route("api/[controller]")]
[ApiController]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Usuarios/Listar
    // GET: api/Usuarios/Listar?estado=Activo
    // GET: api/Usuarios/Listar?estado=Inactivo
    [HttpGet("Listar")]
    public async Task<IActionResult> ListarUsuarios([FromQuery] string? estado)
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

        var consulta =
            from usuario in _context.Usuarios.AsNoTracking()
            join usuarioRol in _context.Usuario_Rols.AsNoTracking()
                on usuario.Id equals usuarioRol.id_usuario
            join rol in _context.Rols.AsNoTracking()
                on usuarioRol.id_rol equals rol.Id
            select new UsuarioListDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Numero = usuario.Numero,
                Email = usuario.Email,
                Estado = usuario.Estado,
                IdRol = rol.Id,
                Rol = rol.Descripcion
            };

        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(u => u.Estado == estado);
        }

        var usuarios = await consulta
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        return Ok(usuarios);
    }

    // GET: api/Usuarios/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerUsuario(int id)
    {
        var usuario = await (
            from u in _context.Usuarios.AsNoTracking()
            join usuarioRol in _context.Usuario_Rols.AsNoTracking()
                on u.Id equals usuarioRol.id_usuario
            join rol in _context.Rols.AsNoTracking()
                on usuarioRol.id_rol equals rol.Id
            where u.Id == id
            select new UsuarioListDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Numero = u.Numero,
                Email = u.Email,
                Estado = u.Estado,
                IdRol = rol.Id,
                Rol = rol.Descripcion
            }
        ).FirstOrDefaultAsync();

        if (usuario == null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado."
            });
        }

        return Ok(usuario);
    }

    // POST: api/Usuarios/Agregar
    [HttpPost("Agregar")]
    public async Task<IActionResult> CrearUsuario(
        [FromBody] UsuarioCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var email = dto.Email.Trim().ToLower();

        var emailExiste = await _context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExiste)
        {
            return Conflict(new
            {
                message = "Ya existe un usuario registrado con ese correo."
            });
        }

        var rol = await _context.Rols
            .FirstOrDefaultAsync(r => r.Id == dto.IdRol);

        if (rol == null)
        {
            return NotFound(new
            {
                message = "El rol seleccionado no existe."
            });
        }

        if (rol.Estado != "Activo")
        {
            return BadRequest(new
            {
                message = "No se puede asignar un rol inactivo."
            });
        }

        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Numero = dto.Numero.Trim(),
                Email = email,

                // Es recomendable guardar esta contraseña cifrada.
                Contraseña = dto.Contrasena,

                Estado = "Activo"
            };

            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();

            var usuarioRol = new Usuario_rol
            {
                id_usuario = usuario.Id,
                id_rol = dto.IdRol,
                Estado = "Activo"
            };

            await _context.Usuario_Rols.AddAsync(usuarioRol);
            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            return Ok(new
            {
                message = "Usuario creado correctamente.",
                usuario = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Estado,
                    IdRol = rol.Id,
                    Rol = rol.Descripcion
                }
            });
        }
        catch
        {
            await transaccion.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Ocurrió un error al registrar el usuario."
            });
        }
    }

    // PUT: api/Usuarios/Modificar/5
    [HttpPut("Modificar/{id:int}")]
    public async Task<IActionResult> ModificarUsuario(
        int id,
        [FromBody] UsuarioUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado."
            });
        }

        var email = dto.Email.Trim().ToLower();

        var emailExiste = await _context.Usuarios.AnyAsync(u =>
            u.Email.ToLower() == email &&
            u.Id != id
        );

        if (emailExiste)
        {
            return Conflict(new
            {
                message = "El correo ya está siendo utilizado por otro usuario."
            });
        }

        var rol = await _context.Rols
            .FirstOrDefaultAsync(r => r.Id == dto.IdRol);

        if (rol == null)
        {
            return NotFound(new
            {
                message = "El rol seleccionado no existe."
            });
        }

        if (rol.Estado != "Activo")
        {
            return BadRequest(new
            {
                message = "No se puede asignar un rol inactivo."
            });
        }

        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        try
        {
            usuario.Nombre = dto.Nombre.Trim();
            usuario.Numero = dto.Telefono.Trim();
            usuario.Email = email;

            if (!string.IsNullOrWhiteSpace(dto.Contrasena))
            {
                // Es recomendable guardar esta contraseña cifrada.
                usuario.Contraseña = dto.Contrasena;
            }

            var usuarioRol = await _context.Usuario_Rols
                .FirstOrDefaultAsync(ur => ur.id_usuario == id);

            if (usuarioRol == null)
            {
                usuarioRol = new Usuario_rol
                {
                    id_usuario = usuario.Id,
                    id_rol = dto.IdRol,
                    Estado = usuario.Estado
                };

                await _context.Usuario_Rols.AddAsync(usuarioRol);
            }
            else
            {
                usuarioRol.id_rol = dto.IdRol;
                usuarioRol.Estado = usuario.Estado;
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Ok(new
            {
                message = "Usuario modificado correctamente.",
                usuario = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Numero,
                    usuario.Email,
                    usuario.Estado,
                    IdRol = rol.Id,
                    Rol = rol.Descripcion
                }
            });
        }
        catch
        {
            await transaccion.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Ocurrió un error al modificar el usuario."
            });
        }
    }

    // PATCH: api/Usuarios/5/estado
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstadoUsuario(
        int id,
        [FromBody] CambiarEstadoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var nuevoEstado = NormalizarEstado(dto.Estado);

        if (nuevoEstado != "Activo" && nuevoEstado != "Inactivo")
        {
            return BadRequest(new
            {
                message = "El estado solamente puede ser Activo o Inactivo."
            });
        }

        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return NotFound(new
            {
                message = "Usuario no encontrado."
            });
        }

        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        try
        {
            usuario.Estado = nuevoEstado;

            var relacionesUsuarioRol = await _context.Usuario_Rols
                .Where(ur => ur.id_usuario == id)
                .ToListAsync();

            foreach (var usuarioRol in relacionesUsuarioRol)
            {
                usuarioRol.Estado = nuevoEstado;
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Ok(new
            {
                message = nuevoEstado == "Activo"
                    ? "Usuario activado correctamente."
                    : "Usuario desactivado correctamente.",

                estado = nuevoEstado
            });
        }
        catch
        {
            await transaccion.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Ocurrió un error al cambiar el estado del usuario."
            });
        }
    }

    private static string NormalizarEstado(string estado)
    {
        estado = estado.Trim().ToLower();

        return estado switch
        {
            "activo" => "Activo",
            "inactivo" => "Inactivo",
            _ => estado
        };
    }
}