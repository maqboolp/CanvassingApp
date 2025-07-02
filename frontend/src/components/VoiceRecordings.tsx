import React, { useState, useEffect, useRef } from 'react';
import {
  Typography,
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
  Chip,
  Tooltip,
  LinearProgress,
  Card,
  CardContent,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  PlayArrow as PlayIcon,
  Stop as StopIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Mic as MicIcon,
  FiberManualRecord as RecordIcon,
  Download as DownloadIcon,
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler, ApiError } from '../utils/apiErrorHandler';

interface VoiceRecording {
  id: number;
  name: string;
  description?: string;
  url: string;
  fileName?: string;
  fileSizeBytes?: number;
  durationSeconds?: number;
  createdAt: string;
  lastUsedAt?: string;
  usageCount: number;
  createdBy: string;
}

interface VoiceRecordingsProps {
  user: { id: string; role: string };
}

const VoiceRecordings: React.FC<VoiceRecordingsProps> = ({ user }) => {
  const [recordings, setRecordings] = useState<VoiceRecording[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [recording, setRecording] = useState(false);
  const [playingId, setPlayingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  
  // Dialog states
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false);
  const [recordDialogOpen, setRecordDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedRecording, setSelectedRecording] = useState<VoiceRecording | null>(null);
  
  // Form states
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    file: null as File | null,
  });
  
  // Audio recording states
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [recordingTime, setRecordingTime] = useState(0);
  const recordingTimerRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    fetchRecordings();
  }, []);

  const fetchRecordings = async () => {
    try {
      setLoading(true);
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voicerecordings`
      );
      setRecordings(data);
    } catch (err) {
      if (err instanceof ApiError && err.isAuthError) {
        return;
      }
      setError('Failed to load voice recordings');
    } finally {
      setLoading(false);
    }
  };

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return 'N/A';
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDuration = (seconds?: number) => {
    if (!seconds) return 'N/A';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const handleFileUpload = async () => {
    if (!formData.file || !formData.name) {
      setError('Please provide a name and select a file');
      return;
    }

    const data = new FormData();
    data.append('file', formData.file);
    data.append('name', formData.name);
    if (formData.description) {
      data.append('description', formData.description);
    }

    try {
      setUploading(true);
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voicerecordings/upload`,
        {
          method: 'POST',
          body: data,
        }
      );
      setSuccess('Voice recording uploaded successfully');
      setUploadDialogOpen(false);
      setFormData({ name: '', description: '', file: null });
      fetchRecordings();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to upload voice recording');
      }
    } finally {
      setUploading(false);
    }
  };

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      
      // Try to use a format supported by Twilio
      let options: MediaRecorderOptions = {};
      if (MediaRecorder.isTypeSupported('audio/wav')) {
        options = { mimeType: 'audio/wav' };
      } else if (MediaRecorder.isTypeSupported('audio/mp3')) {
        options = { mimeType: 'audio/mp3' };
      } else if (MediaRecorder.isTypeSupported('audio/webm')) {
        options = { mimeType: 'audio/webm' };
      }
      
      const mediaRecorder = new MediaRecorder(stream, options);
      mediaRecorderRef.current = mediaRecorder;
      audioChunksRef.current = [];

      mediaRecorder.ondataavailable = (event) => {
        audioChunksRef.current.push(event.data);
      };

      mediaRecorder.onstop = async () => {
        // Try to use audio/wav if supported, otherwise fall back to webm
        const mimeType = MediaRecorder.isTypeSupported('audio/wav') ? 'audio/wav' : 'audio/webm';
        const extension = mimeType === 'audio/wav' ? 'wav' : 'webm';
        
        const audioBlob = new Blob(audioChunksRef.current, { type: mimeType });
        const audioFile = new File([audioBlob], `recording.${extension}`, { type: mimeType });
        
        // Stop all tracks
        stream.getTracks().forEach(track => track.stop());
        
        // Upload the recording
        const data = new FormData();
        data.append('file', audioFile);
        data.append('name', formData.name || 'Voice Recording');
        data.append('description', formData.description || '');
        data.append('durationSeconds', Math.floor(recordingTime).toString());

        try {
          setUploading(true);
          await ApiErrorHandler.makeAuthenticatedRequest(
            `${API_BASE_URL}/api/voicerecordings/upload`,
            {
              method: 'POST',
              body: data,
            }
          );
          setSuccess('Voice recording saved successfully');
          setRecordDialogOpen(false);
          setFormData({ name: '', description: '', file: null });
          fetchRecordings();
        } catch (err) {
          if (err instanceof ApiError) {
            setError(err.message);
          } else {
            setError('Failed to save voice recording');
          }
        } finally {
          setUploading(false);
          setRecording(false);
          setRecordingTime(0);
        }
      };

      mediaRecorder.start();
      setRecording(true);
      
      // Start timer
      recordingTimerRef.current = setInterval(() => {
        setRecordingTime(prev => prev + 1);
      }, 1000);
    } catch (err) {
      setError('Failed to access microphone');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      mediaRecorderRef.current.stop();
      if (recordingTimerRef.current) {
        clearInterval(recordingTimerRef.current);
        recordingTimerRef.current = null;
      }
    }
  };

  const playRecording = (recording: VoiceRecording) => {
    if (playingId === recording.id) {
      if (audioRef.current) {
        audioRef.current.pause();
        audioRef.current = null;
      }
      setPlayingId(null);
    } else {
      if (audioRef.current) {
        audioRef.current.pause();
      }
      const audio = new Audio(recording.url);
      audioRef.current = audio;
      audio.play();
      audio.onended = () => setPlayingId(null);
      setPlayingId(recording.id);
    }
  };

  const handleEdit = async () => {
    if (!selectedRecording) return;

    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voicerecordings/${selectedRecording.id}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            name: formData.name,
            description: formData.description,
          })
        }
      );
      setSuccess('Voice recording updated successfully');
      setEditDialogOpen(false);
      fetchRecordings();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to update voice recording');
      }
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this voice recording?')) return;

    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voicerecordings/${id}`,
        {
          method: 'DELETE'
        }
      );
      setSuccess('Voice recording deleted successfully');
      fetchRecordings();
    } catch (err: any) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to delete voice recording');
      }
    }
  };

  const openEditDialog = (recording: VoiceRecording) => {
    setSelectedRecording(recording);
    setFormData({
      name: recording.name,
      description: recording.description || '',
      file: null,
    });
    setEditDialogOpen(true);
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5">Voice Recordings</Typography>
        <Box>
          <Button
            variant="outlined"
            startIcon={<MicIcon />}
            onClick={() => setRecordDialogOpen(true)}
            sx={{ mr: 2 }}
          >
            Record New
          </Button>
          <Button
            variant="contained"
            startIcon={<UploadIcon />}
            onClick={() => setUploadDialogOpen(true)}
          >
            Upload File
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>
          {success}
        </Alert>
      )}
      
      <Alert severity="info" sx={{ mb: 2 }}>
        <Typography variant="body2">
          <strong>Note:</strong> For best compatibility with phone systems, upload MP3 or WAV files. 
          Browser recordings may not play correctly on all devices.
        </Typography>
      </Alert>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Description</TableCell>
              <TableCell>Duration</TableCell>
              <TableCell>Size</TableCell>
              <TableCell>Created</TableCell>
              <TableCell>Usage</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {recordings.map((recording) => (
              <TableRow key={recording.id}>
                <TableCell>{recording.name}</TableCell>
                <TableCell>{recording.description || '-'}</TableCell>
                <TableCell>{formatDuration(recording.durationSeconds)}</TableCell>
                <TableCell>{formatFileSize(recording.fileSizeBytes)}</TableCell>
                <TableCell>
                  {new Date(recording.createdAt).toLocaleDateString()}
                </TableCell>
                <TableCell>
                  <Chip
                    label={`${recording.usageCount} campaigns`}
                    size="small"
                    color={recording.usageCount > 0 ? 'primary' : 'default'}
                  />
                </TableCell>
                <TableCell>
                  <Tooltip title={playingId === recording.id ? "Stop" : "Play"}>
                    <IconButton
                      onClick={() => playRecording(recording)}
                      color="primary"
                    >
                      {playingId === recording.id ? <StopIcon /> : <PlayIcon />}
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Edit">
                    <IconButton onClick={() => openEditDialog(recording)}>
                      <EditIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Delete">
                    <IconButton
                      onClick={() => handleDelete(recording.id)}
                      color="error"
                      disabled={recording.usageCount > 0}
                    >
                      <DeleteIcon />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {recordings.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  No voice recordings found. Upload or record one to get started.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Upload Dialog */}
      <Dialog open={uploadDialogOpen} onClose={() => setUploadDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Upload Voice Recording</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            margin="normal"
            required
          />
          <TextField
            fullWidth
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            margin="normal"
            multiline
            rows={2}
          />
          <Button
            variant="outlined"
            component="label"
            fullWidth
            sx={{ mt: 2 }}
            startIcon={<UploadIcon />}
          >
            {formData.file ? formData.file.name : 'Select Audio File'}
            <input
              type="file"
              hidden
              accept=".mp3,.wav,.m4a,.ogg,.webm"
              onChange={(e) => {
                if (e.target.files?.[0]) {
                  setFormData({ ...formData, file: e.target.files[0] });
                }
              }}
            />
          </Button>
          {uploading && <LinearProgress sx={{ mt: 2 }} />}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setUploadDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleFileUpload} variant="contained" disabled={uploading}>
            Upload
          </Button>
        </DialogActions>
      </Dialog>

      {/* Record Dialog */}
      <Dialog open={recordDialogOpen} onClose={() => !recording && setRecordDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Record Voice Message</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            margin="normal"
            required
            disabled={recording}
          />
          <TextField
            fullWidth
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            margin="normal"
            multiline
            rows={2}
            disabled={recording}
          />
          
          <Card sx={{ mt: 2, textAlign: 'center' }}>
            <CardContent>
              {recording ? (
                <>
                  <RecordIcon sx={{ fontSize: 48, color: 'error.main', animation: 'pulse 1.5s infinite' }} />
                  <Typography variant="h6" sx={{ mt: 2 }}>
                    Recording... {formatDuration(recordingTime)}
                  </Typography>
                  <Button
                    variant="contained"
                    color="error"
                    onClick={stopRecording}
                    sx={{ mt: 2 }}
                    startIcon={<StopIcon />}
                  >
                    Stop Recording
                  </Button>
                </>
              ) : (
                <>
                  <MicIcon sx={{ fontSize: 48, color: 'text.secondary' }} />
                  <Typography variant="body1" sx={{ mt: 2 }}>
                    Click the button below to start recording
                  </Typography>
                  <Button
                    variant="contained"
                    color="primary"
                    onClick={startRecording}
                    sx={{ mt: 2 }}
                    startIcon={<RecordIcon />}
                  >
                    Start Recording
                  </Button>
                </>
              )}
            </CardContent>
          </Card>
          {uploading && <LinearProgress sx={{ mt: 2 }} />}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRecordDialogOpen(false)} disabled={recording || uploading}>
            Cancel
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Voice Recording</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            label="Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            margin="normal"
            required
          />
          <TextField
            fullWidth
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            margin="normal"
            multiline
            rows={2}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleEdit} variant="contained">
            Save Changes
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default VoiceRecordings;