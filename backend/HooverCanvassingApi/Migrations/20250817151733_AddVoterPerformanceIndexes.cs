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
            // Use raw SQL to check if indexes exist before creating them
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Voters_IsContacted_Zip"" ON ""Voters"" (""IsContacted"", ""Zip"");
                CREATE INDEX IF NOT EXISTS ""IX_Voters_Zip"" ON ""Voters"" (""Zip"");
                CREATE INDEX IF NOT EXISTS ""IX_Voters_LastName_FirstName"" ON ""Voters"" (""LastName"", ""FirstName"");
                CREATE INDEX IF NOT EXISTS ""IX_Voters_IsContacted"" ON ""Voters"" (""IsContacted"");
                CREATE INDEX IF NOT EXISTS ""IX_Voters_Latitude_Longitude"" ON ""Voters"" (""Latitude"", ""Longitude"");
                CREATE INDEX IF NOT EXISTS ""IX_Voters_VoteFrequency"" ON ""Voters"" (""VoteFrequency"");
                CREATE INDEX IF NOT EXISTS ""IX_Voters_CellPhone"" ON ""Voters"" (""CellPhone"");
                CREATE INDEX IF NOT EXISTS ""IX_Voters_CellPhone_IsContacted_LastContactStatus"" ON ""Voters"" (""CellPhone"", ""IsContacted"", ""LastContactStatus"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_Voters_IsContacted_Zip"";
                DROP INDEX IF EXISTS ""IX_Voters_Zip"";
                DROP INDEX IF EXISTS ""IX_Voters_LastName_FirstName"";
                DROP INDEX IF EXISTS ""IX_Voters_IsContacted"";
                DROP INDEX IF EXISTS ""IX_Voters_Latitude_Longitude"";
                DROP INDEX IF EXISTS ""IX_Voters_VoteFrequency"";
                DROP INDEX IF EXISTS ""IX_Voters_CellPhone"";
                DROP INDEX IF EXISTS ""IX_Voters_CellPhone_IsContacted_LastContactStatus"";
            ");
        }
    }
}
