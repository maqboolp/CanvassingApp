import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Alert,
  CircularProgress,
  Chip,
  TextField,
  FormHelperText
} from '@mui/material';
import {
  Phone,
  CallEnd,
  PhoneInTalk,
  Info
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';

interface SimplePhoneProps {
  voter: any;
  user: any;
  onCallComplete: () => void;
}

const SimplePhone: React.FC<SimplePhoneProps> = ({ voter, user, onCallComplete }) => {
  const [callStatus, setCallStatus] = useState<'ready' | 'initiating' | 'active' | 'ended'>('ready');
  const [error, setError] = useState<string | null>(null);
  const [callSid, setCallSid] = useState<string | null>(null);
  const [volunteerPhone, setVolunteerPhone] = useState(user?.phoneNumber || '');
  const [showPhoneInput, setShowPhoneInput] = useState(!user?.phoneNumber);

  const formatPhoneInput = (value: string) => {
    const cleaned = value.replace(/\D/g, '');
    if (cleaned.length <= 3) return cleaned;
    if (cleaned.length <= 6) return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3)}`;
    if (cleaned.length <= 10) return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6, 10)}`;
  };

  const initiateCall = async () => {
    if (!voter || !voter.cellPhone) {
      setError('No voter selected or voter has no phone number');
      return;
    }

    // Validate volunteer phone if needed
    const cleanPhone = volunteerPhone.replace(/\D/g, '');
    if (showPhoneInput && cleanPhone.length !== 10) {
      setError('Please enter a valid 10-digit phone number');
      return;
    }

    try {
      setError(null);
      setCallStatus('initiating');

      const response = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/phonebanking/call`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            voterId: voter.lalVoterId,
            volunteerPhone: showPhoneInput ? `+1${cleanPhone}` : undefined,
            recordCall: true
          })
        }
      );

      if (response.success) {
        setCallSid(response.callSid);
        setCallStatus('active');
        
        // Show success message
        setError(null);
      } else {
        throw new Error(response.message || 'Failed to initiate call');
      }
    } catch (err: any) {
      console.error('Failed to initiate call:', err);
      setError(err.message || 'Failed to initiate call');
      setCallStatus('ready');
    }
  };

  const endCall = () => {
    setCallStatus('ended');
    setCallSid(null);
    onCallComplete();
    
    // Reset to ready after a moment
    setTimeout(() => {
      setCallStatus('ready');
    }, 2000);
  };

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
            label={
              callStatus === 'ready' ? 'Ready' :
              callStatus === 'initiating' ? 'Connecting...' :
              callStatus === 'active' ? 'Call Active' : 'Call Ended'
            }
            color={
              callStatus === 'active' ? 'success' :
              callStatus === 'initiating' ? 'warning' :
              callStatus === 'ended' ? 'default' : 'primary'
            }
            size="small"
            icon={callStatus === 'active' ? <PhoneInTalk /> : undefined}
          />
        </Box>

        {error && (
          <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {voter && (
          <Box mb={2}>
            <Typography variant="body2" color="text.secondary">
              Ready to call:
            </Typography>
            <Typography variant="h6">
              {voter.firstName} {voter.lastName}
            </Typography>
            <Typography variant="body2" color="primary">
              {formatPhoneNumber(voter.cellPhone)}
            </Typography>
          </Box>
        )}

        {/* Show phone input if user doesn't have phone number */}
        {showPhoneInput && (
          <Box mb={2}>
            <TextField
              label="Your Phone Number"
              value={formatPhoneInput(volunteerPhone)}
              onChange={(e) => setVolunteerPhone(e.target.value)}
              fullWidth
              size="small"
              placeholder="(555) 123-4567"
              helperText="Enter your phone number to receive the call"
              disabled={callStatus !== 'ready'}
            />
            <FormHelperText>
              We'll call you first, then connect you to the voter
            </FormHelperText>
          </Box>
        )}

        {/* Info about how it works */}
        {callStatus === 'ready' && (
          <Alert severity="info" icon={<Info />} sx={{ mb: 2 }}>
            <Typography variant="body2">
              Click "Start Call" and your phone will ring. Answer it, and we'll automatically connect you to the voter.
              The voter will see our campaign number, not yours.
            </Typography>
          </Alert>
        )}

        {callStatus === 'initiating' && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            <Box display="flex" alignItems="center" gap={1}>
              <CircularProgress size={16} />
              <Typography variant="body2">
                Calling your phone now... Please answer when it rings.
              </Typography>
            </Box>
          </Alert>
        )}

        {callStatus === 'active' && (
          <Alert severity="success" sx={{ mb: 2 }}>
            <Typography variant="body2">
              Call is active. You should be connected to the voter now.
            </Typography>
          </Alert>
        )}

        <Box display="flex" justifyContent="center" gap={2}>
          {callStatus === 'ready' && (
            <Button
              variant="contained"
              color="success"
              size="large"
              startIcon={<Phone />}
              onClick={initiateCall}
              disabled={!voter || !voter.cellPhone || (showPhoneInput && !volunteerPhone)}
              sx={{ px: 4 }}
            >
              Start Call
            </Button>
          )}

          {callStatus === 'initiating' && (
            <Button
              variant="contained"
              disabled
              startIcon={<CircularProgress size={20} color="inherit" />}
              sx={{ px: 4 }}
            >
              Connecting...
            </Button>
          )}

          {callStatus === 'active' && (
            <Button
              variant="contained"
              color="error"
              size="large"
              startIcon={<CallEnd />}
              onClick={endCall}
              sx={{ px: 4 }}
            >
              End Call
            </Button>
          )}

          {callStatus === 'ended' && (
            <Button
              variant="outlined"
              disabled
              sx={{ px: 4 }}
            >
              Call Ended
            </Button>
          )}
        </Box>

        {!voter && (
          <Alert severity="info" sx={{ mt: 2 }}>
            Select a voter from the list to start calling
          </Alert>
        )}
      </CardContent>
    </Card>
  );
};

export default SimplePhone;