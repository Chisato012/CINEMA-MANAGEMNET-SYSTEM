using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddConcessionManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Combos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Combos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Combos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Combos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Combos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Combos");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Combos");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Combos");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Combos");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Combos");
        }
    }
}
