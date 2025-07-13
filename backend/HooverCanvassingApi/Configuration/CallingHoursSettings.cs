namespace HooverCanvassingApi.Configuration
{
    public class CallingHoursSettings
    {
        public bool EnforceCallingHours { get; set; } = true;
        public int StartHour { get; set; } = 9;  // 9 AM
        public int EndHour { get; set; } = 20;    // 8 PM
        public bool IncludeWeekends { get; set; } = false;
        public string TimeZone { get; set; } = "America/Chicago"; // Central Time
    }
}