using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BadyBackend.Migrations
{
    /// <inheritdoc />
    public partial class migrations23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Marca = table.Column<string>(type: "text", nullable: false),
                    Placa = table.Column<string>(type: "text", nullable: true),
                    Cantidad_Carga = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Asignacion_Vehiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    id_vehiculo = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignacion_Vehiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignacion_Vehiculos_Usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asignacion_Vehiculos_Vehiculos_id_vehiculo",
                        column: x => x.id_vehiculo,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asignacion_Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_asignacion_vehiculo = table.Column<int>(type: "integer", nullable: false),
                    id_pedido = table.Column<int>(type: "integer", nullable: false),
                    Fecha_Asignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fecha_Entrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignacion_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignacion_Pedidos_Asignacion_Vehiculos_id_asignacion_vehic~",
                        column: x => x.id_asignacion_vehiculo,
                        principalTable: "Asignacion_Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asignacion_Pedidos_Pedidos_id_pedido",
                        column: x => x.id_pedido,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Pedidos_id_asignacion_vehiculo",
                table: "Asignacion_Pedidos",
                column: "id_asignacion_vehiculo");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Pedidos_id_pedido",
                table: "Asignacion_Pedidos",
                column: "id_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Vehiculos_id_usuario",
                table: "Asignacion_Vehiculos",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Vehiculos_id_vehiculo",
                table: "Asignacion_Vehiculos",
                column: "id_vehiculo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignacion_Pedidos");

            migrationBuilder.DropTable(
                name: "Asignacion_Vehiculos");

            migrationBuilder.DropTable(
                name: "Vehiculos");
        }
    }
}
