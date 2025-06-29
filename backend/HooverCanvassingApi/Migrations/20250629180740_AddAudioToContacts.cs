using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioToContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AudioDurationSeconds",
                table: "Contacts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFileUrl",
                table: "Contacts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioDurationSeconds",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "AudioFileUrl",
                table: "Contacts");
        }
    }
}
