using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVoterPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add composite index for common phone banking queries
            migrationBuilder.CreateIndex(
                name: "IX_Voters_IsContacted_Zip",
                table: "Voters",
                columns: new[] { "IsContacted", "Zip" });
            
            // Add index for ZIP code queries
            migrationBuilder.CreateIndex(
                name: "IX_Voters_Zip",
                table: "Voters",
                columns: new[] { "Zip" });
            
            // Add index for name searches
            migrationBuilder.CreateIndex(
                name: "IX_Voters_LastName_FirstName",
                table: "Voters",
                columns: new[] { "LastName", "FirstName" });
            
            // Add index for contact status queries
            migrationBuilder.CreateIndex(
                name: "IX_Voters_IsContacted",
                table: "Voters",
                columns: new[] { "IsContacted" });
            
            // Add index for location-based queries
            migrationBuilder.CreateIndex(
                name: "IX_Voters_Latitude_Longitude",
                table: "Voters",
                columns: new[] { "Latitude", "Longitude" });
            
            // Add index for vote frequency
            migrationBuilder.CreateIndex(
                name: "IX_Voters_VoteFrequency",
                table: "Voters",
                columns: new[] { "VoteFrequency" });
            
            // Add index for phone number existence checks
            migrationBuilder.CreateIndex(
                name: "IX_Voters_CellPhone",
                table: "Voters",
                columns: new[] { "CellPhone" });
            
            // Add composite index for phone banking next-to-call queries
            migrationBuilder.CreateIndex(
                name: "IX_Voters_CellPhone_IsContacted_LastContactStatus",
                table: "Voters",
                columns: new[] { "CellPhone", "IsContacted", "LastContactStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voters_IsContacted_Zip",
                table: "Voters");
            
            migrationBuilder.DropIndex(
                name: "IX_Voters_Zip",
                table: "Voters");
            
            migrationBuilder.DropIndex(
                name: "IX_Voters_LastName_FirstName",
                table: "Voters");
            
            migrationBuilder.DropIndex(
                name: "IX_Voters_IsContacted",
                table: "Voters");
            
            migrationBuilder.DropIndex(
                name: "IX_Voters_Latitude_Longitude",
                table: "Voters");
            
            migrationBuilder.DropIndex(
                name: "IX_Voters_VoteFrequency",
                table: "Voters");
            
            migrationBuilder.DropIndex(
                name: "IX_Voters_CellPhone",
                table: "Voters");
            
            migrationBuilder.DropIndex(
                name: "IX_Voters_CellPhone_IsContacted_LastContactStatus",
                table: "Voters");
        }
    }
}
