using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitaKo.Migrations
{
    /// <inheritdoc />
    public partial class AddCostPriceToStoredProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "StoredProducts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "StoredProducts");
        }
    }
}
