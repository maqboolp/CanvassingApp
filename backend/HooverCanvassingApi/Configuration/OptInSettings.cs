namespace HooverCanvassingApi.Configuration
{
    public class OptInSettings
    {
        public string CampaignName { get; set; } = "Tanveer for Hoover";
        public string CampaignPhone { get; set; } = "(205) 555-1234";
        public string OptInWebsiteUrl { get; set; } = "https://t4happ.com/opt-in";
        public string DefaultInvitationMessage { get; set; } = "{CampaignName}: Want campaign updates? Text JOIN to {CampaignPhone} or sign up at {OptInWebsiteUrl}. Reply STOP to opt out.";
        public string WelcomeMessage { get; set; } = "Welcome to {CampaignName}! You've successfully opted in to receive campaign updates. Reply STOP to opt out at any time. Reply HELP for support.";
        public string OptOutMessage { get; set; } = "You have been unsubscribed from {CampaignName} messages. Reply JOIN to resubscribe.";
        public string HelpMessage { get; set; } = "{CampaignName}: Reply STOP to unsubscribe. For support, visit tanveerforhoover.com or call {CampaignPhone}. Msg&data rates may apply.";
        
        public string FormatMessage(string template)
        {
            return template
                .Replace("{CampaignName}", CampaignName)
                .Replace("{CampaignPhone}", CampaignPhone)
                .Replace("{OptInWebsiteUrl}", OptInWebsiteUrl);
        }
    }
}