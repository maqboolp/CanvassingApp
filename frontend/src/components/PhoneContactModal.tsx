import React, { useState, useRef, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  TextField,
  Typography,
  Box,
  Divider,
  IconButton,
  Chip,
  CircularProgress,
  InputAdornment,
  Alert
} from '@mui/material';
import { 
  Phone, 
  Person, 
  Timer, 
  Mic, 
  Stop, 
  Delete, 
  MicOff,
  PhoneInTalk,
  PhoneMissed,
  Voicemail,
  WrongLocation,
  PhoneDisabled,
  Block,
  CallReceived,
  RemoveCircle
} from '@mui/icons-material';
import { Voter, VoterSupport, AuthUser } from '../types';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';

export enum PhoneContactStatus {
  Reached = 'Reached',
  NoAnswer = 'NoAnswer',
  VoiceMail = 'VoiceMail',
  WrongNumber = 'WrongNumber',
  Disconnected = 'Disconnected',
  Refused = 'Refused',
  Callback = 'Callback',
  DoNotCall = 'DoNotCall'
}

interface PhoneContactModalProps {
  open: boolean;
  voter: Voter | null;
  onClose: () => void;
  onSubmit: (status: PhoneContactStatus, notes: string, voterSupport?: VoterSupport, callDuration?: number, audioUrl?: string, audioDuration?: number) => void;
  user?: AuthUser;
}

const PhoneContactModal: React.FC<PhoneContactModalProps> = ({
  open,
  voter,
  onClose,
  onSubmit,
  user
}) => {
  const [status, setStatus] = useState<PhoneContactStatus>(PhoneContactStatus.Reached);
  const [notes, setNotes] = useState('');
  const [voterSupport, setVoterSupport] = useState<VoterSupport | undefined>(undefined);
  const [submitting, setSubmitting] = useState(false);
  const [callStartTime, setCallStartTime] = useState<Date | null>(null);
  const [callDuration, setCallDuration] = useState<number>(0);
  const [timerInterval, setTimerInterval] = useState<NodeJS.Timeout | null>(null);
  
  // Audio recording states
  const [isRecording, setIsRecording] = useState(false);
  const [audioBlob, setAudioBlob] = useState<Blob | null>(null);
  const [audioUrl, setAudioUrl] = useState<string | null>(null);
  const [audioDuration, setAudioDuration] = useState<number>(0);
  const [recordingError, setRecordingError] = useState<string | null>(null);
  const [uploadingAudio, setUploadingAudio] = useState(false);
  
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const recordingStartTimeRef = useRef<number>(0);

  // Start call timer when modal opens
  useEffect(() => {
    if (open) {
      setCallStartTime(new Date());
      const interval = setInterval(() => {
        if (callStartTime) {
          const duration = Math.floor((new Date().getTime() - callStartTime.getTime()) / 1000);
          setCallDuration(duration);
        }
      }, 1000);
      setTimerInterval(interval);
    } else {
      // Reset everything when modal closes
      if (timerInterval) {
        clearInterval(timerInterval);
      }
      setCallStartTime(null);
      setCallDuration(0);
      setStatus(PhoneContactStatus.Reached);
      setNotes('');
      setVoterSupport(undefined);
      setAudioBlob(null);
      setAudioUrl(null);
      setAudioDuration(0);
      setRecordingError(null);
    }

    return () => {
      if (timerInterval) {
        clearInterval(timerInterval);
      }
    };
  }, [open]);

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mediaRecorder = new MediaRecorder(stream);
      mediaRecorderRef.current = mediaRecorder;
      audioChunksRef.current = [];
      recordingStartTimeRef.current = Date.now();

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onstop = () => {
        const audioBlob = new Blob(audioChunksRef.current, { type: 'audio/webm' });
        setAudioBlob(audioBlob);
        const duration = Math.floor((Date.now() - recordingStartTimeRef.current) / 1000);
        setAudioDuration(duration);
        
        // Stop all tracks to release the microphone
        stream.getTracks().forEach(track => track.stop());
      };

      mediaRecorder.start();
      setIsRecording(true);
      setRecordingError(null);
    } catch (error) {
      console.error('Error starting recording:', error);
      setRecordingError('Unable to access microphone. Please check your permissions.');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
    }
  };

  const deleteRecording = () => {
    setAudioBlob(null);
    setAudioUrl(null);
    setAudioDuration(0);
  };

  const uploadAudio = async (blob: Blob): Promise<string | null> => {
    const formData = new FormData();
    formData.append('audioFile', blob, 'phone-recording.webm');
    
    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/phonecontacts/upload-audio`,
        {
          method: 'POST',
          body: formData,
        }
      );
      
      return response.audioUrl;
    } catch (error) {
      console.error('Failed to upload audio:', error);
      throw error;
    }
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      let uploadedAudioUrl = audioUrl;
      
      // Upload audio if recorded
      if (audioBlob && !uploadedAudioUrl) {
        setUploadingAudio(true);
        uploadedAudioUrl = await uploadAudio(audioBlob);
        setUploadingAudio(false);
      }
      
      await onSubmit(
        status, 
        notes, 
        voterSupport, 
        callDuration,
        uploadedAudioUrl || undefined,
        audioDuration || undefined
      );
      onClose();
    } catch (error) {
      setSubmitting(false);
      setUploadingAudio(false);
    }
  };

  const getStatusIcon = (status: PhoneContactStatus) => {
    switch (status) {
      case PhoneContactStatus.Reached:
        return <PhoneInTalk />;
      case PhoneContactStatus.NoAnswer:
        return <PhoneMissed />;
      case PhoneContactStatus.VoiceMail:
        return <Voicemail />;
      case PhoneContactStatus.WrongNumber:
        return <WrongLocation />;
      case PhoneContactStatus.Disconnected:
        return <PhoneDisabled />;
      case PhoneContactStatus.Refused:
        return <Block />;
      case PhoneContactStatus.Callback:
        return <CallReceived />;
      case PhoneContactStatus.DoNotCall:
        return <RemoveCircle />;
      default:
        return <Phone />;
    }
  };

  if (!voter) return null;

  const showVoterSupport = status === PhoneContactStatus.Reached;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" gap={1}>
          <Phone color="primary" />
          <Typography variant="h6">Record Call Results</Typography>
          <Box flex={1} />
          <Chip 
            icon={<Timer />}
            label={formatDuration(callDuration)}
            color="primary"
            variant="outlined"
          />
        </Box>
      </DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {/* Voter Info */}
          <Box sx={{ bgcolor: 'grey.100', p: 2, borderRadius: 1 }}>
            <Box display="flex" alignItems="center" gap={1} mb={1}>
              <Person />
              <Typography variant="subtitle1" fontWeight="bold">
                {voter.firstName} {voter.lastName}
              </Typography>
            </Box>
            {voter.cellPhone && (
              <Box display="flex" alignItems="center" gap={1}>
                <Phone fontSize="small" />
                <Typography variant="body2">{voter.cellPhone}</Typography>
              </Box>
            )}
            <Typography variant="body2" color="text.secondary">
              {voter.addressLine}, {voter.city}, {voter.state} {voter.zip}
            </Typography>
          </Box>

          <Divider />

          {/* Call Status */}
          <FormControl component="fieldset">
            <FormLabel component="legend">Call Status</FormLabel>
            <RadioGroup
              value={status}
              onChange={(e) => setStatus(e.target.value as PhoneContactStatus)}
            >
              <FormControlLabel
                value={PhoneContactStatus.Reached}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.Reached)}
                    <span>Reached - Spoke with voter</span>
                  </Box>
                }
              />
              <FormControlLabel
                value={PhoneContactStatus.NoAnswer}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.NoAnswer)}
                    <span>No Answer</span>
                  </Box>
                }
              />
              <FormControlLabel
                value={PhoneContactStatus.VoiceMail}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.VoiceMail)}
                    <span>Left Voicemail</span>
                  </Box>
                }
              />
              <FormControlLabel
                value={PhoneContactStatus.WrongNumber}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.WrongNumber)}
                    <span>Wrong Number</span>
                  </Box>
                }
              />
              <FormControlLabel
                value={PhoneContactStatus.Disconnected}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.Disconnected)}
                    <span>Number Disconnected</span>
                  </Box>
                }
              />
              <FormControlLabel
                value={PhoneContactStatus.Refused}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.Refused)}
                    <span>Refused to Talk</span>
                  </Box>
                }
              />
              <FormControlLabel
                value={PhoneContactStatus.Callback}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.Callback)}
                    <span>Requested Callback</span>
                  </Box>
                }
              />
              <FormControlLabel
                value={PhoneContactStatus.DoNotCall}
                control={<Radio />}
                label={
                  <Box display="flex" alignItems="center" gap={1}>
                    {getStatusIcon(PhoneContactStatus.DoNotCall)}
                    <span>Do Not Call</span>
                  </Box>
                }
              />
            </RadioGroup>
          </FormControl>

          {/* Voter Support - Only show if reached */}
          {showVoterSupport && (
            <>
              <Divider />
              <FormControl component="fieldset">
                <FormLabel component="legend">Voter Support Level</FormLabel>
                <RadioGroup
                  value={voterSupport || ''}
                  onChange={(e) => setVoterSupport(e.target.value as VoterSupport)}
                >
                  <FormControlLabel value="StrongYes" control={<Radio />} label="Strong Yes - Will vote for candidate" />
                  <FormControlLabel value="LeanYes" control={<Radio />} label="Lean Yes - Likely to vote for candidate" />
                  <FormControlLabel value="Undecided" control={<Radio />} label="Undecided" />
                  <FormControlLabel value="LeanNo" control={<Radio />} label="Lean No - Unlikely to vote for candidate" />
                  <FormControlLabel value="StrongNo" control={<Radio />} label="Strong No - Will not vote for candidate" />
                </RadioGroup>
              </FormControl>
            </>
          )}

          <Divider />

          {/* Notes */}
          <TextField
            label="Notes"
            multiline
            rows={4}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            fullWidth
            placeholder="Add any relevant notes about the call..."
          />

          {/* Audio Recording */}
          <Box>
            <Typography variant="subtitle2" gutterBottom>
              Audio Recording (Optional)
            </Typography>
            {recordingError && (
              <Alert severity="error" sx={{ mb: 1 }}>
                {recordingError}
              </Alert>
            )}
            <Box display="flex" alignItems="center" gap={1}>
              {!isRecording && !audioBlob && (
                <Button
                  variant="outlined"
                  startIcon={<Mic />}
                  onClick={startRecording}
                  disabled={submitting}
                >
                  Start Recording
                </Button>
              )}
              {isRecording && (
                <>
                  <Button
                    variant="contained"
                    color="error"
                    startIcon={<Stop />}
                    onClick={stopRecording}
                  >
                    Stop Recording
                  </Button>
                  <CircularProgress size={20} />
                  <Typography variant="body2" color="error">
                    Recording...
                  </Typography>
                </>
              )}
              {audioBlob && !isRecording && (
                <>
                  <Chip
                    icon={<Mic />}
                    label={`Recording (${formatDuration(audioDuration)})`}
                    color="primary"
                    variant="outlined"
                  />
                  <IconButton onClick={deleteRecording} size="small" color="error">
                    <Delete />
                  </IconButton>
                </>
              )}
            </Box>
          </Box>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={submitting}>
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={submitting || uploadingAudio || (showVoterSupport && !voterSupport)}
          startIcon={submitting || uploadingAudio ? <CircularProgress size={20} /> : null}
        >
          {uploadingAudio ? 'Uploading Audio...' : 'Save Contact'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default PhoneContactModal;