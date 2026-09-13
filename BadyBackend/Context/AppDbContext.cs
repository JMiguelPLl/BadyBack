using BadyBackend.Models;
using BadyBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;

namespace BadyBackend.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IHttpContextAccessor httpContextAccessor
        ) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Rols { get; set; }
        public DbSet<Usuario_rol> Usuario_Rols { get; set; }
        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Sucursal> Sucursales { get; set; }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Detalle_Pedido> Detalle_Pedidos { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Tipo_Pago> Tipo_Pagos { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Asignacion_Vehiculo> Asignacion_Vehiculos { get; set; }
        public DbSet<Asignacion_Pedido> Asignacion_Pedidos { get; set; }
        public DbSet<Asignacion_Pedido_Usuario>
        Asignacion_Pedido_Usuarios
        { get; set; }

        public DbSet<Cierre_Caja> Cierre_Cajas { get; set; }
        public DbSet<Cierre_Caja_Detalle> Cierre_Caja_Detalles { get; set; }
    }

}
