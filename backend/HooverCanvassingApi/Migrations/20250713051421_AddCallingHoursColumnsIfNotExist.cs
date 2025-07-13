using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HooverCanvassingApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCallingHoursColumnsIfNotExist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if columns exist before adding them
            // This makes the migration safe to run on databases that already have these columns
            
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Campaigns' AND column_name = 'EnforceCallingHours') THEN
                        ALTER TABLE ""Campaigns"" ADD ""EnforceCallingHours"" boolean NOT NULL DEFAULT true;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Campaigns' AND column_name = 'StartHour') THEN
                        ALTER TABLE ""Campaigns"" ADD ""StartHour"" integer NOT NULL DEFAULT 9;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Campaigns' AND column_name = 'EndHour') THEN
                        ALTER TABLE ""Campaigns"" ADD ""EndHour"" integer NOT NULL DEFAULT 20;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Campaigns' AND column_name = 'IncludeWeekends') THEN
                        ALTER TABLE ""Campaigns"" ADD ""IncludeWeekends"" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only drop columns if they exist
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Campaigns' AND column_name = 'EnforceCallingHours') THEN
                        ALTER TABLE ""Campaigns"" DROP COLUMN ""EnforceCallingHours"";
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Campaigns' AND column_name = 'StartHour') THEN
                        ALTER TABLE ""Campaigns"" DROP COLUMN ""StartHour"";
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Campaigns' AND column_name = 'EndHour') THEN
                        ALTER TABLE ""Campaigns"" DROP COLUMN ""EndHour"";
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Campaigns' AND column_name = 'IncludeWeekends') THEN
                        ALTER TABLE ""Campaigns"" DROP COLUMN ""IncludeWeekends"";
                    END IF;
                END $$;
            ");
        }
    }
}
