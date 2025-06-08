using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVoterSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VoterSupport",
                table: "Voters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoterSupport",
                table: "Contacts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoterSupport",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "VoterSupport",
                table: "Contacts");
        }
    }
}
