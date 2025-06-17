import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  CardActions,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  IconButton,
  Stack
} from '@mui/material';
import {
  Add as AddIcon,
  Send as SendIcon,
  Schedule as ScheduleIcon,
  Cancel as CancelIcon,
  Delete as DeleteIcon,
  Edit as EditIcon
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { AuthUser } from '../types';

interface Campaign {
  id: number;
  name: string;
  message: string;
  type: number; // 0 = SMS, 1 = RoboCall
  status: number; // 0 = Draft, 1 = Scheduled, 2 = Sending, 3 = Completed, 4 = Failed, 5 = Cancelled
  scheduledTime?: string;
  createdAt: string;
  sentAt?: string;
  totalRecipients: number;
  successfulDeliveries: number;
  failedDeliveries: number;
  pendingDeliveries: number;
  voiceUrl?: string;
  filterZipCodes?: string;
  filterVoteFrequency?: number;
  filterMinAge?: number;
  filterMaxAge?: number;
  filterVoterSupport?: number;
}

interface CampaignDashboardProps {
  user: AuthUser;
}

const getCampaignTypeEnum = (value: string): number => {
  switch (value) {
    case 'SMS': return 0;
    case 'RoboCall': return 1;
    default: return 0;
  }
};

const getVoteFrequencyEnum = (value: string): number => {
  switch (value) {
    case 'NonVoter': return 0;
    case 'Infrequent': return 1;
    case 'Frequent': return 2;
    default: return 0;
  }
};

const getVoterSupportEnum = (value: string): number => {
  switch (value) {
    case 'StrongYes': return 0;
    case 'LeaningYes': return 1;
    case 'Undecided': return 2;
    case 'LeaningNo': return 3;
    case 'StrongNo': return 4;
    default: return 0;
  }
};

// Convert enum numbers back to strings for display
const getCampaignTypeString = (type: number): string => {
  switch (type) {
    case 0: return 'SMS';
    case 1: return 'RoboCall';
    default: return 'SMS';
  }
};

const getCampaignStatusString = (status: number): string => {
  switch (status) {
    case 0: return 'Ready to Send';
    case 1: return 'Scheduled';
    case 2: return 'Sending';
    case 3: return 'Completed';
    case 4: return 'Failed';
    case 5: return 'Cancelled';
    default: return 'Ready to Send';
  }
};

const CampaignDashboard: React.FC<CampaignDashboardProps> = ({ user }) => {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [editingCampaign, setEditingCampaign] = useState<Campaign | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<{[key: string]: string}>({});

  const [newCampaign, setNewCampaign] = useState({
    name: '',
    message: '',
    type: 'SMS' as 'SMS' | 'RoboCall',
    voiceUrl: '',
    filterZipCodes: '',
    filterVoteFrequency: '',
    filterMinAge: '',
    filterMaxAge: '',
    filterVoterSupport: ''
  });

  useEffect(() => {
    fetchCampaigns();
  }, []);

  const fetchCampaigns = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/campaigns`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        console.log('Campaigns data from API:', data);
        setCampaigns(data);
      } else {
        setError('Failed to fetch campaigns');
      }
    } catch (err) {
      setError('Error fetching campaigns');
    } finally {
      setLoading(false);
    }
  };

  const validateForm = () => {
    const errors: {[key: string]: string} = {};
    
    if (!newCampaign.name.trim()) {
      errors.name = 'Campaign name is required';
    }
    
    if (!newCampaign.message.trim()) {
      errors.message = newCampaign.type === 'SMS' ? 'SMS message is required' : 'Call script is required';
    }
    
    if (newCampaign.type === 'RoboCall' && !newCampaign.voiceUrl.trim()) {
      errors.voiceUrl = 'Voice URL is required for robo calls';
    }
    
    if (newCampaign.filterMinAge && newCampaign.filterMaxAge) {
      const minAge = parseInt(newCampaign.filterMinAge);
      const maxAge = parseInt(newCampaign.filterMaxAge);
      if (minAge >= maxAge) {
        errors.filterMaxAge = 'Max age must be greater than min age';
      }
    }
    
    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const createCampaign = async () => {
    if (!validateForm()) {
      return;
    }
    
    try {
      const requestBody = {
        name: newCampaign.name,
        message: newCampaign.message,
        type: getCampaignTypeEnum(newCampaign.type),
        voiceUrl: newCampaign.voiceUrl || null,
        filterZipCodes: newCampaign.filterZipCodes || null,
        filterVoteFrequency: newCampaign.filterVoteFrequency ? getVoteFrequencyEnum(newCampaign.filterVoteFrequency) : null,
        filterMinAge: newCampaign.filterMinAge ? parseInt(newCampaign.filterMinAge) : null,
        filterMaxAge: newCampaign.filterMaxAge ? parseInt(newCampaign.filterMaxAge) : null,
        filterVoterSupport: newCampaign.filterVoterSupport ? getVoterSupportEnum(newCampaign.filterVoterSupport) : null
      };
      
      console.log('Sending campaign request:', requestBody);
      
      const response = await fetch(`${API_BASE_URL}/api/campaigns`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify(requestBody)
      });

      if (response.ok) {
        setSuccess('Campaign created successfully');
        setCreateDialogOpen(false);
        setNewCampaign({
          name: '', message: '', type: 'SMS', voiceUrl: '',
          filterZipCodes: '', filterVoteFrequency: '', filterMinAge: '',
          filterMaxAge: '', filterVoterSupport: ''
        });
        setValidationErrors({});
        fetchCampaigns();
      } else {
        const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
        console.log('Campaign creation failed:', response.status, errorData);
        setError(errorData.error || `Failed to create campaign (${response.status})`);
      }
    } catch (err) {
      setError('Error creating campaign');
    }
  };

  const sendCampaign = async (campaignId: number) => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/campaigns/${campaignId}/send`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        setSuccess('Campaign is being sent');
        fetchCampaigns();
      } else {
        setError('Failed to send campaign');
      }
    } catch (err) {
      setError('Error sending campaign');
    }
  };

  const deleteCampaign = async (campaignId: number) => {
    if (!window.confirm('Are you sure you want to delete this campaign?')) return;

    try {
      const response = await fetch(`${API_BASE_URL}/api/campaigns/${campaignId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        setSuccess('Campaign deleted');
        fetchCampaigns();
      } else {
        setError('Failed to delete campaign');
      }
    } catch (err) {
      setError('Error deleting campaign');
    }
  };

  const editCampaign = (campaign: Campaign) => {
    setEditingCampaign(campaign);
    // Populate form with existing campaign data
    setNewCampaign({
      name: campaign.name,
      message: campaign.message,
      type: getCampaignTypeString(campaign.type) as 'SMS' | 'RoboCall',
      voiceUrl: campaign.voiceUrl || '',
      filterZipCodes: campaign.filterZipCodes || '',
      filterVoteFrequency: getVoteFrequencyString(campaign.filterVoteFrequency),
      filterMinAge: campaign.filterMinAge?.toString() || '',
      filterMaxAge: campaign.filterMaxAge?.toString() || '',
      filterVoterSupport: getVoterSupportString(campaign.filterVoterSupport)
    });
    setEditDialogOpen(true);
  };

  const updateCampaign = async () => {
    if (!validateForm() || !editingCampaign) {
      return;
    }
    
    try {
      const requestBody = {
        name: newCampaign.name,
        message: newCampaign.message,
        voiceUrl: newCampaign.voiceUrl || null,
        filterZipCodes: newCampaign.filterZipCodes || null,
        filterVoteFrequency: newCampaign.filterVoteFrequency ? getVoteFrequencyEnum(newCampaign.filterVoteFrequency) : null,
        filterMinAge: newCampaign.filterMinAge ? parseInt(newCampaign.filterMinAge) : null,
        filterMaxAge: newCampaign.filterMaxAge ? parseInt(newCampaign.filterMaxAge) : null,
        filterVoterSupport: newCampaign.filterVoterSupport ? getVoterSupportEnum(newCampaign.filterVoterSupport) : null
      };
      
      console.log('Updating campaign:', requestBody);
      
      const response = await fetch(`${API_BASE_URL}/api/campaigns/${editingCampaign.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify(requestBody)
      });

      if (response.ok) {
        setSuccess('Campaign updated successfully');
        setEditDialogOpen(false);
        setEditingCampaign(null);
        setNewCampaign({
          name: '', message: '', type: 'SMS', voiceUrl: '',
          filterZipCodes: '', filterVoteFrequency: '', filterMinAge: '',
          filterMaxAge: '', filterVoterSupport: ''
        });
        setValidationErrors({});
        fetchCampaigns();
      } else {
        const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
        console.log('Campaign update failed:', response.status, errorData);
        setError(errorData.error || `Failed to update campaign (${response.status})`);
      }
    } catch (err) {
      setError('Error updating campaign');
    }
  };

  const getVoteFrequencyString = (value?: number): string => {
    switch (value) {
      case 0: return 'NonVoter';
      case 1: return 'Infrequent';
      case 2: return 'Frequent';
      default: return '';
    }
  };

  const getVoterSupportString = (value?: number): string => {
    switch (value) {
      case 0: return 'StrongYes';
      case 1: return 'LeaningYes';
      case 2: return 'Undecided';
      case 3: return 'LeaningNo';
      case 4: return 'StrongNo';
      default: return '';
    }
  };

  const getStatusColor = (status: number): "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning" => {
    switch (status) {
      case 0: return 'default'; // Draft
      case 1: return 'info';    // Scheduled
      case 2: return 'warning'; // Sending
      case 3: return 'success'; // Completed
      case 4: return 'error';   // Failed
      case 5: return 'default'; // Cancelled
      default: return 'default';
    }
  };

  const getTypeColor = (type: number): "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning" => {
    return type === 0 ? 'primary' : 'secondary'; // SMS = primary, RoboCall = secondary
  };

  if (loading) {
    return <Typography>Loading campaigns...</Typography>;
  }

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Campaign Management</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setCreateDialogOpen(true)}
          sx={{ backgroundColor: '#1976d2' }}
        >
          Create Campaign
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }, gap: 3 }}>
        {campaigns.map((campaign) => (
          <Card key={campaign.id}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Typography variant="h6" component="div">
                  {campaign.name}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Box sx={{
                    px: 1.5,
                    py: 0.5,
                    borderRadius: 3,
                    backgroundColor: getTypeColor(campaign.type) === 'primary' ? '#1976d2' : '#9c27b0',
                    color: 'white',
                    fontSize: '0.75rem',
                    fontWeight: 500
                  }}>
                    {getCampaignTypeString(campaign.type)}
                  </Box>
                  <Box sx={{
                    px: 1.5,
                    py: 0.5,
                    borderRadius: 3,
                    backgroundColor: getStatusColor(campaign.status) === 'default' ? '#e0e0e0' : 
                                   getStatusColor(campaign.status) === 'info' ? '#0288d1' :
                                   getStatusColor(campaign.status) === 'warning' ? '#ed6c02' :
                                   getStatusColor(campaign.status) === 'success' ? '#2e7d32' :
                                   getStatusColor(campaign.status) === 'error' ? '#d32f2f' : '#e0e0e0',
                    color: getStatusColor(campaign.status) === 'default' ? '#424242' : 'white',
                    fontSize: '0.75rem',
                    fontWeight: 500
                  }}>
                    {getCampaignStatusString(campaign.status)}
                  </Box>
                </Box>
              </Box>
              
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                {campaign.message.length > 100 
                  ? `${campaign.message.substring(0, 100)}...` 
                  : campaign.message}
              </Typography>

              <Box sx={{ mb: 2 }}>
                <Typography variant="body2">
                  Recipients: {campaign.totalRecipients}
                </Typography>
                <Typography variant="body2">
                  Delivered: {campaign.successfulDeliveries}
                </Typography>
                <Typography variant="body2">
                  Failed: {campaign.failedDeliveries}
                </Typography>
              </Box>

              <Typography variant="caption" color="text.secondary">
                Created: {new Date(campaign.createdAt).toLocaleDateString()}
              </Typography>
            </CardContent>
            
            <CardActions>
              {campaign.status === 0 && ( // Ready to Send status
                <>
                  <Button
                    size="small"
                    startIcon={<SendIcon />}
                    onClick={() => sendCampaign(campaign.id)}
                    variant="contained"
                    color="primary"
                  >
                    Send Now
                  </Button>
                  <Button
                    size="small"
                    startIcon={<EditIcon />}
                    onClick={() => editCampaign(campaign)}
                    variant="outlined"
                  >
                    Edit
                  </Button>
                  <Button
                    size="small"
                    startIcon={<DeleteIcon />}
                    onClick={() => deleteCampaign(campaign.id)}
                    color="error"
                  >
                    Delete
                  </Button>
                </>
              )}
            </CardActions>
          </Card>
        ))}
      </Box>

      {/* Create Campaign Dialog */}
      <Dialog open={createDialogOpen} onClose={() => {
        setCreateDialogOpen(false);
        setValidationErrors({});
      }} maxWidth="md" fullWidth>
        <DialogTitle>Create New Campaign</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                autoFocus
                label="Campaign Name"
                fullWidth
                required
                value={newCampaign.name}
                onChange={(e) => setNewCampaign({ ...newCampaign, name: e.target.value })}
                error={!!validationErrors.name}
                helperText={validationErrors.name}
              />
              <FormControl fullWidth required>
                <InputLabel>Campaign Type</InputLabel>
                <Select
                  value={newCampaign.type}
                  label="Campaign Type"
                  onChange={(e) => setNewCampaign({ ...newCampaign, type: e.target.value as 'SMS' | 'RoboCall' })}
                >
                  <MenuItem value="SMS">SMS</MenuItem>
                  <MenuItem value="RoboCall">Robo Call</MenuItem>
                </Select>
              </FormControl>
            </Box>
            
            <TextField
              label={newCampaign.type === 'SMS' ? 'SMS Message' : 'Call Script'}
              fullWidth
              required
              multiline
              rows={4}
              value={newCampaign.message}
              onChange={(e) => setNewCampaign({ ...newCampaign, message: e.target.value })}
              error={!!validationErrors.message}
              helperText={validationErrors.message || `${newCampaign.message.length}/1600 characters`}
            />

            {newCampaign.type === 'RoboCall' && (
              <TextField
                label="Voice URL (TwiML endpoint)"
                fullWidth
                required
                value={newCampaign.voiceUrl}
                onChange={(e) => setNewCampaign({ ...newCampaign, voiceUrl: e.target.value })}
                error={!!validationErrors.voiceUrl}
                helperText={validationErrors.voiceUrl || "URL that returns TwiML for the voice call"}
              />
            )}

            <Typography variant="subtitle1">Audience Filters</Typography>

            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                label="ZIP Codes (comma separated)"
                fullWidth
                value={newCampaign.filterZipCodes}
                onChange={(e) => setNewCampaign({ ...newCampaign, filterZipCodes: e.target.value })}
                placeholder="35244, 35216, 35226"
              />
              <FormControl fullWidth>
                <InputLabel>Vote Frequency</InputLabel>
                <Select
                  value={newCampaign.filterVoteFrequency}
                  label="Vote Frequency"
                  onChange={(e) => setNewCampaign({ ...newCampaign, filterVoteFrequency: e.target.value })}
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="Frequent">Frequent</MenuItem>
                  <MenuItem value="Infrequent">Infrequent</MenuItem>
                  <MenuItem value="NonVoter">Non Voter</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                label="Min Age"
                type="number"
                fullWidth
                value={newCampaign.filterMinAge}
                onChange={(e) => setNewCampaign({ ...newCampaign, filterMinAge: e.target.value })}
                error={!!validationErrors.filterMinAge}
                helperText={validationErrors.filterMinAge}
              />
              <TextField
                label="Max Age"
                type="number"
                fullWidth
                value={newCampaign.filterMaxAge}
                onChange={(e) => setNewCampaign({ ...newCampaign, filterMaxAge: e.target.value })}
                error={!!validationErrors.filterMaxAge}
                helperText={validationErrors.filterMaxAge}
              />
            </Box>

            <FormControl fullWidth>
              <InputLabel>Voter Support Level</InputLabel>
              <Select
                value={newCampaign.filterVoterSupport}
                label="Voter Support Level"
                onChange={(e) => setNewCampaign({ ...newCampaign, filterVoterSupport: e.target.value })}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="StrongYes">Strong Yes</MenuItem>
                <MenuItem value="LeaningYes">Leaning Yes</MenuItem>
                <MenuItem value="Undecided">Undecided</MenuItem>
                <MenuItem value="LeaningNo">Leaning No</MenuItem>
                <MenuItem value="StrongNo">Strong No</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setCreateDialogOpen(false);
            setValidationErrors({});
          }}>Cancel</Button>
          <Button 
            onClick={createCampaign} 
            variant="contained"
          >
            Create Campaign
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Campaign Dialog */}
      <Dialog open={editDialogOpen} onClose={() => {
        setEditDialogOpen(false);
        setEditingCampaign(null);
        setValidationErrors({});
      }} maxWidth="md" fullWidth>
        <DialogTitle>Edit Campaign</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                autoFocus
                label="Campaign Name"
                fullWidth
                required
                value={newCampaign.name}
                onChange={(e) => setNewCampaign({ ...newCampaign, name: e.target.value })}
                error={!!validationErrors.name}
                helperText={validationErrors.name}
              />
              <FormControl fullWidth required>
                <InputLabel>Campaign Type</InputLabel>
                <Select
                  value={newCampaign.type}
                  label="Campaign Type"
                  disabled // Don't allow changing type when editing
                >
                  <MenuItem value="SMS">SMS</MenuItem>
                  <MenuItem value="RoboCall">Robo Call</MenuItem>
                </Select>
              </FormControl>
            </Box>
            
            <TextField
              label={newCampaign.type === 'SMS' ? 'SMS Message' : 'Call Script'}
              fullWidth
              required
              multiline
              rows={4}
              value={newCampaign.message}
              onChange={(e) => setNewCampaign({ ...newCampaign, message: e.target.value })}
              error={!!validationErrors.message}
              helperText={validationErrors.message || `${newCampaign.message.length}/1600 characters`}
            />

            {newCampaign.type === 'RoboCall' && (
              <TextField
                label="Voice URL (TwiML endpoint)"
                fullWidth
                required
                value={newCampaign.voiceUrl}
                onChange={(e) => setNewCampaign({ ...newCampaign, voiceUrl: e.target.value })}
                error={!!validationErrors.voiceUrl}
                helperText={validationErrors.voiceUrl || "URL that returns TwiML for the voice call"}
              />
            )}

            <Typography variant="subtitle1">Audience Filters</Typography>

            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                label="ZIP Codes (comma separated)"
                fullWidth
                value={newCampaign.filterZipCodes}
                onChange={(e) => setNewCampaign({ ...newCampaign, filterZipCodes: e.target.value })}
                placeholder="35244, 35216, 35226"
              />
              <FormControl fullWidth>
                <InputLabel>Vote Frequency</InputLabel>
                <Select
                  value={newCampaign.filterVoteFrequency}
                  label="Vote Frequency"
                  onChange={(e) => setNewCampaign({ ...newCampaign, filterVoteFrequency: e.target.value })}
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="Frequent">Frequent</MenuItem>
                  <MenuItem value="Infrequent">Infrequent</MenuItem>
                  <MenuItem value="NonVoter">Non Voter</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                label="Min Age"
                type="number"
                fullWidth
                value={newCampaign.filterMinAge}
                onChange={(e) => setNewCampaign({ ...newCampaign, filterMinAge: e.target.value })}
                error={!!validationErrors.filterMinAge}
                helperText={validationErrors.filterMinAge}
              />
              <TextField
                label="Max Age"
                type="number"
                fullWidth
                value={newCampaign.filterMaxAge}
                onChange={(e) => setNewCampaign({ ...newCampaign, filterMaxAge: e.target.value })}
                error={!!validationErrors.filterMaxAge}
                helperText={validationErrors.filterMaxAge}
              />
            </Box>

            <FormControl fullWidth>
              <InputLabel>Voter Support Level</InputLabel>
              <Select
                value={newCampaign.filterVoterSupport}
                label="Voter Support Level"
                onChange={(e) => setNewCampaign({ ...newCampaign, filterVoterSupport: e.target.value })}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="StrongYes">Strong Yes</MenuItem>
                <MenuItem value="LeaningYes">Leaning Yes</MenuItem>
                <MenuItem value="Undecided">Undecided</MenuItem>
                <MenuItem value="LeaningNo">Leaning No</MenuItem>
                <MenuItem value="StrongNo">Strong No</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setEditDialogOpen(false);
            setEditingCampaign(null);
            setValidationErrors({});
          }}>Cancel</Button>
          <Button 
            onClick={updateCampaign} 
            variant="contained"
          >
            Update Campaign
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default CampaignDashboard;