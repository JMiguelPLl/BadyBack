using BadyBackend.DTOs;
using BadyBackend.DTOs.BadyBackend.DTOs;
using BadyBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email.Trim().ToLower();

            /*
             * Primero intenta iniciar sesión como usuario interno.
             */
            var usuario = await (
                from u in _context.Usuarios.AsNoTracking()

                join ur in _context.Usuario_Rols.AsNoTracking()
                    on u.Id equals ur.id_usuario

                join r in _context.Rols.AsNoTracking()
                    on ur.id_rol equals r.Id

                where u.Email.ToLower() == email
                      && u.Estado == "Activo"
                      && ur.Estado == "Activo"
                      && r.Estado == "Activo"

                select new
                {
                    u.Id,
                    u.Nombre,
                    u.Email,
                    u.Contraseña,
                    Rol = r.Descripcion
                }
            ).FirstOrDefaultAsync();

            if (usuario != null)
            {
                if (!BadyBackend.Helpers.PasswordHelper.VerifyPassword(dto.Contrasena, usuario.Contraseña))
                {
                    return Unauthorized(new
                    {
                        message = "Correo o contraseña incorrectos."
                    });
                }

                // Si la contraseña en BD estaba en texto plano, la actualizamos automáticamente a hash BCrypt
                if (!BadyBackend.Helpers.PasswordHelper.IsHashed(usuario.Contraseña))
                {
                    var uDb = await _context.Usuarios.FindAsync(usuario.Id);
                    if (uDb != null)
                    {
                        uDb.Contraseña = BadyBackend.Helpers.PasswordHelper.HashPassword(dto.Contrasena);
                        await _context.SaveChangesAsync();
                    }
                }

                return GenerarRespuestaLogin(
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    "Usuario"
                );
            }

            /*
             * Si no se encontró como usuario, intenta como cliente.
             */
            var cliente = await _context.Clientes
                .AsNoTracking()
                .Where(c =>
                    c.Email.ToLower() == email &&
                    c.Estado == "Activo"
                )
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Email,
                    c.Contraseña
                })
                .FirstOrDefaultAsync();

            if (cliente != null)
            {
                if (!BadyBackend.Helpers.PasswordHelper.VerifyPassword(dto.Contrasena, cliente.Contraseña))
                {
                    return Unauthorized(new
                    {
                        message = "Correo o contraseña incorrectos."
                    });
                }

                // Si la contraseña en BD estaba en texto plano, la actualizamos automáticamente a hash BCrypt
                if (!BadyBackend.Helpers.PasswordHelper.IsHashed(cliente.Contraseña))
                {
                    var cDb = await _context.Clientes.FindAsync(cliente.Id);
                    if (cDb != null)
                    {
                        cDb.Contraseña = BadyBackend.Helpers.PasswordHelper.HashPassword(dto.Contrasena);
                        await _context.SaveChangesAsync();
                    }
                }

                return GenerarRespuestaLogin(
                    cliente.Id,
                    cliente.Nombre,
                    cliente.Email,
                    "Cliente",
                    "Cliente"
                );
            }

            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }

        private IActionResult GenerarRespuestaLogin(
            int id,
            string nombre,
            string email,
            string rol,
            string tipoCuenta)
        {
            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                return StatusCode(500, new
                {
                    message = "La clave JWT no está configurada."
                });
            }

            var expiracion = DateTime.UtcNow.AddHours(8);

            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    id.ToString()
                ),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    id.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    nombre
                ),

                new Claim(
                    ClaimTypes.Email,
                    email
                ),

                new Claim(
                    ClaimTypes.Role,
                    rol
                ),

                new Claim(
                    "tipoCuenta",
                    tipoCuenta
                ),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()
                )
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var credenciales = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiracion,
                signingCredentials: credenciales
            );

            var tokenGenerado = new JwtSecurityTokenHandler()
                .WriteToken(token);

            var respuesta = new LoginResponseDto
            {
                Token = tokenGenerado,
                Expiracion = expiracion,
                Id = id,
                Nombre = nombre,
                Email = email,
                Rol = rol,
                TipoCuenta = tipoCuenta
            };

            return Ok(new
            {
                message = "Inicio de sesión correcto.",
                data = respuesta
            });
        }
    }
}