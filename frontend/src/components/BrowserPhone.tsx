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
  LinearProgress
} from '@mui/material';
import {
  Phone,
  PhoneDisabled,
  MicOff,
  Mic,
  VolumeUp,
  VolumeOff,
  CallEnd,
  Dialpad
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';

interface BrowserPhoneProps {
  voter: any;
  onCallComplete: (duration: number) => void;
  onCallStart: () => void;
}

const BrowserPhone: React.FC<BrowserPhoneProps> = ({ voter, onCallComplete, onCallStart }) => {
  const [device, setDevice] = useState<Device | null>(null);
  const [currentCall, setCurrentCall] = useState<Call | null>(null);
  const [callStatus, setCallStatus] = useState<string>('ready');
  const [callDuration, setCallDuration] = useState<number>(0);
  const [isMuted, setIsMuted] = useState(false);
  const [speakerOn, setSpeakerOn] = useState(true);
  const [isInitializing, setIsInitializing] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const callStartTime = useRef<Date | null>(null);
  const durationInterval = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    initializeDevice();
    
    return () => {
      if (device) {
        device.destroy();
      }
      if (durationInterval.current) {
        clearInterval(durationInterval.current);
      }
    };
  }, []);

  const initializeDevice = async () => {
    try {
      setIsInitializing(true);
      setError(null);

      // Get Twilio access token from backend
      const response = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/phonebanking/v2/token`,
        { method: 'GET' }
      );

      if (!response.token) {
        throw new Error('Failed to get access token');
      }

      // Initialize Twilio Device
      const newDevice = new Device(response.token, {
        logLevel: 1,
        edge: 'ashburn' // Use closest edge location
      });

      // Set up device event handlers
      newDevice.on('ready', () => {
        console.log('Twilio Device ready');
        setCallStatus('ready');
        setIsInitializing(false);
      });

      newDevice.on('error', (error) => {
        console.error('Twilio Device error:', error);
        setError(`Device error: ${error.message}`);
        setCallStatus('error');
      });

      newDevice.on('incoming', (call) => {
        console.log('Incoming call - this should not happen in our use case');
        call.reject();
      });

      // Register the device
      await newDevice.register();
      setDevice(newDevice);

    } catch (err: any) {
      console.error('Failed to initialize device:', err);
      setError('Failed to initialize phone. Please refresh and try again.');
      setIsInitializing(false);
    }
  };

  const startCall = async () => {
    if (!device || !voter) {
      setError('Device not ready or no voter selected');
      return;
    }

    try {
      setError(null);
      setCallStatus('connecting');
      onCallStart();

      // Make the call using voter ID (backend will look up the number)
      const call = await device.connect({
        params: {
          To: `voter_${voter.lalVoterId}`
        }
      });

      // Set up call event handlers
      call.on('accept', () => {
        console.log('Call accepted');
        setCallStatus('connected');
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
        setCallStatus('rejected');
        handleCallEnd();
      });

      setCurrentCall(call);

    } catch (err: any) {
      console.error('Failed to start call:', err);
      setError(`Failed to connect call: ${err.message}`);
      setCallStatus('error');
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
    setCallStatus('ready');
    setCurrentCall(null);
    setCallDuration(0);
    setIsMuted(false);
    callStartTime.current = null;
    
    if (duration > 0) {
      onCallComplete(duration);
    }
  };

  const toggleMute = () => {
    if (currentCall) {
      if (isMuted) {
        currentCall.mute(false);
      } else {
        currentCall.mute(true);
      }
      setIsMuted(!isMuted);
    }
  };

  const toggleSpeaker = () => {
    // Note: Speaker control is handled by the browser/OS
    // This is more of a UI indicator
    setSpeakerOn(!speakerOn);
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
            <Typography>Initializing phone system...</Typography>
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">Browser Phone</Typography>
          <Chip
            label={callStatus === 'ready' ? 'Ready' : callStatus}
            color={
              callStatus === 'connected' ? 'success' :
              callStatus === 'connecting' ? 'warning' :
              callStatus === 'error' ? 'error' : 'default'
            }
            size="small"
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
              {callStatus === 'connected' ? 'Connected to:' : 'Ready to call:'}
            </Typography>
            <Typography variant="h6">
              {voter.firstName} {voter.lastName}
            </Typography>
            <Typography variant="body2" color="primary">
              {formatPhoneNumber(voter.cellPhone)}
            </Typography>
          </Box>
        )}

        {callStatus === 'connecting' && (
          <Box mb={2}>
            <LinearProgress />
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Connecting call...
            </Typography>
          </Box>
        )}

        {callStatus === 'connected' && (
          <Box mb={2}>
            <Typography variant="h4" align="center" color="success.main">
              {formatDuration(callDuration)}
            </Typography>
          </Box>
        )}

        <Box display="flex" justifyContent="center" gap={2}>
          {callStatus === 'ready' && (
            <Button
              variant="contained"
              color="success"
              size="large"
              startIcon={<Phone />}
              onClick={startCall}
              disabled={!voter || !voter.cellPhone}
              sx={{ px: 4 }}
            >
              Start Call
            </Button>
          )}

          {(callStatus === 'connecting' || callStatus === 'connected') && (
            <>
              <IconButton
                color={isMuted ? 'error' : 'default'}
                onClick={toggleMute}
                size="large"
                sx={{
                  bgcolor: isMuted ? 'error.light' : 'grey.200',
                  '&:hover': { bgcolor: isMuted ? 'error.main' : 'grey.300' }
                }}
              >
                {isMuted ? <MicOff /> : <Mic />}
              </IconButton>

              <IconButton
                color="error"
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

              <IconButton
                color={speakerOn ? 'primary' : 'default'}
                onClick={toggleSpeaker}
                size="large"
                sx={{
                  bgcolor: speakerOn ? 'primary.light' : 'grey.200',
                  '&:hover': { bgcolor: speakerOn ? 'primary.main' : 'grey.300' }
                }}
              >
                {speakerOn ? <VolumeUp /> : <VolumeOff />}
              </IconButton>
            </>
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

export default BrowserPhone;