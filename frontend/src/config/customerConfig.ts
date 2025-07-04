// Customer-specific configuration
export interface CustomerConfig {
  logoUrl: string;
  logoAlt: string;
  appTitle: string;
  primaryColor?: string;
  secondaryColor?: string;
}

// Campaign-specific configuration
export interface CampaignConfig {
  candidateName: string;
  campaignName: string;
  campaignTitle: string;
  consentText: string;
  campaignWebsite?: string;
  campaignVenmo?: string;
  campaignYoutube?: string;
  voterRegistrationUrl?: string;
  volunteerHotline?: string;
}

// Get customer configuration from environment variables
export const getCustomerConfig = (): CustomerConfig => {
  return {
    logoUrl: process.env.REACT_APP_LOGO_URL || '/campaign-logo.png',
    logoAlt: process.env.REACT_APP_LOGO_ALT || 'Campaign Logo',
    appTitle: process.env.REACT_APP_TITLE || 'Canvassing App',
    primaryColor: process.env.REACT_APP_PRIMARY_COLOR,
    secondaryColor: process.env.REACT_APP_SECONDARY_COLOR,
  };
};

// Get campaign configuration from environment variables
export const getCampaignConfig = (): CampaignConfig => {
  return {
    candidateName: process.env.REACT_APP_CANDIDATE_NAME || 'Your Candidate',
    campaignName: process.env.REACT_APP_CAMPAIGN_NAME || 'Your Campaign',
    campaignTitle: process.env.REACT_APP_CAMPAIGN_TITLE || 'Campaign for Office',
    consentText: process.env.REACT_APP_CONSENT_TEXT || 'I agree to receive texts and robocalls from the campaign. Message and data rates may apply. Reply STOP to opt out.',
    campaignWebsite: process.env.REACT_APP_CAMPAIGN_WEBSITE,
    campaignVenmo: process.env.REACT_APP_CAMPAIGN_VENMO,
    campaignYoutube: process.env.REACT_APP_CAMPAIGN_YOUTUBE,
    voterRegistrationUrl: process.env.REACT_APP_VOTER_REGISTRATION_URL,
    volunteerHotline: process.env.REACT_APP_VOLUNTEER_HOTLINE,
  };
};

export const customerConfig = getCustomerConfig();
export const campaignConfig = getCampaignConfig();