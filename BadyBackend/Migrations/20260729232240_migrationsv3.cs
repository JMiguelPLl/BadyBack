using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BadyBackend.Migrations
{
    /// <inheritdoc />
    public partial class migrationsv3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_sucursal = table.Column<int>(type: "integer", nullable: false),
                    Id_cliente = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observacion = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Clientes_Id_cliente",
                        column: x => x.Id_cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pedidos_Sucursales_Id_sucursal",
                        column: x => x.Id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_pedido = table.Column<int>(type: "integer", nullable: false),
                    Id_producto = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Detalle_Pedidos_Pedidos_Id_pedido",
                        column: x => x.Id_pedido,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Detalle_Pedidos_Productos_Id_producto",
                        column: x => x.Id_producto,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Pedidos_Id_pedido",
                table: "Detalle_Pedidos",
                column: "Id_pedido");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Pedidos_Id_producto",
                table: "Detalle_Pedidos",
                column: "Id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Id_cliente",
                table: "Pedidos",
                column: "Id_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Id_sucursal",
                table: "Pedidos",
                column: "Id_sucursal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detalle_Pedidos");

            migrationBuilder.DropTable(
                name: "Pedidos");
        }
    }
}
