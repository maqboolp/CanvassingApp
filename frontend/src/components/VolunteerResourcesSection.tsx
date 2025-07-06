import React, { useState, useEffect } from 'react';
import { Box, Typography, Chip, Accordion, AccordionSummary, AccordionDetails, CircularProgress, Alert } from '@mui/material';
import {
  Language,
  VideoLibrary,
  Payment,
  HowToReg,
  Phone,
  Help,
  OpenInNew,
  ExpandMore,
  Link as LinkIcon
} from '@mui/icons-material';
import QRCode from 'react-qr-code';
import { campaignConfig } from '../config/customerConfig';
import { API_BASE_URL } from '../config';

interface VolunteerResourcesSectionProps {
  showQuickTips?: boolean;
  showQRCode?: boolean;
}

interface AdditionalResource {
  id: number;
  title: string;
  url: string;
  description: string;
  category: string;
  isActive: boolean;
  displayOrder: number;
}

const VolunteerResourcesSection: React.FC<VolunteerResourcesSectionProps> = ({ 
  showQuickTips = true,
  showQRCode = true 
}) => {
  const [appSettings, setAppSettings] = useState<{ [key: string]: string }>({});
  const [additionalResources, setAdditionalResources] = useState<AdditionalResource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      // Fetch public app settings
      const settingsResponse = await fetch(`${API_BASE_URL}/api/appsettings/public`);
      if (settingsResponse.ok) {
        const settings = await settingsResponse.json();
        setAppSettings(settings);
      }

      // Fetch additional resources
      const resourcesResponse = await fetch(`${API_BASE_URL}/api/additionalresources`);
      if (resourcesResponse.ok) {
        const resources = await resourcesResponse.json();
        setAdditionalResources(resources);
      }
    } catch (err) {
      setError('Failed to load resources');
    } finally {
      setLoading(false);
    }
  };

  // Use app settings with fallback to environment config
  const campaignWebsite = appSettings.CAMPAIGN_WEBSITE || campaignConfig.campaignWebsite;
  const campaignYoutube = appSettings.CAMPAIGN_YOUTUBE || campaignConfig.campaignYoutube;
  const campaignVenmo = appSettings.CAMPAIGN_VENMO || campaignConfig.campaignVenmo;
  const voterRegistrationUrl = appSettings.VOTER_REGISTRATION_URL || campaignConfig.voterRegistrationUrl;
  const volunteerHotline = appSettings.VOLUNTEER_HOTLINE || campaignConfig.volunteerHotline;
  const supportEmail = appSettings.SUPPORT_EMAIL || campaignConfig.supportEmail;

  // Group additional resources by category
  const groupedResources = additionalResources.reduce((acc, resource) => {
    if (!acc[resource.category]) {
      acc[resource.category] = [];
    }
    acc[resource.category].push(resource);
    return acc;
  }, {} as { [category: string]: AdditionalResource[] });
  return (
    <Box>
      {/* Campaign Information */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
          Campaign Information
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Language fontSize="small" sx={{ color: '#2f1c6a' }} />
            {campaignWebsite ? (
              <a 
                href={campaignWebsite} 
                target="_blank" 
                rel="noopener noreferrer"
                style={{ color: '#2f1c6a', textDecoration: 'none', fontSize: '14px' }}
              >
                Campaign Website <OpenInNew fontSize="small" sx={{ ml: 0.5, verticalAlign: 'middle' }} />
              </a>
            ) : (
              <Typography variant="body2" sx={{ color: '#999', fontStyle: 'italic', fontSize: '14px' }}>
                [REACT_APP_CAMPAIGN_WEBSITE]
              </Typography>
            )}
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <VideoLibrary fontSize="small" sx={{ color: '#2f1c6a' }} />
            {campaignYoutube ? (
              <a 
                href={campaignYoutube} 
                target="_blank" 
                rel="noopener noreferrer"
                style={{ color: '#2f1c6a', textDecoration: 'none', fontSize: '14px' }}
              >
                Campaign Videos <OpenInNew fontSize="small" sx={{ ml: 0.5, verticalAlign: 'middle' }} />
              </a>
            ) : (
              <Typography variant="body2" sx={{ color: '#999', fontStyle: 'italic', fontSize: '14px' }}>
                [REACT_APP_CAMPAIGN_YOUTUBE]
              </Typography>
            )}
          </Box>
        </Box>
      </Box>

      {/* Support the Campaign */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
          Support the Campaign
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Payment fontSize="small" sx={{ color: '#2f1c6a' }} />
            {campaignVenmo ? (
              <>
                <Typography variant="body2" sx={{ color: '#2f1c6a' }}>
                  Venmo: {campaignVenmo}
                </Typography>
                {showQRCode && (
                  <Box sx={{ p: 1, bgcolor: 'white', borderRadius: 1, border: '1px solid #e0e0e0', ml: 1 }}>
                    <QRCode 
                      value={`https://venmo.com/${campaignVenmo.replace('@', '')}`} 
                      size={80}
                      style={{ height: "auto", maxWidth: "100%", width: "100%" }}
                    />
                  </Box>
                )}
              </>
            ) : (
              <Typography variant="body2" sx={{ color: '#999', fontStyle: 'italic' }}>
                [REACT_APP_CAMPAIGN_VENMO]
              </Typography>
            )}
          </Box>
        </Box>
      </Box>

      {/* Voter Resources */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
          Voter Resources
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <HowToReg fontSize="small" sx={{ color: '#2f1c6a' }} />
          {voterRegistrationUrl ? (
            <a 
              href={voterRegistrationUrl} 
              target="_blank" 
              rel="noopener noreferrer"
              style={{ color: '#2f1c6a', textDecoration: 'none', fontSize: '14px' }}
            >
              Check Voter Registration <OpenInNew fontSize="small" sx={{ ml: 0.5, verticalAlign: 'middle' }} />
            </a>
          ) : (
            <Typography variant="body2" sx={{ color: '#999', fontStyle: 'italic', fontSize: '14px' }}>
              [REACT_APP_VOTER_REGISTRATION_URL]
            </Typography>
          )}
        </Box>
      </Box>

      {/* Support & Help */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
          Support & Help
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Phone fontSize="small" sx={{ color: '#2f1c6a' }} />
            <Typography variant="body2" sx={{ color: volunteerHotline ? '#2f1c6a' : '#999', fontStyle: volunteerHotline ? 'normal' : 'italic' }}>
              Volunteer Hotline: {volunteerHotline || '[REACT_APP_VOLUNTEER_HOTLINE]'}
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Help fontSize="small" sx={{ color: '#2f1c6a' }} />
            <Typography variant="body2" sx={{ color: supportEmail ? '#2f1c6a' : '#999', fontStyle: supportEmail ? 'normal' : 'italic' }}>
              App Support: Email {supportEmail || '[REACT_APP_SUPPORT_EMAIL]'}
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* Quick Tips */}
      {showQuickTips && (
        <Box sx={{ 
          p: 2, 
          background: 'rgba(47, 28, 106, 0.05)',
          borderRadius: 2,
          border: '1px solid rgba(47, 28, 106, 0.1)'
        }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
            Canvassing Quick Tips
          </Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
            <Typography variant="body2" sx={{ color: '#5a4080' }}>• Always wear your volunteer badge</Typography>
            <Typography variant="body2" sx={{ color: '#5a4080' }}>• Be respectful and polite</Typography>
            <Typography variant="body2" sx={{ color: '#5a4080' }}>• Don't argue with voters</Typography>
            <Typography variant="body2" sx={{ color: '#5a4080' }}>• Use the app to log all contacts</Typography>
            <Typography variant="body2" sx={{ color: '#5a4080' }}>• Ask for help if you need it</Typography>
          </Box>
        </Box>
      )}

      {/* Additional Resources */}
      {additionalResources.length > 0 && (
        <Box sx={{ mt: 3 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 2, color: '#2f1c6a' }}>
            Additional Resources
          </Typography>
          {loading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', p: 2 }}>
              <CircularProgress size={24} />
            </Box>
          ) : error ? (
            <Alert severity="error">{error}</Alert>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {Object.entries(groupedResources).map(([category, resources]) => (
                <Accordion key={category} defaultExpanded={resources.length <= 5}>
                  <AccordionSummary expandIcon={<ExpandMore />}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <LinkIcon fontSize="small" color="primary" />
                      <Typography variant="subtitle2" sx={{ fontWeight: 500 }}>
                        {category}
                      </Typography>
                      <Chip label={resources.length} size="small" />
                    </Box>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                      {resources.map((resource) => (
                        <Box key={resource.id}>
                          <a 
                            href={resource.url} 
                            target="_blank" 
                            rel="noopener noreferrer"
                            style={{ 
                              color: '#2f1c6a', 
                              textDecoration: 'none', 
                              fontSize: '14px',
                              display: 'flex',
                              alignItems: 'center',
                              gap: '4px'
                            }}
                          >
                            {resource.title} 
                            <OpenInNew fontSize="small" sx={{ ml: 0.5, verticalAlign: 'middle' }} />
                          </a>
                          {resource.description && (
                            <Typography variant="caption" sx={{ color: '#666', display: 'block', ml: 2 }}>
                              {resource.description}
                            </Typography>
                          )}
                        </Box>
                      ))}
                    </Box>
                  </AccordionDetails>
                </Accordion>
              ))}
            </Box>
          )}
        </Box>
      )}
    </Box>
  );
};

export default VolunteerResourcesSection;