using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVoterAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoterAssignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoterAssignments",
                columns: table => new
                {
                    VolunteerId = table.Column<string>(type: "text", nullable: false),
                    VoterId = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterAssignments", x => new { x.VolunteerId, x.VoterId });
                    table.ForeignKey(
                        name: "FK_VoterAssignments_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoterAssignments_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "LalVoterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoterAssignments_VolunteerId",
                table: "VoterAssignments",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VoterAssignments_VoterId",
                table: "VoterAssignments",
                column: "VoterId");
        }
    }
}
