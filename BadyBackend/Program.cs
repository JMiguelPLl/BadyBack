using BadyBackend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

// Compatibilidad de fechas DateTime con PostgreSQL (Npgsql)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Permite acceso local y en la nube (Render asigna la variable de entorno PORT)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5127";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddHttpContextAccessor();

// Límite máximo de archivos: 500 MB
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524288000;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 524288000;
});

// =====================
// CORS
// =====================

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =====================
// BASE DE DATOS
// =====================

var rawConnectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Connection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? builder.Configuration.GetConnectionString("Connection")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'Connection' ni 'DATABASE_URL'.");

string connectionString;
if (rawConnectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
    rawConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    var uri = new Uri(rawConnectionString);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var portNumber = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={host};Port={portNumber};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
}
else
{
    connectionString = rawConnectionString;
    if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
    {
        connectionString += ";SSL Mode=Require;Trust Server Certificate=true;";
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// =====================
// CONTROLADORES
// =====================

builder.Services.AddControllers();

// =====================
// OPENAPI Y SCALAR
// =====================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Info.Title = "BADY'S API";
            document.Info.Version = "v1";

            return Task.CompletedTask;
        });
});

// =====================
// JWT
// =====================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "No se encontró la configuración Jwt:Key.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ========================================================
// MIDDLEWARE GLOBAL: MANEJO DE ERRORES Y CORS INCONDICIONAL
// ========================================================
app.Use(async (context, next) =>
{
    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
    context.Response.Headers["Access-Control-Allow-Headers"] = "*";

    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[ERROR FATAL CAPTURADO]: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}\n");

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "*";

            var errorResponse = new
            {
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            };
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
});

app.UseCors();
app.UseCors("AllowFrontend");

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("BADY'S API Documentation")
        .WithTheme(ScalarTheme.Moon)
        .WithDefaultHttpClient(
            ScalarTarget.CSharp,
            ScalarClient.HttpClient
        )
        .WithPreferredScheme("Bearer");
});

// Se deja comentado para permitir HTTP desde Android
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var imagenesPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "imagenes");
if (!Directory.Exists(imagenesPath))
{
    Directory.CreateDirectory(imagenesPath);
}

app.UseStaticFiles();

app.MapControllers();

// ==========================================
// APLICAR MIGRACIONES AUTOMÁTICAS Y SEED
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        Console.WriteLine(">>> Migraciones de PostgreSQL aplicadas exitosamente.");

        // 1. Roles
        if (!context.Rols.Any())
        {
            context.Rols.AddRange(
                new BadyBackend.Models.Rol { Descripcion = "Administrador", Estado = "Activo" },
                new BadyBackend.Models.Rol { Descripcion = "Distribuidor", Estado = "Activo" },
                new BadyBackend.Models.Rol { Descripcion = "Cliente", Estado = "Activo" }
            );
            context.SaveChanges();
            Console.WriteLine(">>> Roles sembrados exitosamente.");
        }

        // 2. Tipos de Pago
        if (!context.Tipo_Pagos.Any())
        {
            context.Tipo_Pagos.AddRange(
                new BadyBackend.Models.Tipo_Pago { Descripcion = "Efectivo", Estado = "Activo" },
                new BadyBackend.Models.Tipo_Pago { Descripcion = "QR", Estado = "Activo" }
            );
            context.SaveChanges();
            Console.WriteLine(">>> Tipos de pago sembrados exitosamente.");
        }

        // 3. Usuario Administrador por defecto
        if (!context.Usuarios.Any(u => u.Email.ToLower() == "admin@badys.com"))
        {
            var adminUser = new BadyBackend.Models.Usuario
            {
                Nombre = "Administrador General",
                Numero = "70000000",
                Email = "admin@badys.com",
                Contraseña = "admin123",
                Estado = "Activo"
            };
            context.Usuarios.Add(adminUser);
            context.SaveChanges();

            var adminRol = context.Rols.FirstOrDefault(r => r.Descripcion == "Administrador");
            if (adminRol != null)
            {
                context.Usuario_Rols.Add(new BadyBackend.Models.Usuario_rol
                {
                    id_rol = adminRol.Id,
                    id_usuario = adminUser.Id,
                    Estado = "Activo"
                });
                context.SaveChanges();
            }
            Console.WriteLine(">>> Usuario Administrador sembrado exitosamente.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> Error al aplicar migraciones/seed en PostgreSQL: {ex.Message}");
    }
}

app.Run();