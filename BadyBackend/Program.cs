using BadyBackend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

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

var connectionString =
    builder.Configuration.GetConnectionString("Connection")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'Connection'.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

// =====================
// PIPELINE
// =====================

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

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

var imagenesPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "imagenes");
if (!Directory.Exists(imagenesPath))
{
    Directory.CreateDirectory(imagenesPath);
}

app.UseStaticFiles();

app.MapControllers();

app.Run();