using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReligionAndIncomeToVoter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Income",
                table: "Voters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Religion",
                table: "Voters",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Income",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "Religion",
                table: "Voters");
        }
    }
}
