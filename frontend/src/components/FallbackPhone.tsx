import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Alert,
  Button,
  Chip
} from '@mui/material';
import {
  Phone,
  Info,
  Settings
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

interface FallbackPhoneProps {
  voter: any;
  isAdmin?: boolean;
}

const FallbackPhone: React.FC<FallbackPhoneProps> = ({ voter, isAdmin }) => {
  const navigate = useNavigate();

  const formatPhoneNumber = (phone: string | null | undefined): string => {
    if (!phone) return 'No phone number';
    const cleaned = phone.replace(/\D/g, '');
    if (cleaned.length === 10) {
      return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    }
    return phone;
  };

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">Phone System</Typography>
          <Chip
            label="Setup Required"
            color="warning"
            size="small"
            icon={<Info />}
          />
        </Box>

        <Alert severity="info" sx={{ mb: 2 }}>
          <Typography variant="body2" gutterBottom>
            <strong>Browser calling is not configured yet.</strong>
          </Typography>
          <Typography variant="body2">
            {isAdmin ? (
              <>
                To enable browser-based calling:
                <ol style={{ margin: '8px 0', paddingLeft: '20px' }}>
                  <li>Go to Admin Dashboard → Settings</li>
                  <li>Configure your Twilio account credentials</li>
                  <li>Add your Twilio phone number</li>
                </ol>
              </>
            ) : (
              'Please contact your administrator to set up the phone system.'
            )}
          </Typography>
        </Alert>

        {voter && (
          <Box mb={2} p={2} bgcolor="grey.50" borderRadius={1}>
            <Typography variant="caption" color="text.secondary">
              Selected voter:
            </Typography>
            <Typography variant="h6">
              {voter.firstName} {voter.lastName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {formatPhoneNumber(voter.cellPhone)}
            </Typography>
          </Box>
        )}

        <Box display="flex" gap={2}>
          {voter && voter.cellPhone && (
            <Box>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                To call this voter manually:
              </Typography>
              <Typography variant="h6" sx={{ fontFamily: 'monospace' }}>
                {formatPhoneNumber(voter.cellPhone)}
              </Typography>
            </Box>
          )}
          
          {isAdmin && (
            <Button
              variant="contained"
              startIcon={<Settings />}
              onClick={() => navigate('/admin')}
            >
              Configure Twilio
            </Button>
          )}
        </Box>

        <Alert severity="warning" sx={{ mt: 2 }}>
          <Typography variant="caption">
            <strong>Privacy Note:</strong> Manual calling will show your personal number to the voter. 
            Browser calling (when configured) keeps your number private.
          </Typography>
        </Alert>
      </CardContent>
    </Card>
  );
};

export default FallbackPhone;