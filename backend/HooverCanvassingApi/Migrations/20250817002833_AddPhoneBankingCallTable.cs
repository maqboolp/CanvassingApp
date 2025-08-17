using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneBankingCallTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhoneBankingCalls",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    VoterId = table.Column<string>(type: "text", nullable: false),
                    VolunteerPhone = table.Column<string>(type: "text", nullable: false),
                    VoterPhone = table.Column<string>(type: "text", nullable: false),
                    TwimlContent = table.Column<string>(type: "text", nullable: false),
                    TwilioCallSid = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneBankingCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneBankingCalls_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhoneBankingCalls_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "LalVoterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBankingCalls_UserId",
                table: "PhoneBankingCalls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneBankingCalls_VoterId",
                table: "PhoneBankingCalls",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneBankingCalls");
        }
    }
}
