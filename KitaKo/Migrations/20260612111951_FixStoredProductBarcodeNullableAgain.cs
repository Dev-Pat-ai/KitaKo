using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitaKo.Migrations
{
    /// <inheritdoc />
    public partial class FixStoredProductBarcodeNullableAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "StoredProducts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "StoredProducts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldNullable: true);
        }
    }
}
