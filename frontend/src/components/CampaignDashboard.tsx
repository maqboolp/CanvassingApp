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
  Stack,
  Checkbox,
  ListItemText,
  OutlinedInput
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
import { ApiErrorHandler, ApiError } from '../utils/apiErrorHandler';

interface Campaign {
  id: number;
  name: string;
  message: string;
  type: number; // 0 = SMS, 1 = RoboCall
  status: number; // 0 = Draft, 1 = Scheduled, 2 = Sending, 3 = Completed, 4 = Failed, 5 = Cancelled
  scheduledTime?: string;
  createdAt: string;
  sentAt?: string;
  createdById: string;
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
  const [availableZipCodes, setAvailableZipCodes] = useState<string[]>([]);
  const [audienceCount, setAudienceCount] = useState<number>(0);

  const [newCampaign, setNewCampaign] = useState({
    name: '',
    message: '',
    type: 'SMS' as 'SMS' | 'RoboCall',
    voiceUrl: '',
    selectedZipCodes: [] as string[]
  });

  useEffect(() => {
    fetchCampaigns();
    fetchAvailableZipCodes();
  }, []);

  useEffect(() => {
    // Update audience count when ZIP codes change
    if (newCampaign.selectedZipCodes.length > 0) {
      previewAudienceCount();
    } else {
      setAudienceCount(0);
    }
  }, [newCampaign.selectedZipCodes]);

  const fetchCampaigns = async () => {
    try {
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns`
      );
      console.log('Campaigns data from API:', data);
      setCampaigns(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      setError(error instanceof ApiError ? error.message : 'Error fetching campaigns');
    } finally {
      setLoading(false);
    }
  };

  const fetchAvailableZipCodes = async () => {
    try {
      console.log('Fetching available ZIP codes...');
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/available-zipcodes`
      );
      console.log('Received ZIP codes:', data);
      setAvailableZipCodes(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        console.log('Auth error when fetching ZIP codes, user needs to login');
        return;
      }
      console.error('Failed to fetch ZIP codes:', error instanceof ApiError ? error.message : error);
      setError('Failed to load ZIP codes. Please refresh the page.');
    }
  };

  const previewAudienceCount = async () => {
    try {
      const filterZipCodes = JSON.stringify(newCampaign.selectedZipCodes);
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/preview-audience`,
        {
          method: 'POST',
          body: JSON.stringify({ filterZipCodes })
        }
      );
      setAudienceCount(data.count);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        return;
      }
      console.error('Failed to preview audience:', error instanceof ApiError ? error.message : error);
      setAudienceCount(0);
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
    
    if (newCampaign.selectedZipCodes.length === 0) {
      errors.zipCodes = 'Please select at least one ZIP code';
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
        filterZipCodes: JSON.stringify(newCampaign.selectedZipCodes),
        filterVoteFrequency: null,
        filterMinAge: null,
        filterMaxAge: null,
        filterVoterSupport: null
      };
      
      console.log('Sending campaign request:', requestBody);
      
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns`,
        {
          method: 'POST',
          body: JSON.stringify(requestBody)
        }
      );

      setSuccess('Campaign created successfully');
      setCreateDialogOpen(false);
      setNewCampaign({
        name: '', 
        message: '', 
        type: 'SMS', 
        voiceUrl: '',
        selectedZipCodes: []
      });
      setValidationErrors({});
      setAudienceCount(0);
      fetchCampaigns();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      console.log('Campaign creation failed:', error);
      setError(error instanceof ApiError ? error.message : 'Error creating campaign');
    }
  };

  const sendCampaign = async (campaignId: number) => {
    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/${campaignId}/send`,
        {
          method: 'POST'
        }
      );

      setSuccess('Campaign is being sent');
      fetchCampaigns();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      setError(error instanceof ApiError ? error.message : 'Error sending campaign');
    }
  };

  const deleteCampaign = async (campaignId: number) => {
    if (!window.confirm('Are you sure you want to delete this campaign?')) return;

    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/${campaignId}`,
        {
          method: 'DELETE'
        }
      );

      setSuccess('Campaign deleted');
      fetchCampaigns();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      setError(error instanceof ApiError ? error.message : 'Error deleting campaign');
    }
  };

  const editCampaign = (campaign: Campaign) => {
    setEditingCampaign(campaign);
    // Parse existing ZIP codes from JSON string
    let selectedZipCodes: string[] = [];
    if (campaign.filterZipCodes) {
      try {
        selectedZipCodes = JSON.parse(campaign.filterZipCodes);
      } catch {
        // If not JSON, treat as comma-separated string (legacy format)
        selectedZipCodes = campaign.filterZipCodes.split(',').map(z => z.trim()).filter(z => z);
      }
    }
    
    // Populate form with existing campaign data
    setNewCampaign({
      name: campaign.name,
      message: campaign.message,
      type: getCampaignTypeString(campaign.type) as 'SMS' | 'RoboCall',
      voiceUrl: campaign.voiceUrl || '',
      selectedZipCodes
    });
    setEditDialogOpen(true);
    // Fetch ZIP codes when dialog opens to ensure we have fresh data
    if (availableZipCodes.length === 0) {
      fetchAvailableZipCodes();
    }
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
        filterZipCodes: JSON.stringify(newCampaign.selectedZipCodes),
        filterVoteFrequency: null,
        filterMinAge: null,
        filterMaxAge: null,
        filterVoterSupport: null
      };
      
      console.log('Updating campaign:', requestBody);
      
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/${editingCampaign.id}`,
        {
          method: 'PUT',
          body: JSON.stringify(requestBody)
        }
      );

      setSuccess('Campaign updated successfully');
      setEditDialogOpen(false);
      setEditingCampaign(null);
      setNewCampaign({
        name: '', 
        message: '', 
        type: 'SMS', 
        voiceUrl: '',
        selectedZipCodes: []
      });
      setValidationErrors({});
      setAudienceCount(0);
      fetchCampaigns();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      console.log('Campaign update failed:', error);
      setError(error instanceof ApiError ? error.message : 'Error updating campaign');
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

  const canEditCampaign = (campaign: Campaign): boolean => {
    // Can only edit campaigns that are "Ready to Send" (status 0)
    if (campaign.status !== 0) return false;
    
    // SuperAdmins can edit any campaign, Admins can only edit their own
    return user.role === 'superadmin' || campaign.createdById === user.id;
  };

  const canDeleteCampaign = (campaign: Campaign): boolean => {
    // SuperAdmins can delete any campaign, Admins can only delete their own
    return user.role === 'superadmin' || campaign.createdById === user.id;
  };

  const canSendCampaign = (): boolean => {
    // Only SuperAdmins can send campaigns
    return user.role === 'superadmin';
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
          onClick={() => {
            setCreateDialogOpen(true);
            // Fetch ZIP codes when dialog opens to ensure we have fresh data
            if (availableZipCodes.length === 0) {
              fetchAvailableZipCodes();
            }
          }}
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
                {user.role === 'superadmin' && campaign.createdById !== user.id && (
                  <Chip 
                    label="Other Admin" 
                    size="small" 
                    color="info"
                    sx={{ ml: 1, height: 16, fontSize: '0.6rem' }}
                  />
                )}
                {campaign.createdById === user.id && (
                  <Chip 
                    label="Your Campaign" 
                    size="small" 
                    color="success"
                    sx={{ ml: 1, height: 16, fontSize: '0.6rem' }}
                  />
                )}
              </Typography>
            </CardContent>
            
            <CardActions>
              {campaign.status === 0 && ( // Ready to Send status
                <>
                  {canSendCampaign() && (
                    <Button
                      size="small"
                      startIcon={<SendIcon />}
                      onClick={() => sendCampaign(campaign.id)}
                      variant="contained"
                      color="primary"
                    >
                      Send Now
                    </Button>
                  )}
                  {canEditCampaign(campaign) && (
                    <Button
                      size="small"
                      startIcon={<EditIcon />}
                      onClick={() => editCampaign(campaign)}
                      variant="outlined"
                    >
                      Edit
                    </Button>
                  )}
                  {canDeleteCampaign(campaign) && (
                    <Button
                      size="small"
                      startIcon={<DeleteIcon />}
                      onClick={() => deleteCampaign(campaign.id)}
                      color="error"
                    >
                      Delete
                    </Button>
                  )}
                </>
              )}
              {/* Show a message for non-editable campaigns */}
              {campaign.status !== 0 && (
                <Box sx={{ p: 1 }}>
                  <Typography variant="caption" color="text.secondary">
                    Campaign has been sent and cannot be edited
                  </Typography>
                </Box>
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

            <Typography variant="subtitle1">Target Audience</Typography>

            <FormControl fullWidth required>
              <InputLabel>ZIP Codes</InputLabel>
              <Select
                multiple
                value={newCampaign.selectedZipCodes}
                onChange={(e) => setNewCampaign({ ...newCampaign, selectedZipCodes: e.target.value as string[] })}
                input={<OutlinedInput label="ZIP Codes" />}
                renderValue={(selected) => (
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {selected.map((value) => (
                      <Chip key={value} label={value} size="small" />
                    ))}
                  </Box>
                )}
                error={!!validationErrors.zipCodes}
              >
                {availableZipCodes.length === 0 ? (
                  <MenuItem disabled>
                    <ListItemText primary="Loading ZIP codes..." />
                  </MenuItem>
                ) : (
                  availableZipCodes.map((zipCode) => (
                    <MenuItem key={zipCode} value={zipCode}>
                      <Checkbox checked={newCampaign.selectedZipCodes.indexOf(zipCode) > -1} />
                      <ListItemText primary={zipCode} />
                    </MenuItem>
                  ))
                )}
              </Select>
              {validationErrors.zipCodes && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
                  {validationErrors.zipCodes}
                </Typography>
              )}
            </FormControl>

            {audienceCount > 0 && (
              <Alert severity="info">
                <strong>{audienceCount}</strong> voters will receive this campaign
              </Alert>
            )}
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

            <Typography variant="subtitle1">Target Audience</Typography>

            <FormControl fullWidth required>
              <InputLabel>ZIP Codes</InputLabel>
              <Select
                multiple
                value={newCampaign.selectedZipCodes}
                onChange={(e) => setNewCampaign({ ...newCampaign, selectedZipCodes: e.target.value as string[] })}
                input={<OutlinedInput label="ZIP Codes" />}
                renderValue={(selected) => (
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {selected.map((value) => (
                      <Chip key={value} label={value} size="small" />
                    ))}
                  </Box>
                )}
                error={!!validationErrors.zipCodes}
              >
                {availableZipCodes.map((zipCode) => (
                  <MenuItem key={zipCode} value={zipCode}>
                    <Checkbox checked={newCampaign.selectedZipCodes.indexOf(zipCode) > -1} />
                    <ListItemText primary={zipCode} />
                  </MenuItem>
                ))}
              </Select>
              {validationErrors.zipCodes && (
                <Typography variant="caption" color="error" sx={{ mt: 0.5 }}>
                  {validationErrors.zipCodes}
                </Typography>
              )}
            </FormControl>

            {audienceCount > 0 && (
              <Alert severity="info">
                <strong>{audienceCount}</strong> voters will receive this campaign
              </Alert>
            )}
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