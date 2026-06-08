using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KitaKo.Migrations
{
    /// <inheritdoc />
    [Migration("20260604000000_AddUserFinancialSettingsAndOwnershipIndexes")]
    public partial class AddUserFinancialSettingsAndOwnershipIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "Expenses" WHERE "UserId" NOT IN (SELECT "Id" FROM "Users");
                DELETE FROM "Sales" WHERE "UserId" NOT IN (SELECT "Id" FROM "Users");
                DELETE FROM "Utangs" WHERE "UserId" NOT IN (SELECT "Id" FROM "Users");
                """);

            migrationBuilder.CreateTable(
                name: "UserFinancialSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AvailableBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    DailySalesGoal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 1000m),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFinancialSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFinancialSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId_DueDate",
                table: "Expenses",
                columns: new[] { "UserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId_Paid",
                table: "Expenses",
                columns: new[] { "UserId", "Paid" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UserId_Date",
                table: "Sales",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Utangs_UserId_DueDate",
                table: "Utangs",
                columns: new[] { "UserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Utangs_UserId_Paid",
                table: "Utangs",
                columns: new[] { "UserId", "Paid" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFinancialSettings_UserId",
                table: "UserFinancialSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_UserId",
                table: "Expenses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_UserId",
                table: "Sales",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Utangs_Users_UserId",
                table: "Utangs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_UserId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_UserId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Utangs_Users_UserId",
                table: "Utangs");

            migrationBuilder.DropTable(
                name: "UserFinancialSettings");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_UserId_DueDate",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_UserId_Paid",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Sales_UserId_Date",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Utangs_UserId_DueDate",
                table: "Utangs");

            migrationBuilder.DropIndex(
                name: "IX_Utangs_UserId_Paid",
                table: "Utangs");
        }
    }
}
