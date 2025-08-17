import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  FormControlLabel,
  Checkbox,
  Alert,
  CircularProgress,
  Box,
  Typography
} from '@mui/material';
import { Phone, Cancel } from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';

interface PhoneBankingCallDialogProps {
  open: boolean;
  voter: any;
  onClose: () => void;
  onCallInitiated: (callSid: string) => void;
}

const PhoneBankingCallDialog: React.FC<PhoneBankingCallDialogProps> = ({
  open,
  voter,
  onClose,
  onCallInitiated
}) => {
  const [volunteerPhone, setVolunteerPhone] = useState('');
  const [recordCall, setRecordCall] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const formatPhoneNumber = (value: string) => {
    const cleaned = value.replace(/\D/g, '');
    if (cleaned.length <= 3) return cleaned;
    if (cleaned.length <= 6) return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3)}`;
    if (cleaned.length <= 10) return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6, 10)}`;
  };

  const handlePhoneChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatPhoneNumber(e.target.value);
    setVolunteerPhone(formatted);
  };

  const handleInitiateCall = async () => {
    setLoading(true);
    setError(null);

    try {
      // Remove formatting from phone number
      const cleanPhone = volunteerPhone.replace(/\D/g, '');
      
      if (cleanPhone.length !== 10) {
        setError('Please enter a valid 10-digit phone number');
        setLoading(false);
        return;
      }

      const response = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/phonebanking/initiate-call`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            voterId: voter.lalVoterId,
            volunteerPhone: `+1${cleanPhone}`,
            recordCall: recordCall
          })
        }
      );

      if (response.success) {
        onCallInitiated(response.callSid);
        onClose();
      } else {
        setError(response.message || 'Failed to initiate call');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to initiate call');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" gap={1}>
          <Phone color="primary" />
          <Typography>Initiate Call to {voter?.firstName} {voter?.lastName}</Typography>
        </Box>
      </DialogTitle>
      
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Box sx={{ mb: 3 }}>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Enter your phone number to receive the call. Your number will be masked when calling the voter.
          </Typography>
        </Box>

        <TextField
          label="Your Phone Number"
          fullWidth
          value={volunteerPhone}
          onChange={handlePhoneChange}
          placeholder="(555) 123-4567"
          disabled={loading}
          sx={{ mb: 2 }}
          helperText="Enter your 10-digit phone number"
        />

        <FormControlLabel
          control={
            <Checkbox
              checked={recordCall}
              onChange={(e) => setRecordCall(e.target.checked)}
              disabled={loading}
            />
          }
          label="Record this call for quality assurance"
        />

        <Alert severity="info" sx={{ mt: 2 }}>
          <Typography variant="body2">
            When you click "Start Call", your phone will ring first. Once you answer, 
            we'll connect you to the voter automatically.
          </Typography>
        </Alert>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} disabled={loading} startIcon={<Cancel />}>
          Cancel
        </Button>
        <Button
          onClick={handleInitiateCall}
          variant="contained"
          disabled={loading || !volunteerPhone}
          startIcon={loading ? <CircularProgress size={20} /> : <Phone />}
        >
          {loading ? 'Initiating...' : 'Start Call'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default PhoneBankingCallDialog;