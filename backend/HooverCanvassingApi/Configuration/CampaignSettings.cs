namespace HooverCanvassingApi.Configuration
{
    public class CampaignSettings
    {
        // Campaign Identity
        public string CandidateName { get; set; } = "Your Candidate";
        public string CampaignName { get; set; } = "Your Campaign";
        public string CampaignTitle { get; set; } = "Campaign for Office";
        public string PaidForBy { get; set; } = "Paid for by Your Campaign";
        
        // Campaign Contact Info
        public string CampaignEmail { get; set; } = "info@yourcampaign.com";
        public string CampaignPhone { get; set; } = "555-CAMPAIGN";
        public string CampaignWebsite { get; set; } = "https://yourcampaign.com";
        public string CampaignAddress { get; set; } = "123 Campaign St, City, State 12345";
        
        // Campaign Resources
        public string DonationUrl { get; set; } = "";
        public string VolunteerSignupUrl { get; set; } = "";
        public string VoterRegistrationUrl { get; set; } = "";
        
        // Campaign Messaging
        public string DefaultCanvassingScript { get; set; } = "Hi, my name is [Your Name] and I'm a volunteer for {CandidateName}'s campaign. Are you planning to vote in the upcoming election?";
        public string OptInConsentText { get; set; } = "I agree to receive texts and robocalls from {CampaignName}. Message and data rates may apply. Reply STOP to opt out.";
        
        // Election Info
        public string ElectionDate { get; set; } = "";
        public string Office { get; set; } = "City Council";
        public string Jurisdiction { get; set; } = "Your City";
    }
}