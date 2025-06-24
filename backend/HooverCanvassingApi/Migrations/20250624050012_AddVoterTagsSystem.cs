using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVoterTagsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilterTags",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VoterTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoterTags_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VoterTagAssignments",
                columns: table => new
                {
                    VoterId = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedById = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterTagAssignments", x => new { x.VoterId, x.TagId });
                    table.ForeignKey(
                        name: "FK_VoterTagAssignments_AspNetUsers_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VoterTagAssignments_VoterTags_TagId",
                        column: x => x.TagId,
                        principalTable: "VoterTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoterTagAssignments_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "LalVoterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoterTagAssignments_AssignedAt",
                table: "VoterTagAssignments",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VoterTagAssignments_AssignedById",
                table: "VoterTagAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_VoterTagAssignments_TagId",
                table: "VoterTagAssignments",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_VoterTags_CreatedById",
                table: "VoterTags",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VoterTags_TagName",
                table: "VoterTags",
                column: "TagName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoterTagAssignments");

            migrationBuilder.DropTable(
                name: "VoterTags");

            migrationBuilder.DropColumn(
                name: "FilterTags",
                table: "Campaigns");
        }
    }
}
