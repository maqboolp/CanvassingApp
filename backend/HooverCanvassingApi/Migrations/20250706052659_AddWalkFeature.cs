using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddWalkFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalkSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VolunteerId = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    HousesVisited = table.Column<int>(type: "integer", nullable: false),
                    VotersContacted = table.Column<int>(type: "integer", nullable: false),
                    TotalDistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartLatitude = table.Column<double>(type: "double precision", nullable: true),
                    StartLongitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalkSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalkSessions_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WalkSessionId = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VisitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VotersContacted = table.Column<int>(type: "integer", nullable: false),
                    VotersHome = table.Column<int>(type: "integer", nullable: false),
                    ContactIds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseClaims_WalkSessions_WalkSessionId",
                        column: x => x.WalkSessionId,
                        principalTable: "WalkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalkActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WalkSessionId = table.Column<int>(type: "integer", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    HouseClaimId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalkActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalkActivities_HouseClaims_HouseClaimId",
                        column: x => x.HouseClaimId,
                        principalTable: "HouseClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WalkActivities_WalkSessions_WalkSessionId",
                        column: x => x.WalkSessionId,
                        principalTable: "WalkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseClaims_Address",
                table: "HouseClaims",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_HouseClaims_ExpiresAt",
                table: "HouseClaims",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_HouseClaims_Latitude_Longitude",
                table: "HouseClaims",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseClaims_Status",
                table: "HouseClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HouseClaims_WalkSessionId",
                table: "HouseClaims",
                column: "WalkSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WalkActivities_ActivityType",
                table: "WalkActivities",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_WalkActivities_HouseClaimId",
                table: "WalkActivities",
                column: "HouseClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WalkActivities_Timestamp",
                table: "WalkActivities",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WalkActivities_WalkSessionId",
                table: "WalkActivities",
                column: "WalkSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WalkSessions_StartedAt",
                table: "WalkSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalkSessions_Status",
                table: "WalkSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WalkSessions_VolunteerId",
                table: "WalkSessions",
                column: "VolunteerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalkActivities");

            migrationBuilder.DropTable(
                name: "HouseClaims");

            migrationBuilder.DropTable(
                name: "WalkSessions");
        }
    }
}
