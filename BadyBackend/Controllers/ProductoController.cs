using BadyBackend.DTOs;
using BadyBackend.Models;
using BadyBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BadyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductoController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =====================================================
        // LISTAR TODOS LOS PRODUCTOS
        // =====================================================
        /*
         * Lista todos los productos, tanto activos como inactivos,
         * incluyendo el nombre y enlace completo de la imagen.
         *
         * GET: api/Producto/Listar
         */
        [HttpGet("Listar")]
        public async Task<IActionResult> ListarProductos()
        {
            var productos = await _context.Productos
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            var respuesta = productos
                .Select(MapearProducto)
                .ToList();

            return Ok(respuesta);
        }

        // =====================================================
        // LISTAR PRODUCTOS DISPONIBLES
        // =====================================================
        /*
         * Lista solamente productos activos y con stock mayor a 1,
         * incluyendo el enlace de la imagen.
         *
         * GET: api/Producto/Disponibles
         */
        [HttpGet("Disponibles")]
        public async Task<IActionResult> ListarProductosDisponibles()
        {
            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p =>
                    p.Estado == "Activo" &&
                    p.Stock > 1
                )
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            var respuesta = productos
                .Select(MapearProducto)
                .ToList();

            return Ok(respuesta);
        }

        // =====================================================
        // OBTENER PRODUCTO POR ID
        // =====================================================
        /*
         * Obtiene un producto por ID con el enlace de su imagen.
         *
         * GET: api/Producto/5
         */
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerProductoPorId(int id)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return NotFound(new
                {
                    message = "Producto no encontrado."
                });
            }

            return Ok(MapearProducto(producto));
        }

        // =====================================================
        // AGREGAR PRODUCTO (CON O SIN IMAGEN)
        // =====================================================
        /*
         * Agrega un producto nuevo. Puede recibir imagen vía multipart/form-data.
         * El producto se registra inicialmente como Activo.
         *
         * POST: api/Producto
         */
        [HttpPost]
        [Consumes("multipart/form-data", "application/json")]
        public async Task<IActionResult> AgregarProducto(
            [FromForm] ProductoCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var nombreNormalizado = dto.Nombre.Trim();

            var productoExiste = await _context.Productos
                .AnyAsync(p => p.Nombre.ToLower() == nombreNormalizado.ToLower());

            if (productoExiste)
            {
                return Conflict(new
                {
                    message = "Ya existe un producto con ese nombre."
                });
            }

            string? nombreImagen = null;
            if (dto.Imagen != null && dto.Imagen.Length > 0)
            {
                try
                {
                    nombreImagen = await GuardarImagenAsync(dto.Imagen);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new
                    {
                        message = ex.Message
                    });
                }
            }

            var producto = new Producto
            {
                Nombre = nombreNormalizado,
                Descripcion = dto.Descripcion.Trim(),
                Stock = dto.Stock,
                Precio = dto.Precio,
                Estado = "Activo",
                Imagen = nombreImagen
            };

            await _context.Productos.AddAsync(producto);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Producto registrado correctamente.",
                producto = MapearProducto(producto)
            });
        }

        // =====================================================
        // EDITAR PRODUCTO (CON O SIN IMAGEN)
        // =====================================================
        /*
         * Edita los datos principales del producto y permite actualizar o eliminar la imagen.
         *
         * PUT: api/Producto/5
         */
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data", "application/json")]
        public async Task<IActionResult> EditarProducto(
            int id,
            [FromForm] ProductoUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return NotFound(new
                {
                    message = "Producto no encontrado."
                });
            }

            var nombreNormalizado = dto.Nombre.Trim();

            var productoExiste = await _context.Productos
                .AnyAsync(p =>
                    p.Nombre.ToLower() == nombreNormalizado.ToLower() &&
                    p.Id != id
                );

            if (productoExiste)
            {
                return Conflict(new
                {
                    message = "Ya existe otro producto con ese nombre."
                });
            }

            producto.Nombre = nombreNormalizado;
            producto.Descripcion = dto.Descripcion.Trim();
            producto.Stock = dto.Stock;
            producto.Precio = dto.Precio;

            if (dto.EliminarImagen)
            {
                EliminarImagenFisica(producto.Imagen);
                producto.Imagen = null;
            }
            else if (dto.Imagen != null && dto.Imagen.Length > 0)
            {
                try
                {
                    // Eliminar la imagen anterior si existía
                    EliminarImagenFisica(producto.Imagen);
                    producto.Imagen = await GuardarImagenAsync(dto.Imagen);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new
                    {
                        message = ex.Message
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Producto actualizado correctamente.",
                producto = MapearProducto(producto)
            });
        }

        // =====================================================
        // SUBIR O REEMPLAZAR IMAGEN DEDICADO
        // =====================================================
        /*
         * Permite subir o actualizar exclusivamente la imagen de un producto existente.
         *
         * POST: api/Producto/5/imagen
         */
        [HttpPost("{id:int}/imagen")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirImagen(
            int id,
            [FromForm] SubirImagenProductoDto dto)
        {
            if (dto.Archivo == null || dto.Archivo.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Debe enviar un archivo de imagen válido."
                });
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return NotFound(new
                {
                    message = "Producto no encontrado."
                });
            }

            try
            {
                EliminarImagenFisica(producto.Imagen);
                producto.Imagen = await GuardarImagenAsync(dto.Archivo);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Imagen subida correctamente.",
                    producto = MapearProducto(producto)
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // ELIMINAR IMAGEN DEDICADO
        // =====================================================
        /*
         * Elimina la imagen asignada a un producto y borra el archivo físico.
         *
         * DELETE: api/Producto/5/imagen
         */
        [HttpDelete("{id:int}/imagen")]
        public async Task<IActionResult> EliminarImagen(int id)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return NotFound(new
                {
                    message = "Producto no encontrado."
                });
            }

            if (!string.IsNullOrWhiteSpace(producto.Imagen))
            {
                EliminarImagenFisica(producto.Imagen);
                producto.Imagen = null;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Imagen eliminada correctamente.",
                producto = MapearProducto(producto)
            });
        }

        // =====================================================
        // CAMBIAR ESTADO DEL PRODUCTO
        // =====================================================
        /*
         * Cambia el estado del producto entre Activo e Inactivo.
         *
         * PATCH: api/Producto/5/estado
         */
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstadoProducto(
            int id,
            [FromBody] CambiarEstadoProductoDto dto)
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

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return NotFound(new
                {
                    message = "Producto no encontrado."
                });
            }

            producto.Estado = estado;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = estado == "Activo"
                    ? "Producto activado correctamente."
                    : "Producto desactivado correctamente.",
                estado = producto.Estado
            });
        }

        // =====================================================
        // MÉTODOS PRIVADOS AUXILIARES PARA IMÁGENES
        // =====================================================

        private async Task<string> GuardarImagenAsync(IFormFile archivo)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            if (!extensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Formato de imagen no válido. Formatos soportados: JPG, JPEG, PNG, WEBP, GIF.");
            }

            if (archivo.Length > 10 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "El tamaño de la imagen no puede superar los 10 MB.");
            }

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var carpetaImagenes = Path.Combine(webRoot, "imagenes");

            if (!Directory.Exists(carpetaImagenes))
            {
                Directory.CreateDirectory(carpetaImagenes);
            }

            var nombreArchivo = $"producto_{Guid.NewGuid():N}{extension}";
            var rutaCompleta = Path.Combine(carpetaImagenes, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return nombreArchivo;
        }

        private void EliminarImagenFisica(string? nombreImagen)
        {
            if (string.IsNullOrWhiteSpace(nombreImagen)) return;

            try
            {
                var archivo = Path.GetFileName(nombreImagen);
                var webRoot = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }

                var rutaCompleta = Path.Combine(webRoot, "imagenes", archivo);

                if (System.IO.File.Exists(rutaCompleta))
                {
                    System.IO.File.Delete(rutaCompleta);
                }
            }
            catch
            {
                // No interrumpir la operación si el archivo ya no existe o está ocupado
            }
        }

        private string? ConstruirUrlImagen(string? nombreImagen)
        {
            if (string.IsNullOrWhiteSpace(nombreImagen))
                return null;

            if (nombreImagen.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                nombreImagen.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return nombreImagen;
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var path = nombreImagen.StartsWith("/") ? nombreImagen : $"/imagenes/{nombreImagen}";
            return $"{baseUrl}{path}";
        }

        private ProductoResponseDto MapearProducto(Producto p)
        {
            return new ProductoResponseDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Stock = p.Stock,
                Precio = p.Precio,
                Estado = p.Estado,
                Imagen = p.Imagen,
                ImagenUrl = ConstruirUrlImagen(p.Imagen)
            };
        }
    }
}