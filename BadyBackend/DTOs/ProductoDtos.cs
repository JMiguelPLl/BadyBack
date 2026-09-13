using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BadyBackend.DTOs
{
    public class ProductoCreateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [MaxLength(250)]
        public string Descripcion { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a cero.")]
        public decimal Precio { get; set; }

        public IFormFile? Imagen { get; set; }
    }

    public class ProductoUpdateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [MaxLength(250)]
        public string Descripcion { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a cero.")]
        public decimal Precio { get; set; }

        public IFormFile? Imagen { get; set; }

        public bool EliminarImagen { get; set; } = false;
    }

    public class ProductoResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal Precio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Imagen { get; set; }
        public string? ImagenUrl { get; set; }
    }

    public class CambiarEstadoProductoDto
    {
        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string Estado { get; set; } = string.Empty;
    }

    public class SubirImagenProductoDto
    {
        [Required(ErrorMessage = "Debe seleccionar un archivo de imagen.")]
        public IFormFile Archivo { get; set; } = null!;
    }
}