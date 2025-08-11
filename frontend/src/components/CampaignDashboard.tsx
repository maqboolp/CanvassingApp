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
  Stack,
  Checkbox,
  ListItemText,
  OutlinedInput,
  Autocomplete,
  FormControlLabel,
  RadioGroup,
  Radio,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Paper,
  ToggleButton,
  ToggleButtonGroup,
  Switch
} from '@mui/material';
import {
  Add as AddIcon,
  Send as SendIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  LocalOffer,
  PlayArrow as PlayIcon,
  Stop as StopIcon,
  Mic as MicIcon,
  TextFields as TextIcon,
  ContentCopy as CopyIcon,
  ViewList as ListIcon,
  ViewModule as GridIcon,
  Refresh as RefreshIcon,
  Stop as StopCircleIcon
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { AuthUser, VoterTag } from '../types';
import { ApiErrorHandler, ApiError } from '../utils/apiErrorHandler';

interface Campaign {
  id: number;
  name: string;
  message: string;
  type: number | string; // 0 = SMS, 1 = RoboCall, 2 = Email (or 'SMS', 'RoboCall', 'Email')
  status: number | string; // 0 = Draft, 1 = Scheduled, etc. (or 'Draft', 'Scheduled', etc.)
  scheduledTime?: string;
  createdAt: string;
  sentAt?: string;
  createdById: string;
  totalRecipients: number;
  successfulDeliveries: number;
  failedDeliveries: number;
  pendingDeliveries: number;
  voiceUrl?: string;
  voiceRecordingId?: number;
  emailSubject?: string;
  emailHtmlContent?: string;
  emailPlainTextContent?: string;
  filterZipCodes?: string;
  filterVoteFrequency?: number;
  filterMinAge?: number;
  filterMaxAge?: number;
  filterVoterSupport?: number;
  filterTags?: string;
  enforceCallingHours: boolean;
  startHour: number;
  endHour: number;
  includeWeekends: boolean;
  preventDuplicateMessages: boolean;
}

interface CampaignDashboardProps {
  user: AuthUser;
}

interface SendDialogState {
  open: boolean;
  campaignId: number | null;
  overrideOptIn: boolean;
}

const getCampaignTypeEnum = (value: string): number => {
  switch (value) {
    case 'SMS': return 0;
    case 'RoboCall': return 1;
    case 'Email': return 2;
    default: return 0;
  }
};


// Convert enum numbers back to strings for display
const getCampaignTypeString = (type: number | string): string => {
  if (typeof type === 'string') return type;
  switch (type) {
    case 0: return 'SMS';
    case 1: return 'RoboCall';
    case 2: return 'Email';
    default: return 'SMS';
  }
};

const getCampaignStatusString = (status: number | string, campaign?: Campaign): string => {
  // Override status display if campaign has been sent but status not updated
  const statusNum = typeof status === 'string' ? 
    {'Draft': 0, 'Scheduled': 1, 'Sending': 2, 'Completed': 3, 'Failed': 4, 'Cancelled': 5, 'Sealed': 6}[status] || 0 
    : status;
  
  if (campaign && statusNum === 0) {
    if (campaign.successfulDeliveries > 0 || campaign.failedDeliveries > 0) {
      return 'Completed';
    }
  }
  
  if (typeof status === 'string') return status;
  
  switch (statusNum) {
    case 0: return 'Ready to Send';
    case 1: return 'Scheduled';
    case 2: return 'Sending';
    case 3: return 'Completed';
    case 4: return 'Failed';
    case 5: return 'Cancelled';
    case 6: return 'Sealed';
    default: return 'Ready to Send';
  }
};

const CampaignDashboard: React.FC<CampaignDashboardProps> = ({ user }) => {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [editingCampaign, setEditingCampaign] = useState<Campaign | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<{[key: string]: string}>({});
  const [availableZipCodes, setAvailableZipCodes] = useState<string[]>([]);
  const [audienceCount, setAudienceCount] = useState<number>(0);
  const [availableTags, setAvailableTags] = useState<VoterTag[]>([]);
  const [selectedTags, setSelectedTags] = useState<VoterTag[]>([]);
  const [voiceRecordings, setVoiceRecordings] = useState<any[]>([]);
  const [voiceType, setVoiceType] = useState<'text' | 'recording'>('text');
  const [playingRecordingId, setPlayingRecordingId] = useState<number | null>(null);
  const audioRef = React.useRef<HTMLAudioElement | null>(null);
  const [sendDialog, setSendDialog] = useState<SendDialogState>({
    open: false,
    campaignId: null,
    overrideOptIn: false
  });

  const [newCampaign, setNewCampaign] = useState({
    name: '',
    message: '',
    type: 'SMS' as 'SMS' | 'RoboCall' | 'Email',
    voiceUrl: '',
    voiceRecordingId: null as number | null,
    emailSubject: '',
    emailHtmlContent: '',
    emailPlainTextContent: '',
    selectedZipCodes: [] as string[],
    selectedTagIds: [] as number[],
    // Calling hours settings
    enforceCallingHours: true,
    startHour: 9,
    endHour: 20,
    includeWeekends: false,
    // Duplicate prevention
    preventDuplicateMessages: false,
    // Scheduling
    sendNow: true,
    scheduledDate: '',
    scheduledTime: ''
  });
  
  // View mode state
  const [viewMode, setViewMode] = useState<'grid' | 'table'>('table');
  const [orderBy, setOrderBy] = useState<keyof Campaign>('createdAt');
  const [order, setOrder] = useState<'asc' | 'desc'>('desc');

  useEffect(() => {
    fetchCampaigns();
    fetchAvailableZipCodes();
    fetchAvailableTags();
    fetchVoiceRecordings();
  }, []);

  // Monitor campaigns state changes
  useEffect(() => {
    // State updates logged - remove this after debugging is complete
  }, [campaigns]);

  // Auto-refresh when there are campaigns in "Sending" state
  useEffect(() => {
    const hasSendingCampaigns = campaigns.some(c => c.status === 2); // 2 = Sending
    
    if (hasSendingCampaigns) {
      const interval = setInterval(() => {
        fetchCampaigns(true, false); // Don't show success message for auto-refresh
      }, 10000); // Refresh every 10 seconds
      
      return () => clearInterval(interval);
    }
  }, [campaigns]);

  useEffect(() => {
    // Update audience count when ZIP codes or tags change
    if (newCampaign.selectedZipCodes.length > 0 || newCampaign.selectedTagIds.length > 0) {
      previewAudienceCount();
    } else {
      setAudienceCount(0);
    }
  }, [newCampaign.selectedZipCodes, newCampaign.selectedTagIds]);

  const fetchCampaigns = async (isRefresh = false, showSuccess = true) => {
    try {
      if (isRefresh) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }
      
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns`
      );
      // Force a complete state update by creating a new array
      setCampaigns([...data]);
      
      if (isRefresh && showSuccess) {
        setSuccess('Campaigns refreshed');
      }
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      setError(error instanceof ApiError ? error.message : 'Error fetching campaigns');
    } finally {
      setLoading(false);
      setRefreshing(false);
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

  const fetchAvailableTags = async () => {
    try {
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/votertags`
      );
      setAvailableTags(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        return;
      }
      console.error('Failed to fetch tags:', error instanceof ApiError ? error.message : error);
    }
  };

  const fetchVoiceRecordings = async () => {
    try {
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voicerecordings`
      );
      setVoiceRecordings(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        return;
      }
      console.error('Failed to fetch voice recordings:', error instanceof ApiError ? error.message : error);
    }
  };

  const playRecording = (recordingId: number, url: string) => {
    if (playingRecordingId === recordingId) {
      // Stop playing
      if (audioRef.current) {
        audioRef.current.pause();
        audioRef.current = null;
      }
      setPlayingRecordingId(null);
    } else {
      // Stop any current playback
      if (audioRef.current) {
        audioRef.current.pause();
      }
      // Start new playback
      const audio = new Audio(url);
      audioRef.current = audio;
      audio.play();
      audio.onended = () => setPlayingRecordingId(null);
      setPlayingRecordingId(recordingId);
    }
  };

  const previewAudienceCount = async () => {
    try {
      const queryParams = new URLSearchParams();
      
      if (newCampaign.selectedZipCodes.length > 0) {
        queryParams.append('filterZipCodes', JSON.stringify(newCampaign.selectedZipCodes));
      }
      
      if (newCampaign.selectedTagIds.length > 0) {
        newCampaign.selectedTagIds.forEach(tagId => {
          queryParams.append('filterTagIds', tagId.toString());
        });
      }
      
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/recipient-count?${queryParams}`
      );
      setAudienceCount(data);
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
    
    if (newCampaign.type === 'SMS' || (newCampaign.type === 'RoboCall' && voiceType === 'text')) {
      if (!newCampaign.message.trim()) {
        errors.message = newCampaign.type === 'SMS' ? 'SMS message is required' : 'Call script is required';
      }
    }
    
    if (newCampaign.type === 'Email') {
      if (!newCampaign.emailSubject?.trim()) {
        errors.emailSubject = 'Email subject is required';
      }
      if (!newCampaign.emailHtmlContent?.trim()) {
        errors.emailContent = 'Email content is required';
      }
    }
    
    if (newCampaign.type === 'RoboCall' && voiceType === 'recording' && !newCampaign.voiceRecordingId) {
      errors.voiceRecording = 'Please select a voice recording';
    }
    
    
    if (newCampaign.selectedZipCodes.length === 0 && newCampaign.selectedTagIds.length === 0) {
      errors.audience = 'Please select at least one ZIP code or tag for targeting';
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
        voiceRecordingId: newCampaign.voiceRecordingId || null,
        emailSubject: newCampaign.type === 'Email' ? newCampaign.emailSubject : null,
        emailHtmlContent: newCampaign.type === 'Email' ? newCampaign.emailHtmlContent : null,
        emailPlainTextContent: newCampaign.type === 'Email' ? newCampaign.emailPlainTextContent : null,
        filterZipCodes: newCampaign.selectedZipCodes.length > 0 ? JSON.stringify(newCampaign.selectedZipCodes) : null,
        filterVoteFrequency: null,
        filterMinAge: null,
        filterMaxAge: null,
        filterVoterSupport: null,
        filterTagIds: newCampaign.selectedTagIds.length > 0 ? newCampaign.selectedTagIds : null,
        // Add calling hours settings for robocalls
        enforceCallingHours: newCampaign.type === 'RoboCall' ? newCampaign.enforceCallingHours : false,
        startHour: newCampaign.type === 'RoboCall' ? newCampaign.startHour : 9,
        endHour: newCampaign.type === 'RoboCall' ? newCampaign.endHour : 20,
        includeWeekends: newCampaign.type === 'RoboCall' ? newCampaign.includeWeekends : false,
        // Add duplicate prevention setting
        preventDuplicateMessages: newCampaign.preventDuplicateMessages
      };
      
      console.log('Sending campaign request:', requestBody);
      
      const createdCampaign = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns`,
        {
          method: 'POST',
          body: JSON.stringify(requestBody)
        }
      );

      // If scheduling for later, call the schedule endpoint
      if (!newCampaign.sendNow && newCampaign.scheduledDate && newCampaign.scheduledTime) {
        const scheduledDateTime = new Date(`${newCampaign.scheduledDate}T${newCampaign.scheduledTime}`);
        await ApiErrorHandler.makeAuthenticatedRequest(
          `${API_BASE_URL}/api/campaigns/${createdCampaign.id}/schedule`,
          {
            method: 'POST',
            body: JSON.stringify({ scheduledTime: scheduledDateTime.toISOString() })
          }
        );
        setSuccess(`Campaign scheduled for ${scheduledDateTime.toLocaleString()}`);
      } else if (newCampaign.sendNow) {
        // Send immediately
        await ApiErrorHandler.makeAuthenticatedRequest(
          `${API_BASE_URL}/api/campaigns/${createdCampaign.id}/send`,
          {
            method: 'POST',
            body: JSON.stringify({ overrideOptIn: false })
          }
        );
        setSuccess('Campaign created and sending started');
      } else {
        setSuccess('Campaign created successfully');
      }

      setCreateDialogOpen(false);
      setNewCampaign({
        name: '', 
        message: '', 
        type: 'SMS', 
        voiceUrl: '',
        voiceRecordingId: null,
        emailSubject: '',
        emailHtmlContent: '',
        emailPlainTextContent: '',
        selectedZipCodes: [],
        selectedTagIds: [],
        enforceCallingHours: true,
        startHour: 9,
        endHour: 20,
        includeWeekends: false,
        preventDuplicateMessages: false,
        sendNow: true,
        scheduledDate: '',
        scheduledTime: ''
      });
      setVoiceType('text');
      setSelectedTags([]);
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

  const sendCampaign = async () => {
    if (!sendDialog.campaignId) return;
    
    try {
      // Check if this is a retry (campaign status is completed)
      const campaign = campaigns.find(c => c.id === sendDialog.campaignId);
      const isRetry = campaign && checkCampaignStatus(campaign, 3); // Completed status
      
      const endpoint = isRetry 
        ? `${API_BASE_URL}/api/campaigns/${sendDialog.campaignId}/retry-failed`
        : `${API_BASE_URL}/api/campaigns/${sendDialog.campaignId}/send`;
      
      const requestBody: any = { overrideOptIn: sendDialog.overrideOptIn };
      
      
      await ApiErrorHandler.makeAuthenticatedRequest(
        endpoint,
        {
          method: 'POST',
          body: JSON.stringify(requestBody)
        }
      );

      setSuccess(isRetry ? 'Retrying failed messages' : 'Campaign is being sent');
      setSendDialog({ open: false, campaignId: null, overrideOptIn: false });
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

  const forceStopCampaign = async (campaignId: number) => {
    if (!window.confirm('Are you sure you want to force stop this campaign? All pending messages will be marked as failed.')) return;

    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/${campaignId}/force-stop`,
        {
          method: 'POST'
        }
      );

      setSuccess('Campaign force stopped');
      fetchCampaigns();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      setError(error instanceof ApiError ? error.message : 'Error force stopping campaign');
    }
  };

  const duplicateCampaign = async (campaignId: number) => {
    try {
      const duplicatedCampaign = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/campaigns/${campaignId}/duplicate`,
        {
          method: 'POST'
        }
      );

      setSuccess(`Campaign duplicated as "${duplicatedCampaign.name}"`);
      // Add a small delay to ensure backend has fully saved the campaign
      setTimeout(() => {
        fetchCampaigns();
      }, 500);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      setError(error instanceof ApiError ? error.message : 'Error duplicating campaign');
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
    
    // Parse existing tags from JSON string
    let selectedTagIds: number[] = [];
    if (campaign.filterTags) {
      try {
        selectedTagIds = JSON.parse(campaign.filterTags);
      } catch {
        // If parsing fails, default to empty array
        selectedTagIds = [];
      }
    }

    // Populate form with existing campaign data
    setNewCampaign({
      name: campaign.name,
      message: campaign.message,
      type: getCampaignTypeString(campaign.type) as 'SMS' | 'RoboCall' | 'Email',
      voiceUrl: campaign.voiceUrl || '',
      voiceRecordingId: campaign.voiceRecordingId || null,
      emailSubject: campaign.emailSubject || '',
      emailHtmlContent: campaign.emailHtmlContent || '',
      emailPlainTextContent: campaign.emailPlainTextContent || '',
      selectedZipCodes,
      selectedTagIds,
      // Calling hours settings (use defaults if not present)
      enforceCallingHours: (campaign as any).enforceCallingHours ?? true,
      startHour: (campaign as any).startHour ?? 9,
      endHour: (campaign as any).endHour ?? 20,
      includeWeekends: (campaign as any).includeWeekends ?? false,
      // Duplicate prevention setting (use default if not present)
      preventDuplicateMessages: (campaign as any).preventDuplicateMessages ?? false,
      // Scheduling (edit doesn't support changing schedule)
      sendNow: true,
      scheduledDate: '',
      scheduledTime: ''
    });
    
    // Set voice type based on whether a recording is selected
    setVoiceType(campaign.voiceRecordingId ? 'recording' : 'text');
    
    // Set selected tags for UI display
    const campaignTags = availableTags.filter(tag => selectedTagIds.includes(tag.id));
    setSelectedTags(campaignTags);
    
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
        voiceRecordingId: newCampaign.voiceRecordingId || null,
        emailSubject: newCampaign.type === 'Email' ? newCampaign.emailSubject : null,
        emailHtmlContent: newCampaign.type === 'Email' ? newCampaign.emailHtmlContent : null,
        emailPlainTextContent: newCampaign.type === 'Email' ? newCampaign.emailPlainTextContent : null,
        filterZipCodes: newCampaign.selectedZipCodes.length > 0 ? JSON.stringify(newCampaign.selectedZipCodes) : null,
        filterVoteFrequency: null,
        filterMinAge: null,
        filterMaxAge: null,
        filterVoterSupport: null,
        filterTagIds: newCampaign.selectedTagIds.length > 0 ? newCampaign.selectedTagIds : null,
        // Add calling hours settings for RoboCall campaigns
        ...(newCampaign.type === 'RoboCall' ? {
          enforceCallingHours: newCampaign.enforceCallingHours,
          startHour: newCampaign.startHour,
          endHour: newCampaign.endHour,
          includeWeekends: newCampaign.includeWeekends
        } : {}),
        // Add duplicate prevention setting
        preventDuplicateMessages: newCampaign.preventDuplicateMessages
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
        voiceRecordingId: null,
        emailSubject: '',
        emailHtmlContent: '',
        emailPlainTextContent: '',
        selectedZipCodes: [],
        selectedTagIds: [],
        enforceCallingHours: true,
        startHour: 9,
        endHour: 20,
        includeWeekends: false,
        preventDuplicateMessages: false,
        sendNow: true,
        scheduledDate: '',
        scheduledTime: ''
      });
      setVoiceType('text');
      setSelectedTags([]);
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

  const handleTagSelectionChange = (event: any, newValue: VoterTag[]) => {
    setSelectedTags(newValue);
    setNewCampaign(prev => ({
      ...prev,
      selectedTagIds: newValue.map(tag => tag.id)
    }));
  };

  const getStatusColor = (status: number | string): "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning" => {
    const statusNum = typeof status === 'string' ? 
      {'Draft': 0, 'Scheduled': 1, 'Sending': 2, 'Completed': 3, 'Failed': 4, 'Cancelled': 5, 'Sealed': 6}[status] || 0 
      : status;
    
    switch (statusNum) {
      case 0: return 'default'; // Draft
      case 1: return 'info';    // Scheduled
      case 2: return 'warning'; // Sending
      case 3: return 'success'; // Completed
      case 4: return 'error';   // Failed
      case 5: return 'default'; // Cancelled
      default: return 'default';
    }
  };

  const getTypeColor = (type: number | string): "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning" => {
    const isSMS = type === 0 || type === 'SMS';
    return isSMS ? 'primary' : 'secondary';
  };

  // Helper function to check campaign status regardless of whether it's a number or string
  const checkCampaignStatus = (campaign: Campaign, expectedStatus: number): boolean => {
    const statusMap: { [key: string]: number } = {
      'Draft': 0,
      'Scheduled': 1,
      'Sending': 2,
      'Completed': 3,
      'Failed': 4,
      'Cancelled': 5,
      'Sealed': 6
    };
    
    if (typeof campaign.status === 'number') {
      return campaign.status === expectedStatus;
    }
    
    if (typeof campaign.status === 'string') {
      return statusMap[campaign.status] === expectedStatus;
    }
    
    return false;
  };

  const canEditCampaign = (campaign: Campaign): boolean => {
    // Can only edit campaigns that are "Ready to Send" (status 0 or 'Draft') AND have no deliveries yet
    if (!checkCampaignStatus(campaign, 0)) return false;
    
    // If campaign has recipients and any deliveries, it cannot be edited
    // But if it has 0 recipients, it can still be edited (never actually sent)
    if (campaign.totalRecipients > 0 && (campaign.successfulDeliveries > 0 || campaign.failedDeliveries > 0)) {
      return false;
    }
    
    
    // SuperAdmins can edit any campaign
    if (user.role === 'superadmin') return true;
    
    // For duplicated campaigns created in the current session, always allow edit
    // This handles the case where user IDs might be out of sync
    if (campaign.name?.includes('(Copy)') && campaign.totalRecipients === 0) {
      return true;
    }
    
    // Regular users can only edit their own campaigns
    return campaign.createdById === user.id;
  };

  const canDeleteCampaign = (campaign: Campaign): boolean => {
    // SuperAdmins can delete any campaign, Admins can only delete their own
    return user.role === 'superadmin' || campaign.createdById === user.id;
  };

  const canSendCampaign = (): boolean => {
    // Only SuperAdmins can send campaigns
    return user.role === 'superadmin';
  };

  // Sorting functions
  const handleRequestSort = (property: keyof Campaign) => {
    const isAsc = orderBy === property && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(property);
  };

  const sortedCampaigns = [...campaigns].sort((a, b) => {
    let aValue = a[orderBy];
    let bValue = b[orderBy];
    
    // Handle null/undefined values
    if (aValue === null || aValue === undefined) return 1;
    if (bValue === null || bValue === undefined) return -1;
    
    // Compare values
    if (aValue < bValue) {
      return order === 'asc' ? -1 : 1;
    }
    if (aValue > bValue) {
      return order === 'asc' ? 1 : -1;
    }
    return 0;
  });

  if (loading) {
    return <Typography>Loading campaigns...</Typography>;
  }

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Campaign Management</Typography>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <IconButton
            onClick={() => fetchCampaigns(true)}
            disabled={refreshing}
            title="Refresh campaigns"
            sx={{ 
              animation: refreshing ? 'spin 1s linear infinite' : 'none',
              '@keyframes spin': {
                '0%': { transform: 'rotate(0deg)' },
                '100%': { transform: 'rotate(360deg)' }
              }
            }}
          >
            <RefreshIcon />
          </IconButton>
          <ToggleButtonGroup
            value={viewMode}
            exclusive
            onChange={(e, newMode) => newMode && setViewMode(newMode)}
            size="small"
          >
            <ToggleButton value="table" aria-label="table view">
              <ListIcon />
            </ToggleButton>
            <ToggleButton value="grid" aria-label="grid view">
              <GridIcon />
            </ToggleButton>
          </ToggleButtonGroup>
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

      {viewMode === 'grid' ? (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }, gap: 3 }}>
          {sortedCampaigns.map((campaign) => (
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
                    {getCampaignStatusString(campaign.status, campaign)}
                  </Box>
                </Box>
              </Box>
              
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                {campaign.message.length > 100 
                  ? `${campaign.message.substring(0, 100)}...` 
                  : campaign.message}
              </Typography>

              {/* Show scheduled time for scheduled campaigns */}
              {checkCampaignStatus(campaign, 1) && campaign.scheduledTime && (
                <Alert severity="info" sx={{ mb: 2 }}>
                  <Typography variant="caption">
                    Scheduled to start: {new Date(campaign.scheduledTime).toLocaleString()}
                  </Typography>
                </Alert>
              )}

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
              {checkCampaignStatus(campaign, 0) && ( // Ready to Send status
                <>
                  {canSendCampaign() && (
                    <Button
                      size="small"
                      startIcon={<SendIcon />}
                      onClick={() => setSendDialog({ 
                        open: true, 
                        campaignId: campaign.id, 
                        overrideOptIn: false
                      })}
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
                  <Button
                    size="small"
                    startIcon={<CopyIcon />}
                    onClick={() => duplicateCampaign(campaign.id)}
                    variant="outlined"
                    color="info"
                  >
                    Copy
                  </Button>
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
              {/* Show cancel button for scheduled campaigns */}
              {checkCampaignStatus(campaign, 1) && canSendCampaign() && (
                <Button
                  size="small"
                  variant="outlined"
                  color="error"
                  onClick={async () => {
                    if (window.confirm('Are you sure you want to cancel this scheduled campaign?')) {
                      try {
                        await ApiErrorHandler.makeAuthenticatedRequest(
                          `${API_BASE_URL}/api/campaigns/${campaign.id}/cancel`,
                          { method: 'POST' }
                        );
                        setSuccess('Campaign cancelled');
                        fetchCampaigns();
                      } catch (error) {
                        console.error('Error cancelling campaign:', error);
                        setError('Failed to cancel campaign');
                      }
                    }
                  }}
                >
                  Cancel Schedule
                </Button>
              )}
              {/* Show resume button for stuck campaigns in Sending status */}
              {checkCampaignStatus(campaign, 2) && campaign.pendingDeliveries > 0 && canSendCampaign() && (
                <>
                  <Button
                    size="small"
                    startIcon={<SendIcon />}
                    onClick={async () => {
                      try {
                        const data = await ApiErrorHandler.makeAuthenticatedRequest(
                          `${API_BASE_URL}/api/campaigns/check-stuck`,
                          {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' }
                          }
                        );
                        alert(`Resumed ${data.resumedCount} campaigns. Check the campaign status in a few moments.`);
                        fetchCampaigns();
                      } catch (error) {
                        console.error('Error resuming campaigns:', error);
                        alert('Failed to resume campaigns');
                      }
                    }}
                    variant="contained"
                    color="warning"
                  >
                    Resume Campaign
                  </Button>
                  {campaign.type === 1 && (
                    <Box sx={{ mt: 1 }}>
                      <Typography variant="caption" color="text.secondary">
                        Robocalls: 9AM-8PM (local time), Mon-Fri only
                      </Typography>
                    </Box>
                  )}
                </>
              )}
              {/* Force stop button for stuck campaigns (SuperAdmin only) */}
              {checkCampaignStatus(campaign, 2) && user.role === 'superadmin' && (
                <Button
                  size="small"
                  startIcon={<StopCircleIcon />}
                  onClick={() => forceStopCampaign(campaign.id)}
                  color="error"
                  variant="outlined"
                >
                  Force Stop
                </Button>
              )}
              {/* Show retry button for completed campaigns with failed messages */}
              {checkCampaignStatus(campaign, 3) && campaign.failedDeliveries > 0 && canSendCampaign() && (
                <Button
                  size="small"
                  startIcon={<SendIcon />}
                  onClick={() => setSendDialog({ 
                    open: true, 
                    campaignId: campaign.id, 
                    overrideOptIn: false
                  })}
                  variant="outlined"
                  color="warning"
                >
                  Retry Failed ({campaign.failedDeliveries})
                </Button>
              )}
              {/* Show a message for sealed campaigns */}
              {checkCampaignStatus(campaign, 6) && (
                <Box sx={{ p: 1 }}>
                  <Chip 
                    label="✓ All Messages Delivered" 
                    color="success" 
                    size="small" 
                    variant="outlined"
                  />
                </Box>
              )}
              {/* Show a message for other non-editable campaigns */}
              {!canEditCampaign(campaign) && checkCampaignStatus(campaign, 0) && campaign.totalRecipients > 0 && (
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
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>
                  <TableSortLabel
                    active={orderBy === 'name'}
                    direction={orderBy === 'name' ? order : 'asc'}
                    onClick={() => handleRequestSort('name')}
                  >
                    Campaign Name
                  </TableSortLabel>
                </TableCell>
                <TableCell>Type</TableCell>
                <TableCell>
                  <TableSortLabel
                    active={orderBy === 'status'}
                    direction={orderBy === 'status' ? order : 'asc'}
                    onClick={() => handleRequestSort('status')}
                  >
                    Status
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">
                  <TableSortLabel
                    active={orderBy === 'totalRecipients'}
                    direction={orderBy === 'totalRecipients' ? order : 'asc'}
                    onClick={() => handleRequestSort('totalRecipients')}
                  >
                    Recipients
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">
                  <TableSortLabel
                    active={orderBy === 'successfulDeliveries'}
                    direction={orderBy === 'successfulDeliveries' ? order : 'asc'}
                    onClick={() => handleRequestSort('successfulDeliveries')}
                  >
                    Delivered
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">
                  <TableSortLabel
                    active={orderBy === 'failedDeliveries'}
                    direction={orderBy === 'failedDeliveries' ? order : 'asc'}
                    onClick={() => handleRequestSort('failedDeliveries')}
                  >
                    Failed
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">
                  Remaining
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={orderBy === 'createdAt'}
                    direction={orderBy === 'createdAt' ? order : 'asc'}
                    onClick={() => handleRequestSort('createdAt')}
                  >
                    Created
                  </TableSortLabel>
                </TableCell>
                <TableCell>Created By</TableCell>
                <TableCell align="center">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sortedCampaigns.map((campaign) => (
                <TableRow key={campaign.id}>
                  <TableCell>
                    <Typography variant="body2" fontWeight="medium">
                      {campaign.name}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {campaign.message.length > 50 
                        ? `${campaign.message.substring(0, 50)}...` 
                        : campaign.message}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip 
                      label={getCampaignTypeString(campaign.type)} 
                      size="small"
                      color={campaign.type === 0 ? 'primary' : 'secondary'}
                    />
                  </TableCell>
                  <TableCell>
                    <Chip 
                      label={getCampaignStatusString(campaign.status, campaign)}
                      size="small"
                      color={getStatusColor(campaign.status)}
                    />
                  </TableCell>
                  <TableCell align="right">{campaign.totalRecipients}</TableCell>
                  <TableCell align="right">
                    <Typography color="success.main">{campaign.successfulDeliveries}</Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Typography color="error.main">{campaign.failedDeliveries}</Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Typography color="info.main">{campaign.totalRecipients - campaign.successfulDeliveries - campaign.failedDeliveries}</Typography>
                  </TableCell>
                  <TableCell>{new Date(campaign.createdAt).toLocaleDateString()}</TableCell>
                  <TableCell>
                    {user.role === 'superadmin' && campaign.createdById !== user.id && (
                      <Chip label="Other Admin" size="small" color="info" />
                    )}
                    {campaign.createdById === user.id && (
                      <Chip label="You" size="small" color="success" />
                    )}
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', gap: 0.5, justifyContent: 'center' }}>
                      {checkCampaignStatus(campaign, 0) && (
                        <>
                          {canSendCampaign() && (
                            <IconButton
                              size="small"
                              onClick={() => setSendDialog({ 
                                open: true, 
                                campaignId: campaign.id, 
                                overrideOptIn: false
                              })}
                              color="primary"
                              title="Send Campaign"
                            >
                              <SendIcon />
                            </IconButton>
                          )}
                          {canEditCampaign(campaign) && (
                            <IconButton
                              size="small"
                              onClick={() => editCampaign(campaign)}
                              title="Edit Campaign"
                            >
                              <EditIcon />
                            </IconButton>
                          )}
                        </>
                      )}
                      {/* Resume button for stuck campaigns */}
                      {checkCampaignStatus(campaign, 2) && campaign.pendingDeliveries > 0 && canSendCampaign() && (
                        <IconButton
                          size="small"
                          onClick={async () => {
                            try {
                              const data = await ApiErrorHandler.makeAuthenticatedRequest(
                                `${API_BASE_URL}/api/campaigns/check-stuck`,
                                {
                                  method: 'POST',
                                  headers: { 'Content-Type': 'application/json' }
                                }
                              );
                              alert(`Resumed ${data.resumedCount} campaigns. Check the campaign status in a few moments.`);
                              fetchCampaigns();
                            } catch (error) {
                              console.error('Error resuming campaigns:', error);
                              alert('Failed to resume campaigns');
                            }
                          }}
                          color="warning"
                          title="Resume Campaign"
                        >
                          <PlayIcon />
                        </IconButton>
                      )}
                      {checkCampaignStatus(campaign, 3) && campaign.failedDeliveries > 0 && canSendCampaign() && (
                        <Button
                          size="small"
                          startIcon={<SendIcon />}
                          onClick={() => setSendDialog({ 
                            open: true, 
                            campaignId: campaign.id, 
                            overrideOptIn: false
                          })}
                          variant="outlined"
                          color="warning"
                        >
                          Retry ({campaign.failedDeliveries})
                        </Button>
                      )}
                      <IconButton
                        size="small"
                        onClick={() => duplicateCampaign(campaign.id)}
                        color="info"
                        title="Duplicate Campaign"
                      >
                        <CopyIcon />
                      </IconButton>
                      {/* Force stop button for stuck campaigns (SuperAdmin only) */}
                      {checkCampaignStatus(campaign, 2) && user.role === 'superadmin' && (
                        <IconButton
                          size="small"
                          onClick={() => forceStopCampaign(campaign.id)}
                          color="error"
                          title="Force Stop Campaign"
                        >
                          <StopCircleIcon />
                        </IconButton>
                      )}
                      {canDeleteCampaign(campaign) && (
                        <IconButton
                          size="small"
                          onClick={() => deleteCampaign(campaign.id)}
                          color="error"
                          title="Delete Campaign"
                        >
                          <DeleteIcon />
                        </IconButton>
                      )}
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

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
                  onChange={(e) => setNewCampaign({ ...newCampaign, type: e.target.value as 'SMS' | 'RoboCall' | 'Email' })}
                >
                  <MenuItem value="SMS">SMS</MenuItem>
                  <MenuItem value="RoboCall">Robo Call</MenuItem>
                  <MenuItem value="Email">Email</MenuItem>
                </Select>
              </FormControl>
            </Box>
            
            {(newCampaign.type === 'SMS' || (newCampaign.type === 'RoboCall' && voiceType === 'text')) && (
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
            )}

            {newCampaign.type === 'Email' && (
              <>
                <TextField
                  label="Email Subject"
                  fullWidth
                  required
                  value={newCampaign.emailSubject}
                  onChange={(e) => setNewCampaign({ ...newCampaign, emailSubject: e.target.value })}
                  error={!!validationErrors.emailSubject}
                  helperText={validationErrors.emailSubject}
                  sx={{ mb: 2 }}
                />
                <TextField
                  label="Email Content (HTML)"
                  fullWidth
                  required
                  multiline
                  rows={6}
                  value={newCampaign.emailHtmlContent}
                  onChange={(e) => setNewCampaign({ ...newCampaign, emailHtmlContent: e.target.value })}
                  error={!!validationErrors.emailContent}
                  helperText={validationErrors.emailContent || 'You can use HTML tags for formatting'}
                  sx={{ mb: 2 }}
                />
                <TextField
                  label="Plain Text Content (Optional)"
                  fullWidth
                  multiline
                  rows={4}
                  value={newCampaign.emailPlainTextContent}
                  onChange={(e) => setNewCampaign({ ...newCampaign, emailPlainTextContent: e.target.value })}
                  helperText="Fallback for email clients that don't support HTML"
                />
              </>
            )}

            {newCampaign.type === 'RoboCall' && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Voice Type
                </Typography>
                <RadioGroup
                  value={voiceType}
                  onChange={(e) => {
                    const newVoiceType = e.target.value as 'text' | 'recording';
                    setVoiceType(newVoiceType);
                    if (newVoiceType === 'recording') {
                      setNewCampaign({ ...newCampaign, voiceRecordingId: null, message: '' });
                    } else {
                      setNewCampaign({ ...newCampaign, voiceRecordingId: null });
                    }
                  }}
                >
                  <FormControlLabel 
                    value="text" 
                    control={<Radio />} 
                    label={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <TextIcon />
                        <span>Text-to-Speech (Use call script above)</span>
                      </Box>
                    }
                  />
                  <FormControlLabel 
                    value="recording" 
                    control={<Radio />} 
                    label={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <MicIcon />
                        <span>Voice Recording</span>
                      </Box>
                    }
                  />
                </RadioGroup>

                {voiceType === 'recording' && (
                  <FormControl fullWidth sx={{ mt: 2 }} error={!!validationErrors.voiceRecording}>
                    <InputLabel>Select Voice Recording</InputLabel>
                    <Select
                      value={newCampaign.voiceRecordingId || ''}
                      label="Select Voice Recording"
                      onChange={(e) => setNewCampaign({ 
                        ...newCampaign, 
                        voiceRecordingId: e.target.value ? Number(e.target.value) : null 
                      })}
                    >
                      <MenuItem value="">
                        <em>None</em>
                      </MenuItem>
                      {voiceRecordings.map((recording) => (
                        <MenuItem key={recording.id} value={recording.id}>
                          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%' }}>
                            <Box>
                              <Typography variant="body2">{recording.name}</Typography>
                              {recording.description && (
                                <Typography variant="caption" color="text.secondary">
                                  {recording.description}
                                </Typography>
                              )}
                            </Box>
                            <IconButton
                              size="small"
                              onClick={(e) => {
                                e.stopPropagation();
                                playRecording(recording.id, recording.url);
                              }}
                            >
                              {playingRecordingId === recording.id ? <StopIcon /> : <PlayIcon />}
                            </IconButton>
                          </Box>
                        </MenuItem>
                      ))}
                    </Select>
                    {validationErrors.voiceRecording && (
                      <Typography variant="caption" color="error" sx={{ mt: 0.5, display: 'block' }}>
                        {validationErrors.voiceRecording}
                      </Typography>
                    )}
                    {voiceRecordings.length === 0 && (
                      <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
                        No voice recordings available. Go to Voice Recordings tab to create one.
                      </Typography>
                    )}
                  </FormControl>
                )}
              </Box>
            )}

            <Typography variant="subtitle1">Target Audience</Typography>

            <FormControl fullWidth>
              <InputLabel>ZIP Codes (Optional)</InputLabel>
              <Select
                multiple
                value={newCampaign.selectedZipCodes}
                onChange={(e) => setNewCampaign({ ...newCampaign, selectedZipCodes: e.target.value as string[] })}
                input={<OutlinedInput label="ZIP Codes (Optional)" />}
                renderValue={(selected) => (
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {selected.map((value) => (
                      <Chip key={value} label={value} size="small" />
                    ))}
                  </Box>
                )}
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
            </FormControl>

            <Typography variant="subtitle2" sx={{ mt: 2, mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
              <LocalOffer fontSize="small" />
              Filter by Tags (Optional)
            </Typography>
            
            <Autocomplete
              multiple
              size="small"
              options={availableTags}
              getOptionLabel={(option) => option.tagName}
              value={selectedTags}
              onChange={handleTagSelectionChange}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip
                    {...getTagProps({ index })}
                    key={option.id}
                    label={option.tagName}
                    size="small"
                    sx={{
                      backgroundColor: option.color || '#2196F3',
                      color: 'white',
                      '& .MuiChip-deleteIcon': {
                        color: 'white'
                      }
                    }}
                  />
                ))
              }
              renderOption={(props, option) => (
                <li {...props} key={option.id}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Box
                      sx={{
                        width: 12,
                        height: 12,
                        borderRadius: '50%',
                        backgroundColor: option.color || '#2196F3'
                      }}
                    />
                    {option.tagName}
                  </Box>
                </li>
              )}
              renderInput={(params) => (
                <TextField
                  {...params}
                  placeholder={selectedTags.length === 0 ? "Select tags to target specific voter groups..." : ""}
                  variant="outlined"
                  size="small"
                  helperText="Select tags to target voters with specific characteristics"
                />
              )}
              sx={{ mb: 2 }}
            />

            {validationErrors.audience && (
              <Typography variant="caption" color="error" sx={{ mb: 1, display: 'block' }}>
                {validationErrors.audience}
              </Typography>
            )}

            {/* Calling Hours Settings for RoboCall campaigns */}
            {newCampaign.type === 'RoboCall' && (
              <>
                <Typography variant="subtitle1" sx={{ mt: 2 }}>Calling Hours</Typography>
                
                <FormControlLabel
                  control={
                    <Switch
                      checked={newCampaign.enforceCallingHours}
                      onChange={(e) => setNewCampaign({ ...newCampaign, enforceCallingHours: e.target.checked })}
                    />
                  }
                  label="Enforce calling hours"
                />
                
                {newCampaign.enforceCallingHours && (
                  <Box sx={{ mt: 2, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                    <FormControl fullWidth size="small">
                      <InputLabel>Start Hour</InputLabel>
                      <Select
                        value={newCampaign.startHour}
                        label="Start Hour"
                        onChange={(e) => setNewCampaign({ ...newCampaign, startHour: Number(e.target.value) })}
                      >
                        {Array.from({ length: 24 }, (_, i) => (
                          <MenuItem key={i} value={i}>
                            {i === 0 ? '12:00 AM' : i < 12 ? `${i}:00 AM` : i === 12 ? '12:00 PM' : `${i - 12}:00 PM`}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    
                    <FormControl fullWidth size="small">
                      <InputLabel>End Hour</InputLabel>
                      <Select
                        value={newCampaign.endHour}
                        label="End Hour"
                        onChange={(e) => setNewCampaign({ ...newCampaign, endHour: Number(e.target.value) })}
                      >
                        {Array.from({ length: 24 }, (_, i) => (
                          <MenuItem key={i} value={i}>
                            {i === 0 ? '12:00 AM' : i < 12 ? `${i}:00 AM` : i === 12 ? '12:00 PM' : `${i - 12}:00 PM`}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Box>
                )}
                
                {newCampaign.enforceCallingHours && (
                  <FormControlLabel
                    sx={{ mt: 1 }}
                    control={
                      <Switch
                        checked={newCampaign.includeWeekends}
                        onChange={(e) => setNewCampaign({ ...newCampaign, includeWeekends: e.target.checked })}
                      />
                    }
                    label="Include weekends"
                  />
                )}
                
                {newCampaign.enforceCallingHours && (
                  <Alert severity="info" sx={{ mt: 1 }}>
                    <Typography variant="caption">
                      Calls will be made {newCampaign.includeWeekends ? 'every day' : 'Monday-Friday'} between{' '}
                      {newCampaign.startHour === 0 ? '12:00 AM' : newCampaign.startHour < 12 ? `${newCampaign.startHour}:00 AM` : newCampaign.startHour === 12 ? '12:00 PM' : `${newCampaign.startHour - 12}:00 PM`} and{' '}
                      {newCampaign.endHour === 0 ? '12:00 AM' : newCampaign.endHour < 12 ? `${newCampaign.endHour}:00 AM` : newCampaign.endHour === 12 ? '12:00 PM' : `${newCampaign.endHour - 12}:00 PM`} (Central Time).
                      Campaigns will automatically pause outside these hours and resume when calling hours begin.
                    </Typography>
                  </Alert>
                )}
                
                {!newCampaign.enforceCallingHours && user.role === 'superadmin' && (
                  <Alert severity="warning" sx={{ mt: 1 }}>
                    <Typography variant="caption">
                      Warning: Calling hours enforcement is disabled. Calls will be made immediately regardless of time.
                      This should only be used for testing purposes.
                    </Typography>
                  </Alert>
                )}
              </>
            )}

            {/* Duplicate Prevention Setting */}
            <Typography variant="subtitle1" sx={{ mt: 2 }}>Message Settings</Typography>
            
            <FormControlLabel
              control={
                <Switch
                  checked={newCampaign.preventDuplicateMessages}
                  onChange={(e) => setNewCampaign({ ...newCampaign, preventDuplicateMessages: e.target.checked })}
                />
              }
              label="Prevent duplicate messages"
            />
            
            {newCampaign.preventDuplicateMessages && (
              <Alert severity="info" sx={{ mt: 1 }}>
                <Typography variant="caption">
                  Voters who have already received the same {newCampaign.type === 'SMS' ? 'SMS message' : 'voice call'} will be automatically excluded from this campaign.
                </Typography>
              </Alert>
            )}

            {/* Scheduling Options */}
            <Typography variant="subtitle1" sx={{ mt: 2 }}>Campaign Timing</Typography>
            
            <RadioGroup
              value={newCampaign.sendNow ? 'now' : 'scheduled'}
              onChange={(e) => setNewCampaign({ ...newCampaign, sendNow: e.target.value === 'now' })}
            >
              <FormControlLabel value="now" control={<Radio />} label="Send immediately after creation" />
              <FormControlLabel value="scheduled" control={<Radio />} label="Schedule for later" />
            </RadioGroup>
            
            {!newCampaign.sendNow && (
              <Box sx={{ mt: 2, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                <TextField
                  label="Schedule Date"
                  type="date"
                  value={newCampaign.scheduledDate}
                  onChange={(e) => setNewCampaign({ ...newCampaign, scheduledDate: e.target.value })}
                  InputLabelProps={{ shrink: true }}
                  inputProps={{ min: new Date().toISOString().split('T')[0] }}
                  fullWidth
                  size="small"
                />
                <TextField
                  label="Schedule Time"
                  type="time"
                  value={newCampaign.scheduledTime}
                  onChange={(e) => setNewCampaign({ ...newCampaign, scheduledTime: e.target.value })}
                  InputLabelProps={{ shrink: true }}
                  fullWidth
                  size="small"
                />
              </Box>
            )}
            
            {!newCampaign.sendNow && newCampaign.scheduledDate && newCampaign.scheduledTime && (
              <Alert severity="info" sx={{ mt: 1 }}>
                <Typography variant="caption">
                  Campaign will start on {new Date(newCampaign.scheduledDate + 'T' + newCampaign.scheduledTime).toLocaleDateString('en-US', { month: 'numeric', day: 'numeric', year: 'numeric' })} at {newCampaign.scheduledTime} (local time)
                  {newCampaign.type === 'RoboCall' && newCampaign.enforceCallingHours && 
                    '. Calls will respect the configured calling hours.'}
                </Typography>
              </Alert>
            )}

            {audienceCount > 0 && (
              <Alert severity="info" sx={{ mt: 2 }}>
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
            {newCampaign.sendNow ? 'Create & Send Campaign' : 'Schedule Campaign'}
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
                  <MenuItem value="Email">Email</MenuItem>
                </Select>
              </FormControl>
            </Box>
            
            {(newCampaign.type === 'SMS' || (newCampaign.type === 'RoboCall' && voiceType === 'text')) && (
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
            )}

            {newCampaign.type === 'Email' && (
              <>
                <TextField
                  label="Email Subject"
                  fullWidth
                  required
                  value={newCampaign.emailSubject}
                  onChange={(e) => setNewCampaign({ ...newCampaign, emailSubject: e.target.value })}
                  error={!!validationErrors.emailSubject}
                  helperText={validationErrors.emailSubject}
                  sx={{ mb: 2 }}
                />
                <TextField
                  label="Email Content (HTML)"
                  fullWidth
                  required
                  multiline
                  rows={6}
                  value={newCampaign.emailHtmlContent}
                  onChange={(e) => setNewCampaign({ ...newCampaign, emailHtmlContent: e.target.value })}
                  error={!!validationErrors.emailContent}
                  helperText={validationErrors.emailContent || 'You can use HTML tags for formatting'}
                  sx={{ mb: 2 }}
                />
                <TextField
                  label="Plain Text Content (Optional)"
                  fullWidth
                  multiline
                  rows={4}
                  value={newCampaign.emailPlainTextContent}
                  onChange={(e) => setNewCampaign({ ...newCampaign, emailPlainTextContent: e.target.value })}
                  helperText="Fallback for email clients that don't support HTML"
                />
              </>
            )}

            {newCampaign.type === 'RoboCall' && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Voice Type
                </Typography>
                <RadioGroup
                  value={voiceType}
                  onChange={(e) => {
                    const newVoiceType = e.target.value as 'text' | 'recording';
                    setVoiceType(newVoiceType);
                    if (newVoiceType === 'recording') {
                      setNewCampaign({ ...newCampaign, voiceRecordingId: null, message: '' });
                    } else {
                      setNewCampaign({ ...newCampaign, voiceRecordingId: null });
                    }
                  }}
                >
                  <FormControlLabel 
                    value="text" 
                    control={<Radio />} 
                    label={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <TextIcon />
                        <span>Text-to-Speech (Use call script above)</span>
                      </Box>
                    }
                  />
                  <FormControlLabel 
                    value="recording" 
                    control={<Radio />} 
                    label={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <MicIcon />
                        <span>Voice Recording</span>
                      </Box>
                    }
                  />
                </RadioGroup>

                {voiceType === 'recording' && (
                  <FormControl fullWidth sx={{ mt: 2 }} error={!!validationErrors.voiceRecording}>
                    <InputLabel>Select Voice Recording</InputLabel>
                    <Select
                      value={newCampaign.voiceRecordingId || ''}
                      label="Select Voice Recording"
                      onChange={(e) => setNewCampaign({ 
                        ...newCampaign, 
                        voiceRecordingId: e.target.value ? Number(e.target.value) : null 
                      })}
                    >
                      <MenuItem value="">
                        <em>None</em>
                      </MenuItem>
                      {voiceRecordings.map((recording) => (
                        <MenuItem key={recording.id} value={recording.id}>
                          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%' }}>
                            <Box>
                              <Typography variant="body2">{recording.name}</Typography>
                              {recording.description && (
                                <Typography variant="caption" color="text.secondary">
                                  {recording.description}
                                </Typography>
                              )}
                            </Box>
                            <IconButton
                              size="small"
                              onClick={(e) => {
                                e.stopPropagation();
                                playRecording(recording.id, recording.url);
                              }}
                            >
                              {playingRecordingId === recording.id ? <StopIcon /> : <PlayIcon />}
                            </IconButton>
                          </Box>
                        </MenuItem>
                      ))}
                    </Select>
                    {validationErrors.voiceRecording && (
                      <Typography variant="caption" color="error" sx={{ mt: 0.5, display: 'block' }}>
                        {validationErrors.voiceRecording}
                      </Typography>
                    )}
                    {voiceRecordings.length === 0 && (
                      <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
                        No voice recordings available. Go to Voice Recordings tab to create one.
                      </Typography>
                    )}
                  </FormControl>
                )}
              </Box>
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

            <Typography variant="subtitle2" sx={{ mt: 2, mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
              <LocalOffer fontSize="small" />
              Filter by Tags (Optional)
            </Typography>
            
            <Autocomplete
              multiple
              size="small"
              options={availableTags}
              getOptionLabel={(option) => option.tagName}
              value={selectedTags}
              onChange={handleTagSelectionChange}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip
                    {...getTagProps({ index })}
                    key={option.id}
                    label={option.tagName}
                    size="small"
                    sx={{
                      backgroundColor: option.color || '#2196F3',
                      color: 'white',
                      '& .MuiChip-deleteIcon': {
                        color: 'white'
                      }
                    }}
                  />
                ))
              }
              renderOption={(props, option) => (
                <li {...props} key={option.id}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Box
                      sx={{
                        width: 12,
                        height: 12,
                        borderRadius: '50%',
                        backgroundColor: option.color || '#2196F3'
                      }}
                    />
                    {option.tagName}
                  </Box>
                </li>
              )}
              renderInput={(params) => (
                <TextField
                  {...params}
                  placeholder={selectedTags.length === 0 ? "Select tags to target specific voter groups..." : ""}
                  variant="outlined"
                  size="small"
                  helperText="Select tags to target voters with specific characteristics"
                />
              )}
              sx={{ mb: 2 }}
            />

            {/* Calling Hours Settings for RoboCall campaigns */}
            {newCampaign.type === 'RoboCall' && (
              <>
                <Typography variant="subtitle1" sx={{ mt: 2 }}>Calling Hours</Typography>
                
                <FormControlLabel
                  control={
                    <Switch
                      checked={newCampaign.enforceCallingHours}
                      onChange={(e) => setNewCampaign({ ...newCampaign, enforceCallingHours: e.target.checked })}
                    />
                  }
                  label="Enforce calling hours"
                />
                
                {newCampaign.enforceCallingHours && (
                  <Box sx={{ mt: 2, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                    <FormControl fullWidth size="small">
                      <InputLabel>Start Hour</InputLabel>
                      <Select
                        value={newCampaign.startHour}
                        label="Start Hour"
                        onChange={(e) => setNewCampaign({ ...newCampaign, startHour: Number(e.target.value) })}
                      >
                        {Array.from({ length: 24 }, (_, i) => (
                          <MenuItem key={i} value={i}>
                            {i === 0 ? '12:00 AM' : i < 12 ? `${i}:00 AM` : i === 12 ? '12:00 PM' : `${i - 12}:00 PM`}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    
                    <FormControl fullWidth size="small">
                      <InputLabel>End Hour</InputLabel>
                      <Select
                        value={newCampaign.endHour}
                        label="End Hour"
                        onChange={(e) => setNewCampaign({ ...newCampaign, endHour: Number(e.target.value) })}
                      >
                        {Array.from({ length: 24 }, (_, i) => (
                          <MenuItem key={i} value={i}>
                            {i === 0 ? '12:00 AM' : i < 12 ? `${i}:00 AM` : i === 12 ? '12:00 PM' : `${i - 12}:00 PM`}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Box>
                )}
                
                {newCampaign.enforceCallingHours && (
                  <FormControlLabel
                    sx={{ mt: 1 }}
                    control={
                      <Switch
                        checked={newCampaign.includeWeekends}
                        onChange={(e) => setNewCampaign({ ...newCampaign, includeWeekends: e.target.checked })}
                      />
                    }
                    label="Include weekends"
                  />
                )}
                
                {newCampaign.enforceCallingHours && (
                  <Alert severity="info" sx={{ mt: 1 }}>
                    <Typography variant="caption">
                      Calls will be made {newCampaign.includeWeekends ? 'every day' : 'Monday-Friday'} between{' '}
                      {newCampaign.startHour === 0 ? '12:00 AM' : newCampaign.startHour < 12 ? `${newCampaign.startHour}:00 AM` : newCampaign.startHour === 12 ? '12:00 PM' : `${newCampaign.startHour - 12}:00 PM`} and{' '}
                      {newCampaign.endHour === 0 ? '12:00 AM' : newCampaign.endHour < 12 ? `${newCampaign.endHour}:00 AM` : newCampaign.endHour === 12 ? '12:00 PM' : `${newCampaign.endHour - 12}:00 PM`} (Central Time).
                      Campaigns will automatically pause outside these hours and resume when calling hours begin.
                    </Typography>
                  </Alert>
                )}
                
                {!newCampaign.enforceCallingHours && user.role === 'superadmin' && (
                  <Alert severity="warning" sx={{ mt: 1 }}>
                    <Typography variant="caption">
                      Warning: Calling hours enforcement is disabled. Calls will be made immediately regardless of time.
                      This should only be used for testing purposes.
                    </Typography>
                  </Alert>
                )}
              </>
            )}

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

      {/* Send Campaign Confirmation Dialog */}
      <Dialog 
        open={sendDialog.open} 
        onClose={() => setSendDialog({ open: false, campaignId: null, overrideOptIn: false })}
        maxWidth="sm" 
        fullWidth
      >
        <DialogTitle>
          {campaigns.find(c => c.id === sendDialog.campaignId)?.status === 3 
            ? 'Retry Failed Messages' 
            : 'Confirm Campaign Send'
          }
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <Alert severity="warning">
              {campaigns.find(c => c.id === sendDialog.campaignId)?.status === 3 
                ? `Are you sure you want to retry sending to ${campaigns.find(c => c.id === sendDialog.campaignId)?.failedDeliveries} failed recipients?`
                : 'Are you sure you want to send this campaign? This action cannot be undone.'
              }
            </Alert>
            
            {campaigns.find(c => c.id === sendDialog.campaignId)?.type === 0 && (
              <>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={sendDialog.overrideOptIn}
                      onChange={(e) => setSendDialog({ ...sendDialog, overrideOptIn: e.target.checked })}
                      color="warning"
                    />
                  }
                  label={
                    <Box>
                      <Typography variant="body2">
                        Override opt-in status (Send to all recipients)
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        By default, SMS messages are only sent to opted-in users. Check this to send to all recipients regardless of opt-in status.
                      </Typography>
                    </Box>
                  }
                />
                
                {sendDialog.overrideOptIn && (
                  <Alert severity="error">
                    Warning: Sending messages to users who haven't opted in may violate TCPA regulations and could result in legal penalties.
                  </Alert>
                )}
              </>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button 
            onClick={() => setSendDialog({ open: false, campaignId: null, overrideOptIn: false })}
          >
            Cancel
          </Button>
          <Button 
            onClick={sendCampaign} 
            variant="contained"
            color={sendDialog.overrideOptIn ? "warning" : "primary"}
          >
            {campaigns.find(c => c.id === sendDialog.campaignId)?.status === 3 
              ? (sendDialog.overrideOptIn ? "Retry All" : "Retry Failed") 
              : (sendDialog.overrideOptIn ? "Send to All" : "Send Campaign")
            }
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default CampaignDashboard;