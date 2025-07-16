using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class ResetActiveCallsCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reset all CurrentActiveCalls to 0 since we're no longer tracking this
            migrationBuilder.Sql("UPDATE \"PhoneNumbers\" SET \"CurrentActiveCalls\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
