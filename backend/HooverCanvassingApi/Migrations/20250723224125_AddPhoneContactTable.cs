using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneContactTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhoneContacts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    VoterId = table.Column<string>(type: "text", nullable: false),
                    VolunteerId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VoterSupport = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AudioFileUrl = table.Column<string>(type: "text", nullable: true),
                    AudioDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CallDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    PhoneNumberUsed = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneContacts_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhoneContacts_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "LalVoterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_Timestamp",
                table: "PhoneContacts",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_VolunteerId",
                table: "PhoneContacts",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_VoterId",
                table: "PhoneContacts",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneContacts");
        }
    }
}
