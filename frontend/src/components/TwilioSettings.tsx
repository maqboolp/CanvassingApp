import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Alert,
  Divider,
  FormHelperText,
  InputAdornment,
  IconButton,
  LinearProgress,
  Chip
} from '@mui/material';
import {
  Save as SaveIcon,
  Visibility,
  VisibilityOff,
  Phone as PhoneIcon,
  Message as MessageIcon,
  Info as InfoIcon
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';

interface TwilioSettingsData {
  accountSid: string;
  fromPhoneNumber: string;
  smsPhoneNumber: string;
  messagingServiceSid: string;
  hasAuthToken: boolean;
}

export const TwilioSettings: React.FC = () => {
  const [settings, setSettings] = useState<TwilioSettingsData>({
    accountSid: '',
    fromPhoneNumber: '',
    smsPhoneNumber: '',
    messagingServiceSid: '',
    hasAuthToken: false
  });

  const [formData, setFormData] = useState({
    accountSid: '',
    authToken: '',
    fromPhoneNumber: '',
    smsPhoneNumber: '',
    messagingServiceSid: ''
  });

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showAuthToken, setShowAuthToken] = useState(false);

  useEffect(() => {
    fetchSettings();
  }, []);

  const fetchSettings = async () => {
    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequestRaw(
        `${API_BASE_URL}/api/settings/twilio`
      );
      
      if (!response || !response.ok) {
        const status = response?.status || 'Network error';
        throw new Error(`HTTP error! status: ${status}`);
      }
      
      const data = await response.json();
      setSettings(data);
      setFormData({
        accountSid: data.accountSid,
        authToken: '', // Never populate auth token for security
        fromPhoneNumber: data.fromPhoneNumber,
        smsPhoneNumber: data.smsPhoneNumber,
        messagingServiceSid: data.messagingServiceSid
      });
    } catch (err) {
      setError('Failed to load Twilio settings');
      console.error('Twilio settings error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    setError('');
    setSuccess('');

    try {
      const updateData: any = {};

      // Only include fields that have changed
      if (formData.accountSid !== settings.accountSid) {
        updateData.accountSid = formData.accountSid;
      }
      if (formData.authToken) {
        updateData.authToken = formData.authToken;
      }
      if (formData.fromPhoneNumber !== settings.fromPhoneNumber) {
        updateData.fromPhoneNumber = formData.fromPhoneNumber;
      }
      if (formData.smsPhoneNumber !== settings.smsPhoneNumber) {
        updateData.smsPhoneNumber = formData.smsPhoneNumber;
      }
      if (formData.messagingServiceSid !== settings.messagingServiceSid) {
        updateData.messagingServiceSid = formData.messagingServiceSid;
      }

      const response = await ApiErrorHandler.makeAuthenticatedRequestRaw(
        `${API_BASE_URL}/api/settings/twilio`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(updateData)
        }
      );

      if (!response || !response.ok) {
        const status = response?.status || 'Network error';
        throw new Error(`HTTP error! status: ${status}`);
      }

      const result = await response.json();
      setSuccess(result.message || 'Settings updated successfully');
      
      // Clear auth token field after save
      setFormData({ ...formData, authToken: '' });
      
      // Refresh settings
      fetchSettings();
    } catch (err) {
      setError('Failed to save Twilio settings');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  const formatPhoneNumber = (value: string) => {
    // Remove all non-digits
    const digits = value.replace(/\D/g, '');
    
    // Format as US phone number
    if (digits.length <= 3) return digits;
    if (digits.length <= 6) return `(${digits.slice(0, 3)}) ${digits.slice(3)}`;
    if (digits.length <= 10) return `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`;
    return `+${digits.slice(0, 1)} (${digits.slice(1, 4)}) ${digits.slice(4, 7)}-${digits.slice(7, 11)}`;
  };

  if (loading) return <LinearProgress />;

  return (
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" gap={1} mb={3}>
          <PhoneIcon color="primary" />
          <Typography variant="h5">Twilio Configuration</Typography>
        </Box>

        {error && (
          <Alert severity="error" onClose={() => setError('')} sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {success && (
          <Alert severity="success" onClose={() => setSuccess('')} sx={{ mb: 2 }}>
            {success}
          </Alert>
        )}

        <Box display="flex" flexDirection="column" gap={3}>
          {/* Account Credentials */}
          <Box>
            <Typography variant="h6" gutterBottom>
              Account Credentials
            </Typography>
            
            <Box display="flex" flexDirection="column" gap={2}>
              <TextField
                label="Account SID"
                fullWidth
                value={formData.accountSid}
                onChange={(e) => setFormData({ ...formData, accountSid: e.target.value })}
                placeholder="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
                helperText="Your Twilio Account SID"
              />

              <TextField
                label="Auth Token"
                fullWidth
                type={showAuthToken ? 'text' : 'password'}
                value={formData.authToken}
                onChange={(e) => setFormData({ ...formData, authToken: e.target.value })}
                placeholder={settings.hasAuthToken ? '••••••••••••••••' : 'Enter auth token'}
                helperText={settings.hasAuthToken ? 'Leave blank to keep existing token' : 'Your Twilio Auth Token'}
                InputProps={{
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() => setShowAuthToken(!showAuthToken)}
                        edge="end"
                      >
                        {showAuthToken ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  )
                }}
              />
            </Box>
          </Box>

          <Divider />

          {/* Phone Numbers */}
          <Box>
            <Typography variant="h6" gutterBottom>
              Phone Numbers
            </Typography>
            
            <Box display="flex" alignItems="center" gap={1} mb={2}>
              <InfoIcon color="info" fontSize="small" />
              <Typography variant="body2" color="text.secondary">
                Configure separate phone numbers for SMS and voice calls
              </Typography>
            </Box>

            <Box display="flex" flexDirection="column" gap={2}>
              <TextField
                label="Voice Call Phone Number"
                fullWidth
                value={formData.fromPhoneNumber}
                onChange={(e) => setFormData({ ...formData, fromPhoneNumber: formatPhoneNumber(e.target.value) })}
                placeholder="+1 (555) 123-4567"
                helperText="Primary phone number for robocalls"
                InputProps={{
                  startAdornment: <InputAdornment position="start"><PhoneIcon /></InputAdornment>
                }}
              />

              <TextField
                label="SMS Phone Number (Optional)"
                fullWidth
                value={formData.smsPhoneNumber}
                onChange={(e) => setFormData({ ...formData, smsPhoneNumber: formatPhoneNumber(e.target.value) })}
                placeholder="+1 (555) 987-6543"
                helperText="Dedicated SMS number (if different from voice). Leave blank to use voice number for SMS."
                InputProps={{
                  startAdornment: <InputAdornment position="start"><MessageIcon /></InputAdornment>
                }}
              />

              {formData.smsPhoneNumber && (
                <Alert severity="info" icon={<InfoIcon />}>
                  SMS messages will be sent from {formData.smsPhoneNumber}, while robocalls will use {formData.fromPhoneNumber || 'the voice number'}
                </Alert>
              )}
            </Box>
          </Box>

          <Divider />

          {/* Advanced Settings */}
          <Box>
            <Typography variant="h6" gutterBottom>
              Advanced Settings
            </Typography>
            
            <TextField
              label="Messaging Service SID (Optional)"
              fullWidth
              value={formData.messagingServiceSid}
              onChange={(e) => setFormData({ ...formData, messagingServiceSid: e.target.value })}
              placeholder="MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
              helperText="For high-volume SMS with multiple numbers and advanced features"
            />
          </Box>

          {/* Save Button */}
          <Box display="flex" justifyContent="flex-end" gap={2} mt={2}>
            <Button
              variant="contained"
              startIcon={<SaveIcon />}
              onClick={handleSave}
              disabled={saving}
            >
              {saving ? 'Saving...' : 'Save Settings'}
            </Button>
          </Box>

          {/* Configuration Status */}
          <Box bgcolor="grey.100" p={2} borderRadius={1}>
            <Typography variant="subtitle2" gutterBottom>
              Configuration Status
            </Typography>
            <Box display="flex" gap={1} flexWrap="wrap">
              <Chip 
                label="Account SID" 
                size="small" 
                color={formData.accountSid ? 'success' : 'default'}
              />
              <Chip 
                label="Auth Token" 
                size="small" 
                color={settings.hasAuthToken || formData.authToken ? 'success' : 'default'}
              />
              <Chip 
                label="Voice Number" 
                size="small" 
                color={formData.fromPhoneNumber ? 'success' : 'default'}
              />
              <Chip 
                label="SMS Number" 
                size="small" 
                color={formData.smsPhoneNumber ? 'info' : 'default'}
                variant={formData.smsPhoneNumber ? 'filled' : 'outlined'}
              />
            </Box>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
};