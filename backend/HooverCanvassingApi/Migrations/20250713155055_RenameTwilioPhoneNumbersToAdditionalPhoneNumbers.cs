using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameTwilioPhoneNumbersToAdditionalPhoneNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwilioPhoneNumbers");

            migrationBuilder.CreateTable(
                name: "AdditionalPhoneNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxConcurrentCalls = table.Column<int>(type: "integer", nullable: false),
                    CurrentActiveCalls = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalCallsMade = table.Column<int>(type: "integer", nullable: false),
                    TotalCallsFailed = table.Column<int>(type: "integer", nullable: false),
                    RateLimitResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalPhoneNumbers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdditionalPhoneNumbers");

            migrationBuilder.CreateTable(
                name: "TwilioPhoneNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentActiveCalls = table.Column<int>(type: "integer", nullable: false),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxConcurrentCalls = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    RateLimitResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalCallsFailed = table.Column<int>(type: "integer", nullable: false),
                    TotalCallsMade = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwilioPhoneNumbers", x => x.Id);
                });
        }
    }
}
