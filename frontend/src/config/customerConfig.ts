// Customer-specific configuration
export interface CustomerConfig {
  logoUrl: string;
  logoAlt: string;
  appTitle: string;
  primaryColor?: string;
  secondaryColor?: string;
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

export const customerConfig = getCustomerConfig();