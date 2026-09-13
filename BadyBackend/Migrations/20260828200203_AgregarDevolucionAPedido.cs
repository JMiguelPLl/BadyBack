using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BadyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDevolucionAPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha_Devolucion",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motivo_Devolucion",
                table: "Pedidos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fecha_Devolucion",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Motivo_Devolucion",
                table: "Pedidos");
        }
    }
}
