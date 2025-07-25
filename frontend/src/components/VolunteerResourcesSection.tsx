import React from 'react';
import { Box, Typography, Chip } from '@mui/material';
import {
  Language,
  VideoLibrary,
  Payment,
  HowToReg,
  Phone,
  Help,
  OpenInNew
} from '@mui/icons-material';
import QRCode from 'react-qr-code';
import { campaignConfig } from '../config/customerConfig';

interface VolunteerResourcesSectionProps {
  showQuickTips?: boolean;
  showQRCode?: boolean;
}

const VolunteerResourcesSection: React.FC<VolunteerResourcesSectionProps> = ({ 
  showQuickTips = true,
  showQRCode = true 
}) => {
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
            {campaignConfig.campaignWebsite ? (
              <a 
                href={campaignConfig.campaignWebsite} 
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
            {campaignConfig.campaignYoutube ? (
              <a 
                href={campaignConfig.campaignYoutube} 
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
            {campaignConfig.campaignVenmo ? (
              <>
                <Typography variant="body2" sx={{ color: '#2f1c6a' }}>
                  Venmo: {campaignConfig.campaignVenmo}
                </Typography>
                {showQRCode && (
                  <Box sx={{ p: 1, bgcolor: 'white', borderRadius: 1, border: '1px solid #e0e0e0', ml: 1 }}>
                    <QRCode 
                      value={`https://venmo.com/${campaignConfig.campaignVenmo.replace('@', '')}`} 
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
          {campaignConfig.voterRegistrationUrl ? (
            <a 
              href={campaignConfig.voterRegistrationUrl} 
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
            <Typography variant="body2" sx={{ color: campaignConfig.volunteerHotline ? '#2f1c6a' : '#999', fontStyle: campaignConfig.volunteerHotline ? 'normal' : 'italic' }}>
              Volunteer Hotline: {campaignConfig.volunteerHotline || '[REACT_APP_VOLUNTEER_HOTLINE]'}
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Help fontSize="small" sx={{ color: '#2f1c6a' }} />
            <Typography variant="body2" sx={{ color: campaignConfig.supportEmail ? '#2f1c6a' : '#999', fontStyle: campaignConfig.supportEmail ? 'normal' : 'italic' }}>
              App Support: Email {campaignConfig.supportEmail || '[REACT_APP_SUPPORT_EMAIL]'}
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
    </Box>
  );
};

export default VolunteerResourcesSection;