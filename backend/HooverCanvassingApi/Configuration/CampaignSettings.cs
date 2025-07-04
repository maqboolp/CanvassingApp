namespace HooverCanvassingApi.Configuration
{
    public class CampaignSettings
    {
        // Campaign Identity
        public string CandidateName { get; set; } = "[Campaign__CandidateName]";
        public string CampaignName { get; set; } = "[Campaign__CampaignName]";
        public string CampaignTitle { get; set; } = "[Campaign__CampaignTitle]";
        public string PaidForBy { get; set; } = "[Campaign__PaidForBy]";
        
        // Campaign Contact Info
        public string CampaignEmail { get; set; } = "[Campaign__CampaignEmail]";
        public string CampaignPhone { get; set; } = "[Campaign__CampaignPhone]";
        public string CampaignWebsite { get; set; } = "[Campaign__CampaignWebsite]";
        public string CampaignAddress { get; set; } = "[Campaign__CampaignAddress]";
        
        // Campaign Resources
        public string DonationUrl { get; set; } = "[Campaign__DonationUrl]";
        public string VolunteerSignupUrl { get; set; } = "[Campaign__VolunteerSignupUrl]";
        public string VoterRegistrationUrl { get; set; } = "[Campaign__VoterRegistrationUrl]";
        
        // Campaign Messaging
        public string DefaultCanvassingScript { get; set; } = "[Campaign__DefaultCanvassingScript]";
        public string OptInConsentText { get; set; } = "[Campaign__OptInConsentText]";
        
        // Election Info
        public string ElectionDate { get; set; } = "[Campaign__ElectionDate]";
        public string Office { get; set; } = "[Campaign__Office]";
        public string Jurisdiction { get; set; } = "[Campaign__Jurisdiction]";
    }
}