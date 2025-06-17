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
  Delete as DeleteIcon
} from '@mui/icons-material';

interface Campaign {
  id: number;
  name: string;
  message: string;
  type: 'SMS' | 'RoboCall';
  status: 'Draft' | 'Scheduled' | 'Sending' | 'Completed' | 'Failed' | 'Cancelled';
  scheduledTime?: string;
  createdAt: string;
  sentAt?: string;
  totalRecipients: number;
  successfulDeliveries: number;
  failedDeliveries: number;
  pendingDeliveries: number;
  voiceUrl?: string;
  filterZipCodes?: string;
  filterVoteFrequency?: string;
  filterMinAge?: number;
  filterMaxAge?: number;
  filterVoterSupport?: string;
}

const CampaignDashboard: React.FC = () => {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

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
      const token = localStorage.getItem('token');
      const response = await fetch('/api/campaigns', {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        const data = await response.json();
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

  const createCampaign = async () => {
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('/api/campaigns', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          name: newCampaign.name,
          message: newCampaign.message,
          type: newCampaign.type,
          voiceUrl: newCampaign.voiceUrl || null,
          filterZipCodes: newCampaign.filterZipCodes || null,
          filterVoteFrequency: newCampaign.filterVoteFrequency || null,
          filterMinAge: newCampaign.filterMinAge ? parseInt(newCampaign.filterMinAge) : null,
          filterMaxAge: newCampaign.filterMaxAge ? parseInt(newCampaign.filterMaxAge) : null,
          filterVoterSupport: newCampaign.filterVoterSupport || null
        })
      });

      if (response.ok) {
        setSuccess('Campaign created successfully');
        setCreateDialogOpen(false);
        setNewCampaign({
          name: '', message: '', type: 'SMS', voiceUrl: '',
          filterZipCodes: '', filterVoteFrequency: '', filterMinAge: '',
          filterMaxAge: '', filterVoterSupport: ''
        });
        fetchCampaigns();
      } else {
        setError('Failed to create campaign');
      }
    } catch (err) {
      setError('Error creating campaign');
    }
  };

  const sendCampaign = async (campaignId: number) => {
    try {
      const token = localStorage.getItem('token');
      const response = await fetch(`/api/campaigns/${campaignId}/send`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
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
      const token = localStorage.getItem('token');
      const response = await fetch(`/api/campaigns/${campaignId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
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

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Draft': return 'default';
      case 'Scheduled': return 'info';
      case 'Sending': return 'warning';
      case 'Completed': return 'success';
      case 'Failed': return 'error';
      case 'Cancelled': return 'default';
      default: return 'default';
    }
  };

  const getTypeColor = (type: string) => {
    return type === 'SMS' ? 'primary' : 'secondary';
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
                <Box>
                  <Chip
                    label={campaign.type}
                    color={getTypeColor(campaign.type) as any}
                    size="small"
                    sx={{ mr: 1 }}
                  />
                  <Chip
                    label={campaign.status}
                    color={getStatusColor(campaign.status) as any}
                    size="small"
                  />
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
              {campaign.status === 'Draft' && (
                <>
                  <Button
                    size="small"
                    startIcon={<SendIcon />}
                    onClick={() => sendCampaign(campaign.id)}
                  >
                    Send Now
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
      <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Create New Campaign</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                autoFocus
                label="Campaign Name"
                fullWidth
                value={newCampaign.name}
                onChange={(e) => setNewCampaign({ ...newCampaign, name: e.target.value })}
              />
              <FormControl fullWidth>
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
              multiline
              rows={4}
              value={newCampaign.message}
              onChange={(e) => setNewCampaign({ ...newCampaign, message: e.target.value })}
              helperText={`${newCampaign.message.length}/1600 characters`}
            />

            {newCampaign.type === 'RoboCall' && (
              <TextField
                label="Voice URL (TwiML endpoint)"
                fullWidth
                value={newCampaign.voiceUrl}
                onChange={(e) => setNewCampaign({ ...newCampaign, voiceUrl: e.target.value })}
                helperText="URL that returns TwiML for the voice call"
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
              />
              <TextField
                label="Max Age"
                type="number"
                fullWidth
                value={newCampaign.filterMaxAge}
                onChange={(e) => setNewCampaign({ ...newCampaign, filterMaxAge: e.target.value })}
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
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button 
            onClick={createCampaign} 
            variant="contained"
            disabled={!newCampaign.name || !newCampaign.message}
          >
            Create Campaign
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default CampaignDashboard;