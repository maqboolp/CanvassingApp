import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Switch,
  FormControlLabel,
  IconButton,
  Alert,
  Chip,
  Slider,
  FormControl,
  FormLabel,
  LinearProgress,
  Tooltip
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Phone as PhoneIcon,
  Info as InfoIcon
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';

interface TwilioPhoneNumber {
  id: number;
  number: string;
  isActive: boolean;
  isMainNumber: boolean;
  maxConcurrentCalls: number;
  currentActiveCalls: number;
  createdAt: string;
  lastUsedAt?: string;
  totalCallsMade: number;
  totalCallsFailed: number;
  notes?: string;
}

export const PhoneNumberManagement: React.FC = () => {
  const [phoneNumbers, setPhoneNumbers] = useState<TwilioPhoneNumber[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedNumber, setSelectedNumber] = useState<TwilioPhoneNumber | null>(null);
  const [newNumbers, setNewNumbers] = useState('');
  const [editForm, setEditForm] = useState({ isActive: true, maxConcurrentCalls: 50 });

  useEffect(() => {
    fetchPhoneNumbers();
  }, []);

  const fetchPhoneNumbers = async () => {
    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequestRaw(
        `${API_BASE_URL}/api/phonenumberpool`
      );
      
      if (!response || !response.ok) {
        const status = response?.status || 'Network error';
        throw new Error(`HTTP error! status: ${status}`);
      }
      
      const data = await response.json();
      setPhoneNumbers(data);
    } catch (err) {
      setError('Failed to fetch phone numbers. Please ensure the database migration has been applied.');
      console.error('Phone number fetch error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAddNumbers = async () => {
    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequestRaw(
        `${API_BASE_URL}/api/phonenumberpool`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ phoneNumbers: newNumbers })
        }
      );
      
      if (!response || !response.ok) {
        const status = response?.status || 'Network error';
        throw new Error(`HTTP error! status: ${status}`);
      }
      
      setAddDialogOpen(false);
      setNewNumbers('');
      fetchPhoneNumbers();
    } catch (err) {
      setError('Failed to add phone number');
      console.error('Add phone number error:', err);
    }
  };

  const handleUpdateNumber = async () => {
    if (!selectedNumber) return;

    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequestRaw(
        `${API_BASE_URL}/api/phonenumberpool/${selectedNumber.id}`,
        {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(editForm)
        }
      );
      
      if (!response || !response.ok) {
        const status = response?.status || 'Network error';
        throw new Error(`HTTP error! status: ${status}`);
      }
      
      setEditDialogOpen(false);
      setSelectedNumber(null);
      fetchPhoneNumbers();
    } catch (err) {
      setError('Failed to update phone number');
      console.error('Update phone number error:', err);
    }
  };

  const handleToggleActive = async (number: TwilioPhoneNumber) => {
    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequestRaw(
        `${API_BASE_URL}/api/phonenumberpool/${number.id}`,
        {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            isActive: !number.isActive,
            maxConcurrentCalls: number.maxConcurrentCalls
          })
        }
      );
      
      if (!response || !response.ok) {
        const status = response?.status || 'Network error';
        throw new Error(`HTTP error! status: ${status}`);
      }
      
      fetchPhoneNumbers();
    } catch (err) {
      setError(`Failed to ${number.isActive ? 'disable' : 'enable'} phone number`);
      console.error('Toggle phone number error:', err);
    }
  };

  const handleDeleteNumber = async (id: number) => {
    if (!window.confirm('Are you sure you want to remove this phone number?')) return;

    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequestRaw(
        `${API_BASE_URL}/api/phonenumberpool/${id}`,
        { method: 'DELETE' }
      );
      
      if (!response || !response.ok) {
        const status = response?.status || 'Network error';
        throw new Error(`HTTP error! status: ${status}`);
      }
      
      fetchPhoneNumbers();
    } catch (err) {
      setError('Failed to delete phone number');
      console.error('Delete phone number error:', err);
    }
  };

  const openEditDialog = (number: TwilioPhoneNumber) => {
    setSelectedNumber(number);
    setEditForm({
      isActive: number.isActive,
      maxConcurrentCalls: number.maxConcurrentCalls
    });
    setEditDialogOpen(true);
  };

  const formatPhoneNumber = (phone: string) => {
    const cleaned = phone.replace(/\D/g, '');
    if (cleaned.length === 11 && cleaned.startsWith('1')) {
      return `+1 (${cleaned.slice(1, 4)}) ${cleaned.slice(4, 7)}-${cleaned.slice(7)}`;
    }
    return phone;
  };

  const getSuccessRate = (made: number, failed: number) => {
    if (made === 0) return '0';
    return ((made - failed) / made * 100).toFixed(1);
  };

  if (loading) return <LinearProgress />;

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Box>
          <Typography variant="h5">Additional Phone Numbers for Robocalls</Typography>
          <Typography variant="body2" color="text.secondary">
            Add extra phone numbers to speed up robocall campaigns and prevent rate limiting
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setAddDialogOpen(true)}
        >
          Add Phone Number
        </Button>
      </Box>

      {error && (
        <Alert severity="error" onClose={() => setError('')} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Card>
        <CardContent>
          <Box display="flex" alignItems="center" gap={1} mb={2}>
            <InfoIcon color="info" fontSize="small" />
            <Typography variant="body2" color="text.secondary">
              These additional numbers work alongside your primary voice number to handle more concurrent calls. Each number can handle its configured limit of simultaneous calls.
            </Typography>
          </Box>

          {phoneNumbers.length === 0 ? (
            <Box textAlign="center" py={4}>
              <PhoneIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
              <Typography variant="h6" color="text.secondary">
                No phone numbers configured
              </Typography>
              <Typography variant="body2" color="text.secondary" mb={2}>
                Add Twilio phone numbers to enable concurrent robocalls
              </Typography>
              <Button
                variant="outlined"
                startIcon={<AddIcon />}
                onClick={() => setAddDialogOpen(true)}
              >
                Add First Number
              </Button>
            </Box>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Phone Number</TableCell>
                    <TableCell>Enabled</TableCell>
                    <TableCell align="center">Active Calls</TableCell>
                    <TableCell align="center">Max Concurrent</TableCell>
                    <TableCell align="center">Total Calls</TableCell>
                    <TableCell align="center">Success Rate</TableCell>
                    <TableCell>Last Used</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {phoneNumbers.map((number) => (
                    <TableRow key={number.id}>
                      <TableCell>
                        <Box>
                          <Typography variant="body2">
                            {formatPhoneNumber(number.number)}
                          </Typography>
                          {number.isMainNumber && (
                            <Typography variant="caption" color="primary">
                              Main Number
                            </Typography>
                          )}
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Tooltip title={number.isActive ? 'Click to disable this number for calls' : 'Click to enable this number for calls'}>
                          <Switch
                            checked={number.isActive}
                            onChange={() => handleToggleActive(number)}
                            color="success"
                            size="small"
                          />
                        </Tooltip>
                      </TableCell>
                      <TableCell align="center">
                        <Box display="flex" alignItems="center" justifyContent="center" gap={1}>
                          <Typography variant="body2">
                            {number.currentActiveCalls}
                          </Typography>
                          {number.currentActiveCalls > 0 && (
                            <LinearProgress
                              variant="determinate"
                              value={(number.currentActiveCalls / number.maxConcurrentCalls) * 100}
                              sx={{ width: 50, height: 4 }}
                            />
                          )}
                        </Box>
                      </TableCell>
                      <TableCell align="center">{number.maxConcurrentCalls}</TableCell>
                      <TableCell align="center">
                        <Tooltip title={`${number.totalCallsFailed} failed`}>
                          <span>{number.totalCallsMade}</span>
                        </Tooltip>
                      </TableCell>
                      <TableCell align="center">
                        <Chip
                          label={`${getSuccessRate(number.totalCallsMade, number.totalCallsFailed)}%`}
                          size="small"
                          color={
                            parseFloat(getSuccessRate(number.totalCallsMade, number.totalCallsFailed)) > 80
                              ? 'success'
                              : parseFloat(getSuccessRate(number.totalCallsMade, number.totalCallsFailed)) > 60
                              ? 'warning'
                              : 'error'
                          }
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>
                        {number.lastUsedAt
                          ? new Date(number.lastUsedAt).toLocaleString()
                          : 'Never'
                        }
                      </TableCell>
                      <TableCell align="right">
                        <IconButton
                          size="small"
                          onClick={() => openEditDialog(number)}
                          color="primary"
                        >
                          <EditIcon />
                        </IconButton>
                        <IconButton
                          size="small"
                          onClick={() => handleDeleteNumber(number.id)}
                          color="error"
                        >
                          <DeleteIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      {/* Add Phone Numbers Dialog */}
      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Phone Numbers</DialogTitle>
        <DialogContent>
          <Box display="flex" flexDirection="column" gap={2} mt={1}>
            <TextField
              label="Phone Numbers"
              fullWidth
              multiline
              rows={4}
              value={newNumbers}
              onChange={(e) => setNewNumbers(e.target.value)}
              placeholder="+15551234567, +15559876543, +15555555555"
              helperText="Enter Twilio phone numbers separated by commas"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleAddNumbers}
            variant="contained"
            disabled={!newNumbers.trim()}
          >
            Add Numbers
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Phone Number Dialog */}
      <Dialog open={editDialogOpen} onClose={() => setEditDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Phone Number</DialogTitle>
        <DialogContent>
          <Box display="flex" flexDirection="column" gap={3} mt={2}>
            <FormControlLabel
              control={
                <Switch
                  checked={editForm.isActive}
                  onChange={(e) => setEditForm({ ...editForm, isActive: e.target.checked })}
                />
              }
              label="Active"
            />
            
            <FormControl fullWidth>
              <FormLabel>Max Concurrent Calls</FormLabel>
              <Box px={2}>
                <Slider
                  value={editForm.maxConcurrentCalls}
                  onChange={(_, value) => setEditForm({ ...editForm, maxConcurrentCalls: value as number })}
                  min={1}
                  max={50}
                  marks
                  valueLabelDisplay="auto"
                />
              </Box>
              <Typography variant="caption" color="text.secondary">
                Maximum number of simultaneous calls this number can handle
              </Typography>
            </FormControl>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleUpdateNumber} variant="contained">
            Save Changes
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};