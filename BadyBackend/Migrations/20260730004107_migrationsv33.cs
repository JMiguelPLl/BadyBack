using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BadyBackend.Migrations
{
    public partial class migrationsv33 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Pagos"
                ALTER COLUMN "SaldoPendiente"
                TYPE numeric(10,2)
                USING (
                    CASE
                        WHEN "SaldoPendiente" IS NULL
                             OR BTRIM("SaldoPendiente") = ''
                            THEN 0.00

                        WHEN REPLACE(
                                BTRIM("SaldoPendiente"),
                                ',',
                                '.'
                             )
                             ~ '^[+-]?[0-9]+([.][0-9]+)?$'
                            THEN REPLACE(
                                BTRIM("SaldoPendiente"),
                                ',',
                                '.'
                            )::numeric(10,2)

                        ELSE 0.00
                    END
                );
                """
            );

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoPagado",
                table: "Pagos",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Pagos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Pagos"
                ALTER COLUMN "SaldoPendiente"
                TYPE text
                USING "SaldoPendiente"::text;
                """
            );

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoPagado",
                table: "Pagos",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Pagos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}