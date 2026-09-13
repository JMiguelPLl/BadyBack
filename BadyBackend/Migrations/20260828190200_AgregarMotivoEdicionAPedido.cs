using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BadyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMotivoEdicionAPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Motivo_Edicion",
                table: "Pedidos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Motivo_Edicion",
                table: "Pedidos");
        }
    }
}
