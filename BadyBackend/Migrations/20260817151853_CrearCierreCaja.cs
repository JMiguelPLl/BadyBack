using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BadyBackend.Migrations
{
    /// <inheritdoc />
    public partial class CrearCierreCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cierre_Cajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_usuario = table.Column<int>(type: "integer", nullable: false),
                    Fecha_Apertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fecha_Cierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Total_Efectivo = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Total_QR = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Total_Recaudado = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cierre_Cajas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cierre_Cajas_Usuarios_Id_usuario",
                        column: x => x.Id_usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cierre_Caja_Detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_cierre_caja = table.Column<int>(type: "integer", nullable: false),
                    Id_pago = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cierre_Caja_Detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cierre_Caja_Detalles_Cierre_Cajas_Id_cierre_caja",
                        column: x => x.Id_cierre_caja,
                        principalTable: "Cierre_Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cierre_Caja_Detalles_Pagos_Id_pago",
                        column: x => x.Id_pago,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cierre_Caja_Detalles_Id_cierre_caja",
                table: "Cierre_Caja_Detalles",
                column: "Id_cierre_caja");

            migrationBuilder.CreateIndex(
                name: "IX_Cierre_Caja_Detalles_Id_pago",
                table: "Cierre_Caja_Detalles",
                column: "Id_pago");

            migrationBuilder.CreateIndex(
                name: "IX_Cierre_Cajas_Id_usuario",
                table: "Cierre_Cajas",
                column: "Id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cierre_Caja_Detalles");

            migrationBuilder.DropTable(
                name: "Cierre_Cajas");
        }
    }
}
