import React from 'react';
import { Box, Container, Typography, Paper, Alert, Chip } from '@mui/material';
import { Warning } from '@mui/icons-material';

interface MissingConfig {
  variable: string;
  current: string;
  description: string;
}

const ConfigurationError: React.FC = () => {
  const missingConfigs: MissingConfig[] = [];

  // Check for missing configurations
  const checkConfig = (variable: string, value: string | undefined, description: string) => {
    if (!value || (value.startsWith('[') && value.endsWith(']'))) {
      missingConfigs.push({ variable, current: value || 'undefined', description });
    }
  };

  // Check customer config
  checkConfig('REACT_APP_LOGO_URL', process.env.REACT_APP_LOGO_URL, 'Campaign logo URL');
  checkConfig('REACT_APP_LOGO_ALT', process.env.REACT_APP_LOGO_ALT, 'Logo alt text');
  checkConfig('REACT_APP_TITLE', process.env.REACT_APP_TITLE, 'Application title');

  // Check campaign config
  checkConfig('REACT_APP_CANDIDATE_NAME', process.env.REACT_APP_CANDIDATE_NAME, 'Candidate name');
  checkConfig('REACT_APP_CAMPAIGN_NAME', process.env.REACT_APP_CAMPAIGN_NAME, 'Campaign name');
  checkConfig('REACT_APP_CAMPAIGN_TITLE', process.env.REACT_APP_CAMPAIGN_TITLE, 'Full campaign title');
  checkConfig('REACT_APP_CONSENT_TEXT', process.env.REACT_APP_CONSENT_TEXT, 'SMS consent text');

  if (missingConfigs.length === 0) {
    return null;
  }

  return (
    <Container maxWidth="md" sx={{ mt: 4 }}>
      <Paper sx={{ p: 4, backgroundColor: '#fff3e0' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
          <Warning color="warning" sx={{ fontSize: 40, mr: 2 }} />
          <Typography variant="h4" color="warning.main">
            Configuration Required
          </Typography>
        </Box>

        <Alert severity="warning" sx={{ mb: 3 }}>
          <Typography variant="body1">
            The application is not properly configured. Please set the following environment variables:
          </Typography>
        </Alert>

        <Box sx={{ mb: 3 }}>
          {missingConfigs.map((config) => (
            <Box key={config.variable} sx={{ mb: 2, p: 2, backgroundColor: '#fff', borderRadius: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <Chip label="Missing" color="error" size="small" sx={{ mr: 1 }} />
                <Typography variant="h6" sx={{ fontFamily: 'monospace' }}>
                  {config.variable}
                </Typography>
              </Box>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                {config.description}
              </Typography>
              <Typography variant="caption" sx={{ fontFamily: 'monospace', color: 'error.main' }}>
                Current value: {config.current}
              </Typography>
            </Box>
          ))}
        </Box>

        <Alert severity="info">
          <Typography variant="body2">
            <strong>For development:</strong> Create a <code>.env.local</code> file in the frontend directory
          </Typography>
          <Typography variant="body2">
            <strong>For production:</strong> Set these as environment variables in your deployment platform
          </Typography>
        </Alert>

        <Box sx={{ mt: 3, p: 2, backgroundColor: '#f5f5f5', borderRadius: 1 }}>
          <Typography variant="body2" sx={{ fontFamily: 'monospace', whiteSpace: 'pre' }}>
{`# Example .env.local file:
${missingConfigs.map(c => `${c.variable}=your_value_here`).join('\n')}
`}
          </Typography>
        </Box>
      </Paper>
    </Container>
  );
};

export default ConfigurationError;