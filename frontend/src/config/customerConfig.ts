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
  supportEmail?: string;
  contactEmail?: string;
}

// Get customer configuration from environment variables
export const getCustomerConfig = (): CustomerConfig => {
  return {
    logoUrl: process.env.REACT_APP_LOGO_URL || '[REACT_APP_LOGO_URL]',
    logoAlt: process.env.REACT_APP_LOGO_ALT || '[REACT_APP_LOGO_ALT]',
    appTitle: process.env.REACT_APP_TITLE || '[REACT_APP_TITLE]',
    primaryColor: process.env.REACT_APP_PRIMARY_COLOR,
    secondaryColor: process.env.REACT_APP_SECONDARY_COLOR,
  };
};

// Get campaign configuration from environment variables
export const getCampaignConfig = (): CampaignConfig => {
  return {
    candidateName: process.env.REACT_APP_CANDIDATE_NAME || '[REACT_APP_CANDIDATE_NAME]',
    campaignName: process.env.REACT_APP_CAMPAIGN_NAME || '[REACT_APP_CAMPAIGN_NAME]',
    campaignTitle: process.env.REACT_APP_CAMPAIGN_TITLE || '[REACT_APP_CAMPAIGN_TITLE]',
    consentText: process.env.REACT_APP_CONSENT_TEXT || '[REACT_APP_CONSENT_TEXT]',
    campaignWebsite: process.env.REACT_APP_CAMPAIGN_WEBSITE,
    campaignVenmo: process.env.REACT_APP_CAMPAIGN_VENMO,
    campaignYoutube: process.env.REACT_APP_CAMPAIGN_YOUTUBE,
    voterRegistrationUrl: process.env.REACT_APP_VOTER_REGISTRATION_URL,
    volunteerHotline: process.env.REACT_APP_VOLUNTEER_HOTLINE,
    supportEmail: process.env.REACT_APP_SUPPORT_EMAIL,
    contactEmail: process.env.REACT_APP_CONTACT_EMAIL || process.env.REACT_APP_SUPPORT_EMAIL,
  };
};

export const customerConfig = getCustomerConfig();
export const campaignConfig = getCampaignConfig();