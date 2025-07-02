using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceRecordings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VoiceRecordingId",
                table: "Campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VoiceRecordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceRecordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceRecordings_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_VoiceRecordingId",
                table: "Campaigns",
                column: "VoiceRecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRecordings_CreatedById",
                table: "VoiceRecordings",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_VoiceRecordings_VoiceRecordingId",
                table: "Campaigns",
                column: "VoiceRecordingId",
                principalTable: "VoiceRecordings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_VoiceRecordings_VoiceRecordingId",
                table: "Campaigns");

            migrationBuilder.DropTable(
                name: "VoiceRecordings");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_VoiceRecordingId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "VoiceRecordingId",
                table: "Campaigns");
        }
    }
}
