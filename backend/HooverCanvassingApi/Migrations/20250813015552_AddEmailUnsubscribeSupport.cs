using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailUnsubscribeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailOptOut",
                table: "Voters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailOptOutDate",
                table: "Voters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallToActionText",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallToActionUrl",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportantDates",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowSocialLinks",
                table: "Campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwitterUrl",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailUnsubscribes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    VoterId = table.Column<string>(type: "text", nullable: true),
                    UnsubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnsubscribeToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CampaignId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailUnsubscribes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailUnsubscribes_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailUnsubscribes_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "LalVoterId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailUnsubscribes_CampaignId",
                table: "EmailUnsubscribes",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailUnsubscribes_Email",
                table: "EmailUnsubscribes",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EmailUnsubscribes_UnsubscribedAt",
                table: "EmailUnsubscribes",
                column: "UnsubscribedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailUnsubscribes_UnsubscribeToken",
                table: "EmailUnsubscribes",
                column: "UnsubscribeToken");

            migrationBuilder.CreateIndex(
                name: "IX_EmailUnsubscribes_VoterId",
                table: "EmailUnsubscribes",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailUnsubscribes");

            migrationBuilder.DropColumn(
                name: "EmailOptOut",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "EmailOptOutDate",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "CallToActionText",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "CallToActionUrl",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ImportantDates",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "ShowSocialLinks",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "TwitterUrl",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Campaigns");
        }
    }
}
