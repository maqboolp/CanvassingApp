import React, { useState, useEffect, useRef } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  IconButton,
  Alert,
  CircularProgress,
  Chip,
  LinearProgress,
  Tooltip
} from '@mui/material';
import {
  Phone,
  PhoneDisabled,
  MicOff,
  Mic,
  VolumeUp,
  CallEnd,
  Headset,
  SignalCellularAlt
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';

// Conditional import for Twilio Voice SDK
let Device: any;

try {
  const twilioSDK = require('@twilio/voice-sdk');
  Device = twilioSDK.Device;
} catch (e) {
  console.warn('Twilio Voice SDK not available. Phone features will be disabled.');
  // Create mock classes to prevent runtime errors
  class MockCall {
    on() {}
    disconnect() {}
    mute() {}
  }
  
  Device = class MockDevice {
    on() {}
    register() { return Promise.resolve(); }
    connect() { return Promise.resolve(new MockCall()); }
    destroy() {}
  };
}

interface WebRTCPhoneProps {
  voter: any;
  onCallComplete: () => void;
}

const WebRTCPhone: React.FC<WebRTCPhoneProps> = ({ voter, onCallComplete }) => {
  const [device, setDevice] = useState<any>(null);
  const [currentCall, setCurrentCall] = useState<any>(null);
  const [deviceState, setDeviceState] = useState<'offline' | 'ready' | 'busy'>('offline');
  const [callDuration, setCallDuration] = useState<number>(0);
  const [isMuted, setIsMuted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  
  const callStartTime = useRef<Date | null>(null);
  const durationInterval = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    initializeDevice();
    
    return () => {
      cleanup();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const cleanup = () => {
    if (durationInterval.current) {
      clearInterval(durationInterval.current);
    }
    if (device) {
      device.destroy();
    }
  };

  const initializeDevice = async () => {
    try {
      setIsInitializing(true);
      setError(null);

      // Check if we have a mock Device (SDK not available)
      if (!Device || Device.name === 'MockDevice') {
        console.warn('Twilio Voice SDK not available - using fallback mode');
        setError('Phone system is not available. Please use the standard calling feature.');
        setDeviceState('offline');
        setIsInitializing(false);
        return;
      }

      // Get Twilio access token
      let response;
      try {
        response = await ApiErrorHandler.makeAuthenticatedRequest(
          `${API_BASE_URL}/api/browser-call/token`,
          { method: 'GET' }
        );
      } catch (apiError: any) {
        console.error('Failed to get token from API:', apiError);
        throw new Error('Phone service is temporarily unavailable. Please try again later.');
      }

      if (!response || !response.token) {
        throw new Error('Failed to get access token from server');
      }

      // Create new Device with the token
      const newDevice = new Device(response.token, {
        logLevel: 1,
        // codecPreferences are optional, using defaults
        edge: 'ashburn'
      });

      // Register event handlers
      newDevice.on('registered', () => {
        console.log('Device registered successfully');
        setDeviceState('ready');
        setIsInitializing(false);
      });

      newDevice.on('error', (error: any) => {
        console.error('Device error:', error);
        setError(`Connection error: ${error.message}`);
        setDeviceState('offline');
      });

      newDevice.on('incoming', (call: any) => {
        // We don't expect incoming calls in this use case
        console.log('Unexpected incoming call');
        call.reject();
      });

      // Register the device
      await newDevice.register();
      setDevice(newDevice);

    } catch (err: any) {
      console.error('Failed to initialize device:', err);
      const errorMessage = err.message || 'Failed to initialize phone system. Please refresh the page.';
      setError(errorMessage);
      setIsInitializing(false);
      setDeviceState('offline');
    }
  };

  const makeCall = async () => {
    if (!device || !voter || deviceState !== 'ready') {
      setError('Phone system not ready or no voter selected');
      return;
    }

    try {
      setError(null);
      setDeviceState('busy');

      // Log the voter details for debugging
      console.log('Making call to voter:', {
        name: `${voter.firstName} ${voter.lastName}`,
        phone: voter.cellPhone,
        voterId: voter.lalVoterId
      });

      // Make the call with voter ID
      // The backend will look up the actual phone number
      const params = {
        To: `voter:${voter.lalVoterId}`
      };

      const call = await device.connect({ params });

      // Set up call event handlers
      call.on('accept', () => {
        console.log('Call connected');
        callStartTime.current = new Date();
        
        // Start duration timer
        durationInterval.current = setInterval(() => {
          if (callStartTime.current) {
            const duration = Math.floor((new Date().getTime() - callStartTime.current.getTime()) / 1000);
            setCallDuration(duration);
          }
        }, 1000);
      });

      call.on('disconnect', () => {
        console.log('Call disconnected');
        handleCallEnd();
      });

      call.on('cancel', () => {
        console.log('Call cancelled');
        handleCallEnd();
      });

      call.on('reject', () => {
        console.log('Call rejected');
        setError('Call was rejected');
        handleCallEnd();
      });

      setCurrentCall(call);

    } catch (err: any) {
      console.error('Failed to make call:', err);
      setError(`Failed to connect call: ${err.message}`);
      setDeviceState('ready');
    }
  };

  const endCall = () => {
    if (currentCall) {
      currentCall.disconnect();
    }
  };

  const handleCallEnd = () => {
    if (durationInterval.current) {
      clearInterval(durationInterval.current);
    }
    
    const duration = callDuration;
    
    setDeviceState('ready');
    setCurrentCall(null);
    setCallDuration(0);
    setIsMuted(false);
    callStartTime.current = null;
    
    if (duration > 0) {
      onCallComplete();
    }
  };

  const toggleMute = () => {
    if (currentCall) {
      const newMuteState = !isMuted;
      currentCall.mute(newMuteState);
      setIsMuted(newMuteState);
    }
  };

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const formatPhoneNumber = (phone: string | null | undefined): string => {
    if (!phone) return 'No phone number';
    const cleaned = phone.replace(/\D/g, '');
    if (cleaned.length === 10) {
      return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    }
    return phone;
  };

  if (isInitializing) {
    return (
      <Card>
        <CardContent>
          <Box display="flex" alignItems="center" gap={2}>
            <CircularProgress size={20} />
            <Typography>Initializing browser phone system...</Typography>
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        {/* Header */}
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Box display="flex" alignItems="center" gap={1}>
            <Headset color="primary" />
            <Typography variant="h6">Browser Phone</Typography>
          </Box>
          <Box display="flex" alignItems="center" gap={1}>
            <Tooltip title={deviceState === 'ready' ? 'Connected' : deviceState === 'busy' ? 'On Call' : 'Offline'}>
              <SignalCellularAlt 
                color={deviceState === 'ready' ? 'success' : deviceState === 'busy' ? 'warning' : 'error'}
                fontSize="small"
              />
            </Tooltip>
            <Chip
              label={
                deviceState === 'ready' ? 'Ready' :
                deviceState === 'busy' ? 'On Call' : 'Offline'
              }
              color={
                deviceState === 'ready' ? 'success' :
                deviceState === 'busy' ? 'warning' : 'error'
              }
              size="small"
              variant={deviceState === 'busy' ? 'filled' : 'outlined'}
            />
          </Box>
        </Box>

        {/* Error Display */}
        {error && (
          <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {/* Voter Information */}
        {voter && (
          <Box mb={2} p={2} bgcolor="grey.50" borderRadius={1}>
            <Typography variant="caption" color="text.secondary">
              {currentCall ? 'Connected to:' : 'Ready to call:'}
            </Typography>
            <Typography variant="h6">
              {voter.firstName} {voter.lastName}
            </Typography>
            <Typography variant="body2" color="primary">
              {formatPhoneNumber(voter.cellPhone)}
            </Typography>
          </Box>
        )}

        {/* Call Duration Display */}
        {currentCall && (
          <Box mb={2}>
            <LinearProgress variant="indeterminate" sx={{ mb: 1 }} />
            <Typography variant="h3" align="center" color="success.main">
              {formatDuration(callDuration)}
            </Typography>
            <Typography variant="caption" align="center" display="block" color="text.secondary">
              Call in progress
            </Typography>
          </Box>
        )}

        {/* Call Controls */}
        <Box display="flex" justifyContent="center" gap={2}>
          {!currentCall && deviceState === 'ready' && (
            <Button
              variant="contained"
              color="success"
              size="large"
              startIcon={<Phone />}
              onClick={makeCall}
              disabled={!voter || !voter.cellPhone}
              sx={{ px: 4, py: 1.5 }}
            >
              Start Call
            </Button>
          )}

          {currentCall && (
            <>
              <Tooltip title={isMuted ? 'Unmute' : 'Mute'}>
                <IconButton
                  onClick={toggleMute}
                  size="large"
                  sx={{
                    bgcolor: isMuted ? 'error.main' : 'grey.200',
                    color: isMuted ? 'white' : 'text.primary',
                    '&:hover': { 
                      bgcolor: isMuted ? 'error.dark' : 'grey.300' 
                    }
                  }}
                >
                  {isMuted ? <MicOff /> : <Mic />}
                </IconButton>
              </Tooltip>

              <IconButton
                onClick={endCall}
                size="large"
                sx={{
                  bgcolor: 'error.main',
                  color: 'white',
                  px: 3,
                  '&:hover': { bgcolor: 'error.dark' }
                }}
              >
                <CallEnd />
              </IconButton>

              <Tooltip title="Volume is controlled by your device">
                <IconButton
                  size="large"
                  sx={{
                    bgcolor: 'grey.200',
                    '&:hover': { bgcolor: 'grey.300' }
                  }}
                >
                  <VolumeUp />
                </IconButton>
              </Tooltip>
            </>
          )}

          {deviceState === 'offline' && (
            <Button
              variant="outlined"
              onClick={initializeDevice}
              startIcon={<PhoneDisabled />}
            >
              Reconnect
            </Button>
          )}
        </Box>

        {/* Info Messages */}
        {!voter && deviceState === 'ready' && (
          <Alert severity="info" sx={{ mt: 2 }}>
            Select a voter from the list to start calling
          </Alert>
        )}

        {deviceState === 'ready' && voter && (
          <Alert severity="success" sx={{ mt: 2 }}>
            Phone system ready. Click "Start Call" to dial directly from your browser. 
            No phone needed - use your computer's microphone and speakers!
          </Alert>
        )}
      </CardContent>
    </Card>
  );
};

export default WebRTCPhone;