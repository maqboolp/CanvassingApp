using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVoterCampaignTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CallCount",
                table: "Voters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCallAt",
                table: "Voters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastCallCampaignId",
                table: "Voters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCampaignContactAt",
                table: "Voters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastCampaignId",
                table: "Voters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSmsAt",
                table: "Voters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSmsCampaignId",
                table: "Voters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmsCount",
                table: "Voters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCampaignContacts",
                table: "Voters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VoterLalVoterId",
                table: "CampaignMessages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMessages_VoterLalVoterId",
                table: "CampaignMessages",
                column: "VoterLalVoterId");

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignMessages_Voters_VoterLalVoterId",
                table: "CampaignMessages",
                column: "VoterLalVoterId",
                principalTable: "Voters",
                principalColumn: "LalVoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CampaignMessages_Voters_VoterLalVoterId",
                table: "CampaignMessages");

            migrationBuilder.DropIndex(
                name: "IX_CampaignMessages_VoterLalVoterId",
                table: "CampaignMessages");

            migrationBuilder.DropColumn(
                name: "CallCount",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "LastCallAt",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "LastCallCampaignId",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "LastCampaignContactAt",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "LastCampaignId",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "LastSmsAt",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "LastSmsCampaignId",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "SmsCount",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "TotalCampaignContacts",
                table: "Voters");

            migrationBuilder.DropColumn(
                name: "VoterLalVoterId",
                table: "CampaignMessages");
        }
    }
}
