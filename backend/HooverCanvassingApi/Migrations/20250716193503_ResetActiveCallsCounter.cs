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
            // Check if table exists before trying to update it
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'PhoneNumbers') THEN
                        UPDATE ""PhoneNumbers"" SET ""CurrentActiveCalls"" = 0;
                    ELSIF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'TwilioPhoneNumbers') THEN
                        UPDATE ""TwilioPhoneNumbers"" SET ""CurrentActiveCalls"" = 0;
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
