using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsConsentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmsConsentStatus",
                table: "Voters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SmsOptInAt",
                table: "Voters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsOptInMethod",
                table: "Voters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsOptInSource",
                table: "Voters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SmsOptOutAt",
                table: "Voters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VoterId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RawMessage = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FormUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ConsentLanguageShown = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentLanguage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentRecords_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "LalVoterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Voters_SmsConsentStatus",
                table: "Voters",
                column: "SmsConsentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_Action",
                table: "ConsentRecords",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_Timestamp",
                table: "ConsentRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecords_VoterId",
                table: "ConsentRecords",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentRecords");

            migrationBuilder.DropIndex(
                name: "IX_Voters_SmsConsentStatus",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "SmsConsentStatus",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "SmsOptInAt",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "SmsOptInMethod",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "SmsOptInSource",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "SmsOptOutAt",
                table: "Voters");
        }
    }
}
