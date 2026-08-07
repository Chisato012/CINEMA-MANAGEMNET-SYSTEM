using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddHallNumberToShowtimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HallNumber",
                table: "Showtimes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HallNumber",
                table: "Showtimes");
        }
    }
}
