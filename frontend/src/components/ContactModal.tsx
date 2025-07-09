import React, { useState, useRef } from 'react';
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
  InputAdornment
} from '@mui/material';
import { ContactPhone, Person, LocationOn, Mic, Stop, Delete, CameraAlt, PhotoCamera, MicOff } from '@mui/icons-material';
import { Voter, ContactStatus, VoterSupport, AuthUser } from '../types';
import { API_BASE_URL } from '../config';
import { campaignConfig } from '../config/customerConfig';

interface ContactModalProps {
  open: boolean;
  voter: Voter | null;
  onClose: () => void;
  onSubmit: (status: ContactStatus, notes: string, voterSupport?: VoterSupport, audioUrl?: string, audioDuration?: number, photoUrl?: string) => void;
  user?: AuthUser;
}

const ContactModal: React.FC<ContactModalProps> = ({
  open,
  voter,
  onClose,
  onSubmit,
  user
}) => {
  const [status, setStatus] = useState<ContactStatus>('reached');
  const [notes, setNotes] = useState('');
  const [voterSupport, setVoterSupport] = useState<VoterSupport | undefined>(undefined);
  const [submitting, setSubmitting] = useState(false);
  const [currentLocation, setCurrentLocation] = useState<{ latitude: number; longitude: number } | null>(null);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [checkingLocation, setCheckingLocation] = useState(false);
  const [distance, setDistance] = useState<number | null>(null);
  
  // Voice recording states
  const [isRecording, setIsRecording] = useState(false);
  const [audioBlob, setAudioBlob] = useState<Blob | null>(null);
  const [audioUrl, setAudioUrl] = useState<string | null>(null);
  const [recordingTime, setRecordingTime] = useState(0);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const recordingIntervalRef = useRef<NodeJS.Timeout | null>(null);
  
  // Photo capture states
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [photoPreviewUrl, setPhotoPreviewUrl] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  
  // Speech recognition states
  const [isListening, setIsListening] = useState(false);
  const [speechSupported, setSpeechSupported] = useState(false);
  const recognitionRef = useRef<any>(null);

  // Calculate distance between two coordinates using Haversine formula
  const calculateDistance = (lat1: number, lon1: number, lat2: number, lon2: number): number => {
    const R = 6371; // Earth's radius in km
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = 
      Math.sin(dLat/2) * Math.sin(dLat/2) +
      Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
      Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    return R * c * 1000; // Convert to meters
  };

  // Check location when modal opens
  React.useEffect(() => {
    if (open && voter?.latitude && voter?.longitude) {
      setCheckingLocation(true);
      setLocationError(null);
      
      if (!navigator.geolocation) {
        setLocationError('Geolocation is not supported by your browser');
        setCheckingLocation(false);
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position) => {
          const location = {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude
          };
          setCurrentLocation(location);
          
          // Calculate distance to voter
          const dist = calculateDistance(
            location.latitude,
            location.longitude,
            voter.latitude!,
            voter.longitude!
          );
          setDistance(Math.round(dist));
          setCheckingLocation(false);
        },
        (error) => {
          setLocationError('Unable to get your location. Please enable location services.');
          setCheckingLocation(false);
        },
        { 
          enableHighAccuracy: true,
          timeout: 10000,
          maximumAge: 0 
        }
      );
    }
  }, [open, voter]);

  // Initialize speech recognition
  React.useEffect(() => {
    // Check for speech recognition support
    const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    
    if (SpeechRecognition) {
      setSpeechSupported(true);
      
      const recognition = new SpeechRecognition();
      recognition.continuous = true;
      recognition.interimResults = true;
      recognition.lang = 'en-US';
      
      recognition.onresult = (event: any) => {
        let finalTranscript = '';
        let interimTranscript = '';
        
        for (let i = event.resultIndex; i < event.results.length; i++) {
          const transcript = event.results[i][0].transcript;
          if (event.results[i].isFinal) {
            finalTranscript += transcript + ' ';
          } else {
            interimTranscript += transcript;
          }
        }
        
        if (finalTranscript) {
          setNotes(prevNotes => {
            // Add a space if there's already content
            const separator = prevNotes && !prevNotes.endsWith(' ') ? ' ' : '';
            return prevNotes + separator + finalTranscript.trim();
          });
        }
      };
      
      recognition.onerror = (event: any) => {
        console.error('Speech recognition error:', event.error);
        setIsListening(false);
        
        if (event.error === 'no-speech') {
          // Silently stop - this is normal
        } else if (event.error === 'not-allowed') {
          alert('Microphone access denied. Please allow microphone access to use voice-to-text.');
        } else {
          alert(`Speech recognition error: ${event.error}`);
        }
      };
      
      recognition.onend = () => {
        setIsListening(false);
      };
      
      recognitionRef.current = recognition;
    } else {
      setSpeechSupported(false);
      console.log('Speech recognition not supported in this browser');
    }
    
    // Cleanup
    return () => {
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (e) {
          // Ignore errors during cleanup
        }
      }
    };
  }, []);

  // Speech recognition functions
  const startListening = () => {
    if (recognitionRef.current && !isListening) {
      try {
        recognitionRef.current.start();
        setIsListening(true);
      } catch (e) {
        console.error('Error starting speech recognition:', e);
        alert('Could not start voice input. Please try again.');
      }
    }
  };

  const stopListening = () => {
    if (recognitionRef.current && isListening) {
      try {
        recognitionRef.current.stop();
        setIsListening(false);
      } catch (e) {
        console.error('Error stopping speech recognition:', e);
      }
    }
  };

  const toggleListening = () => {
    if (isListening) {
      stopListening();
    } else {
      startListening();
    }
  };

  // Voice recording functions
  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      
      // Check for MediaRecorder support and determine mime type
      let mimeType = 'audio/webm';
      const options: MediaRecorderOptions = {};
      
      if (MediaRecorder.isTypeSupported('audio/webm;codecs=opus')) {
        options.mimeType = 'audio/webm;codecs=opus';
      } else if (MediaRecorder.isTypeSupported('audio/webm')) {
        options.mimeType = 'audio/webm';
      } else if (MediaRecorder.isTypeSupported('audio/mp4')) {
        // iOS Safari fallback
        options.mimeType = 'audio/mp4';
        mimeType = 'audio/mp4';
      } else if (MediaRecorder.isTypeSupported('audio/aac')) {
        // Another iOS fallback
        options.mimeType = 'audio/aac';
        mimeType = 'audio/aac';
      }
      
      const mediaRecorder = new MediaRecorder(stream, options);
      mediaRecorderRef.current = mediaRecorder;
      audioChunksRef.current = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onstop = () => {
        const audioBlob = new Blob(audioChunksRef.current, { type: mimeType });
        setAudioBlob(audioBlob);
        setAudioUrl(URL.createObjectURL(audioBlob));
        stream.getTracks().forEach(track => track.stop());
      };

      mediaRecorder.onerror = (event: any) => {
        console.error('MediaRecorder error:', event.error);
        alert('Recording error occurred. Please try again.');
        stream.getTracks().forEach(track => track.stop());
        setIsRecording(false);
      };

      // Use timeslice for iOS compatibility
      mediaRecorder.start(1000); // Collect data every second
      setIsRecording(true);
      setRecordingTime(0);
      
      // Start timer
      recordingIntervalRef.current = setInterval(() => {
        setRecordingTime(prev => prev + 1);
      }, 1000);
    } catch (error) {
      console.error('Error starting recording:', error);
      alert('Unable to access microphone. Please check your permissions.');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
      
      if (recordingIntervalRef.current) {
        clearInterval(recordingIntervalRef.current);
        recordingIntervalRef.current = null;
      }
    }
  };

  const deleteRecording = () => {
    setAudioBlob(null);
    setAudioUrl(null);
    setRecordingTime(0);
  };

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  // Photo handling functions
  const handlePhotoSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      // Check if it's a HEIC file
      if (file.type === 'image/heic' || file.type === 'image/heif' || file.name.toLowerCase().endsWith('.heic')) {
        // Convert HEIC to JPEG
        try {
          const convertedFile = await convertHeicToJpeg(file);
          setPhotoFile(convertedFile);
          
          // Create preview URL
          const reader = new FileReader();
          reader.onloadend = () => {
            setPhotoPreviewUrl(reader.result as string);
          };
          reader.readAsDataURL(convertedFile);
        } catch (error) {
          console.error('Error converting HEIC image:', error);
          alert('Unable to process HEIC image. Please try a different format.');
        }
      } else {
        setPhotoFile(file);
        
        // Create preview URL
        const reader = new FileReader();
        reader.onloadend = () => {
          setPhotoPreviewUrl(reader.result as string);
        };
        reader.readAsDataURL(file);
      }
    }
  };

  const convertHeicToJpeg = async (heicFile: File): Promise<File> => {
    // Create a canvas element
    const img = new Image();
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    
    return new Promise((resolve, reject) => {
      // For HEIC files, we'll use the browser's built-in image decoding
      // This works in Safari and some other browsers that support HEIC
      const url = URL.createObjectURL(heicFile);
      
      img.onload = () => {
        canvas.width = img.width;
        canvas.height = img.height;
        ctx?.drawImage(img, 0, 0);
        
        canvas.toBlob((blob) => {
          if (blob) {
            const convertedFile = new File([blob], heicFile.name.replace(/\.heic$/i, '.jpg'), {
              type: 'image/jpeg',
              lastModified: Date.now(),
            });
            resolve(convertedFile);
          } else {
            reject(new Error('Failed to convert image'));
          }
          URL.revokeObjectURL(url);
        }, 'image/jpeg', 0.9);
      };
      
      img.onerror = () => {
        URL.revokeObjectURL(url);
        reject(new Error('Failed to load HEIC image'));
      };
      
      img.src = url;
    });
  };

  const deletePhoto = () => {
    setPhotoFile(null);
    setPhotoPreviewUrl(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSubmit = async () => {
    if (!voter) return;
    
    setSubmitting(true);
    try {
      let audioUrl: string | undefined;
      let audioDuration: number | undefined;
      let photoUrl: string | undefined;
      
      // Upload audio if present
      if (audioBlob) {
        try {
          const formData = new FormData();
          formData.append('audioFile', audioBlob, 'voice-memo.webm');
          
          const token = localStorage.getItem('auth_token');
          const response = await fetch(`${API_BASE_URL}/api/contacts/upload-audio`, {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`
            },
            body: formData
          });
          
          if (response.ok) {
            const result = await response.json();
            audioUrl = result.audioUrl;
            audioDuration = recordingTime;
          } else {
            console.error('Failed to upload audio');
          }
        } catch (error) {
          console.error('Error uploading audio:', error);
        }
      }
      
      // Upload photo if present
      if (photoFile) {
        try {
          const formData = new FormData();
          formData.append('photoFile', photoFile);
          
          const token = localStorage.getItem('auth_token');
          const response = await fetch(`${API_BASE_URL}/api/contacts/upload-photo`, {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`
            },
            body: formData
          });
          
          if (response.ok) {
            const result = await response.json();
            photoUrl = result.photoUrl;
          } else {
            console.error('Failed to upload photo');
          }
        } catch (error) {
          console.error('Error uploading photo:', error);
        }
      }
      
      // Pass audio and photo info to parent
      await onSubmit(status, notes, voterSupport, audioUrl, audioDuration, photoUrl);
      
      // Reset form
      setStatus('reached');
      setNotes('');
      setVoterSupport(undefined);
      deleteRecording();
      deletePhoto();
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = () => {
    setStatus('reached');
    setNotes('');
    setVoterSupport(undefined);
    setCurrentLocation(null);
    setLocationError(null);
    setDistance(null);
    setCheckingLocation(false);
    deleteRecording();
    deletePhoto();
    if (recordingIntervalRef.current) {
      clearInterval(recordingIntervalRef.current);
    }
    // Stop speech recognition if active
    if (isListening) {
      stopListening();
    }
    onClose();
  };

  const isAdmin = user?.role === 'admin' || user?.role === 'superadmin';
  const isVolunteer = user?.role === 'volunteer';
  const isWithinProximity = distance !== null && distance <= 100;
  const hasPhoto = photoFile !== null;
  const canSubmit = !isVolunteer || isWithinProximity || (locationError !== null && hasPhoto);

  if (!voter) return null;

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <ContactPhone />
        Contact Voter
      </DialogTitle>
      
      <DialogContent>
        {/* Proximity Warning/Status */}
        <Box sx={{ 
          mb: 2, 
          p: 2, 
          bgcolor: checkingLocation ? 'info.light' : 
                   locationError ? (isAdmin ? 'warning.light' : 'error.light') :
                   isWithinProximity ? 'success.light' : 
                   (isAdmin ? 'warning.light' : 'error.light'), 
          borderRadius: 1 
        }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <LocationOn color={
              checkingLocation ? "info" :
              locationError ? (isAdmin ? "warning" : "error") :
              isWithinProximity ? "success" : 
              (isAdmin ? "warning" : "error")
            } />
            <Box>
              <Typography variant="body2" color={
                checkingLocation ? "info.dark" :
                locationError ? (isAdmin ? "warning.dark" : "error.dark") :
                isWithinProximity ? "success.dark" : 
                (isAdmin ? "warning.dark" : "error.dark")
              }>
                {checkingLocation ? (
                  <>Checking your location...</>
                ) : locationError ? (
                  isAdmin ? (
                    <>{locationError} As an admin, you can override this requirement.</>
                  ) : (
                    <>
                      {locationError}
                      {!hasPhoto && (
                        <Box component="span" sx={{ display: 'block', mt: 0.5, fontWeight: 'bold' }}>
                          Please take a photo of the house address to verify your visit.
                        </Box>
                      )}
                    </>
                  )
                ) : distance !== null ? (
                  isWithinProximity ? (
                    <>You are {distance} meters from the voter - within the required 100 meter range</>
                  ) : (
                    isAdmin ? (
                      <>You are {distance} meters from the voter - outside the 100 meter range. As an admin, you can override this requirement.</>
                    ) : (
                      <>You are {distance} meters from the voter - you must be within 100 meters to log this contact</>
                    )
                  )
                ) : (
                  <>Location check required</>
                )}
              </Typography>
              {/* Show override notice for admins when outside proximity */}
              {isAdmin && distance !== null && !isWithinProximity && (
                <Typography variant="caption" color="warning.dark" sx={{ mt: 0.5, display: 'block' }}>
                  <strong>Admin Override:</strong> You can proceed despite being outside the proximity requirement.
                </Typography>
              )}
            </Box>
          </Box>
        </Box>

        {/* Quick Reminder */}
        <Box sx={{ mb: 2, p: 1.5, bgcolor: 'info.light', borderRadius: 1 }}>
          <Typography variant="body2" color="info.contrastText">
            <strong>Tip:</strong> Keep it brief and friendly! See the Resources tab for your script.
          </Typography>
        </Box>

        {/* Voter Information */}
        <Box sx={{ mb: 3, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <Person fontSize="small" />
            <Typography variant="h6">
              {voter.firstName} {voter.middleName} {voter.lastName}
            </Typography>
          </Box>
          
          <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
            <LocationOn fontSize="small" color="action" />
            <Box>
              <Typography variant="body2">
                {voter.addressLine}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {voter.city}, {voter.state} {voter.zip}
              </Typography>
            </Box>
          </Box>
          
          {voter.cellPhone && (
            <Typography variant="body2" sx={{ mt: 1 }}>
              <strong>Phone:</strong> {voter.cellPhone}
            </Typography>
          )}
          
          {voter.email && (
            <Typography variant="body2">
              <strong>Email:</strong> {voter.email}
            </Typography>
          )}
          
          <Typography variant="body2">
            <strong>Age:</strong> {voter.age} • <strong>Gender:</strong> {voter.gender}
          </Typography>
          
          <Typography variant="body2">
            <strong>Vote Frequency:</strong> {voter.voteFrequency}
          </Typography>
        </Box>

        <Divider sx={{ my: 2 }} />

        {/* Contact Status */}
        <FormControl component="fieldset" sx={{ mb: 3 }}>
          <FormLabel component="legend">Contact Result *</FormLabel>
          <RadioGroup
            value={status}
            onChange={(e) => setStatus(e.target.value as ContactStatus)}
          >
            <FormControlLabel
              value="reached"
              control={<Radio />}
              label="Reached - Successfully spoke with voter"
            />
            <FormControlLabel
              value="not-home"
              control={<Radio />}
              label="Not Home - No one answered"
            />
            <FormControlLabel
              value="refused"
              control={<Radio />}
              label="Refused - Voter declined to speak"
            />
            <FormControlLabel
              value="needs-follow-up"
              control={<Radio />}
              label="Needs Follow-up - Require additional contact"
            />
          </RadioGroup>
        </FormControl>

        {/* Voter Support - Only show when status is "reached" */}
        {status === 'reached' && (
          <FormControl component="fieldset" sx={{ mb: 3 }}>
            <FormLabel component="legend">Voter Support Level (Optional)</FormLabel>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              How does this voter feel about {campaignConfig.candidateName}'s candidacy?
            </Typography>
            <RadioGroup
              value={voterSupport || ''}
              onChange={(e) => setVoterSupport(e.target.value as VoterSupport || undefined)}
            >
              <FormControlLabel
                value="strongyes"
                control={<Radio />}
                label={`Strong Yes - Will vote for ${campaignConfig.candidateName}`}
              />
              <FormControlLabel
                value="leaningyes"
                control={<Radio />}
                label={`Leaning Yes - May vote for ${campaignConfig.candidateName}`}
              />
              <FormControlLabel
                value="undecided"
                control={<Radio />}
                label="Undecided - Need to do research"
              />
              <FormControlLabel
                value="leaningno"
                control={<Radio />}
                label={`Leaning No - Not into ${campaignConfig.candidateName}`}
              />
              <FormControlLabel
                value="strongno"
                control={<Radio />}
                label={`Strong No - Definitely not voting for ${campaignConfig.candidateName}`}
              />
              <FormControlLabel
                value=""
                control={<Radio />}
                label="Prefer not to share"
              />
            </RadioGroup>
          </FormControl>
        )}

        {/* Notes */}
        <Box sx={{ mb: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <Typography variant="subtitle2">
              Notes (Optional)
            </Typography>
            {isListening && (
              <Chip
                label="Listening..."
                color="error"
                size="small"
                icon={<CircularProgress size={14} color="inherit" />}
              />
            )}
          </Box>
          
          {/* Voice input helper text */}
          {speechSupported && !isListening && (
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
              💡 Tap the microphone icon in the text field to use voice-to-text
            </Typography>
          )}
          
          {/* Voice Recording Controls */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            {!isRecording && !audioUrl && (
              <Button
                variant="outlined"
                startIcon={<Mic />}
                onClick={startRecording}
                size="small"
              >
                Record Voice Memo
              </Button>
            )}
            
            {isRecording && (
              <>
                <Button
                  variant="contained"
                  color="error"
                  startIcon={<Stop />}
                  onClick={stopRecording}
                  size="small"
                >
                  Stop Recording
                </Button>
                <Chip
                  label={formatTime(recordingTime)}
                  color="error"
                  size="small"
                  icon={<CircularProgress size={16} color="inherit" />}
                />
              </>
            )}
            
            {audioUrl && !isRecording && (
              <>
                <audio controls src={audioUrl} style={{ height: '35px' }} />
                <Chip
                  label={formatTime(recordingTime)}
                  size="small"
                  color="success"
                />
                <IconButton
                  size="small"
                  onClick={deleteRecording}
                  color="error"
                >
                  <Delete />
                </IconButton>
              </>
            )}
          </Box>
          
          {/* Photo Capture Controls */}
          <Box sx={{ 
            mb: 2,
            ...(locationError && !hasPhoto && isVolunteer && {
              p: 2,
              bgcolor: 'warning.light',
              borderRadius: 1,
              border: '2px solid',
              borderColor: 'warning.main'
            })
          }}>
            {locationError && !hasPhoto && isVolunteer && (
              <Typography variant="body2" sx={{ mb: 1, fontWeight: 'bold', color: 'warning.dark' }}>
                📷 Photo Required: Since location services are unavailable, please take a photo of the house address to verify your visit.
              </Typography>
            )}
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              capture="environment"
              onChange={handlePhotoSelect}
              style={{ display: 'none' }}
              id="photo-input"
            />
            
            {!photoPreviewUrl && (
              <Button
                variant="outlined"
                startIcon={<PhotoCamera />}
                onClick={() => fileInputRef.current?.click()}
                size="small"
              >
                Take Photo
              </Button>
            )}
            
            {photoPreviewUrl && (
              <>
                <Box
                  component="img"
                  src={photoPreviewUrl}
                  alt="Contact photo"
                  sx={{
                    height: 60,
                    width: 60,
                    objectFit: 'cover',
                    borderRadius: 1,
                    border: '2px solid',
                    borderColor: 'primary.main'
                  }}
                />
                <Chip
                  label="Photo added"
                  size="small"
                  color="success"
                  icon={<CameraAlt />}
                />
                <IconButton
                  size="small"
                  onClick={deletePhoto}
                  color="error"
                >
                  <Delete />
                </IconButton>
              </>
            )}
            </Box>
          </Box>
          
          <TextField
            fullWidth
            multiline
            rows={4}
            placeholder="Add any written notes about the interaction, voter feedback, concerns, etc."
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            variant="outlined"
            InputProps={{
              endAdornment: speechSupported ? (
                <InputAdornment position="end" sx={{ alignSelf: 'flex-start', mt: 1 }}>
                  <IconButton
                    onClick={toggleListening}
                    color={isListening ? "error" : "default"}
                    size="small"
                    title={isListening ? "Stop voice input" : "Start voice input"}
                  >
                    {isListening ? <MicOff /> : <Mic />}
                  </IconButton>
                </InputAdornment>
              ) : null
            }}
          />
        </Box>

      </DialogContent>
      
      <DialogActions sx={{ p: 2 }}>
        <Button
          onClick={handleClose}
          disabled={submitting}
        >
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={submitting || !canSubmit || checkingLocation}
          startIcon={<ContactPhone />}
          color={isAdmin && distance !== null && !isWithinProximity ? "warning" : "primary"}
        >
          {submitting ? 'Logging Contact...' : 
           checkingLocation ? 'Checking Location...' :
           (!canSubmit && locationError && !hasPhoto) ? 'Photo Required' :
           !canSubmit ? 'Too Far Away' : 
           (isAdmin && distance !== null && !isWithinProximity) ? 'Override & Log Contact' : 
           'Log Contact'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ContactModal;