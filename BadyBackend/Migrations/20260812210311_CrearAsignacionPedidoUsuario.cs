using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BadyBackend.Migrations
{
    /// <inheritdoc />
    public partial class CrearAsignacionPedidoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Asignacion_Pedido_Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_asignacion_pedido = table.Column<int>(type: "integer", nullable: false),
                    Id_asignacion_vehiculo = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignacion_Pedido_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignacion_Pedido_Usuarios_Asignacion_Pedidos_Id_asignacion~",
                        column: x => x.Id_asignacion_pedido,
                        principalTable: "Asignacion_Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asignacion_Pedido_Usuarios_Asignacion_Vehiculos_Id_asignaci~",
                        column: x => x.Id_asignacion_vehiculo,
                        principalTable: "Asignacion_Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Pedido_Usuarios_Id_asignacion_pedido",
                table: "Asignacion_Pedido_Usuarios",
                column: "Id_asignacion_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_Pedido_Usuarios_Id_asignacion_vehiculo",
                table: "Asignacion_Pedido_Usuarios",
                column: "Id_asignacion_vehiculo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignacion_Pedido_Usuarios");
        }
    }
}
