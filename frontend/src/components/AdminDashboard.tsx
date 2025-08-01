import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import VersionInfo from './VersionInfo';
import CampaignDashboard from './CampaignDashboard';
import ResourceLinksSection from './ResourceLinksSection';
import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  Box,
  Container,
  Card,
  CardContent,
  IconButton,
  Menu,
  MenuItem,
  Tab,
  Tabs,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Alert,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControlLabel,
  Checkbox,
  Chip,
  FormControl,
  InputLabel,
  Select,
  InputAdornment,
  Fab,
  useMediaQuery,
  useTheme,
  Tooltip
} from '@mui/material';
import {
  ExitToApp,
  AccountCircle,
  HowToVote,
  Upload,
  Analytics,
  People,
  GetApp,
  LocationOn,
  Lock,
  History,
  VpnKey,
  Refresh,
  ContactPhone,
  EmojiEvents,
  Star,
  SwapHoriz,
  Campaign,
  Phone,
  PhoneInTalk,
  MenuBook,
  Visibility,
  VisibilityOff,
  Email,
  LocalOffer,
  Add,
  Edit,
  Delete,
  Schedule,
  TableChart as TableIcon,
  Close as CloseIcon,
  Settings as SettingsIcon
} from '@mui/icons-material';
import { AuthUser, Voter, ContactStatus, VoterSupport, VoterTagDetail } from '../types';
import VoterList from './VoterList';
import VoterContactHistory from './VoterContactHistory';
import ContactModal from './ContactModal';
import PhoneContactModal, { PhoneContactStatus } from './PhoneContactModal';
import VoiceRecordings from './VoiceRecordings';
import VoterStagingImport from './VoterStagingImport';
import AnalyticsComponent from './Analytics';
import VolunteerResourcesSection from './VolunteerResourcesSection';
import { PhoneNumberManagement } from './PhoneNumberManagement';
import { TwilioSettings } from './TwilioSettings';
import { API_BASE_URL } from '../config';
import { customerConfig } from '../config/customerConfig';
import { ApiErrorHandler, ApiError } from '../utils/apiErrorHandler';
import { validatePassword, getPasswordHelperText, PASSWORD_REQUIREMENTS } from '../utils/passwordValidation';


interface AdminDashboardProps {
  user: AuthUser;
  onLogout: () => void;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`simple-tabpanel-${index}`}
      aria-labelledby={`simple-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: { xs: 0, sm: 2, md: 3 } }}>{children}</Box>}
    </div>
  );
}

const AdminDashboard: React.FC<AdminDashboardProps> = ({ user, onLogout }) => {
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [currentTab, setCurrentTab] = useState(0);
  
  // Dynamic tab indices based on user role
  const getTabIndex = (tabName: string) => {
    const tabs = [
      'analytics',
      'users', 
      'pending',
      'doorToDoor',
      'phoneBanking',
      'history',
      ...(user.role === 'admin' || user.role === 'superadmin' ? ['campaigns'] : []),
      ...(user.role === 'admin' || user.role === 'superadmin' ? ['voiceRecordings'] : []),
      ...(user.role === 'admin' || user.role === 'superadmin' ? ['tags'] : []),
      'resources',
      'engagement',
      ...(user.role === 'admin' || user.role === 'superadmin' ? ['dataManagement'] : []),
      ...(user.role === 'admin' || user.role === 'superadmin' ? ['settings'] : [])
    ];
    return tabs.indexOf(tabName);
  };
  const [analytics, setAnalytics] = useState<any>(null);
  const [volunteers, setVolunteers] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [importDialog, setImportDialog] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);
  const [importLoading, setImportLoading] = useState(false);
  const [importResult, setImportResult] = useState<any>(null);
  const [enableGeocoding, setEnableGeocoding] = useState(false);
  const [stagingImportDialog, setStagingImportDialog] = useState(false);
  const [createVolunteerDialog, setCreateVolunteerDialog] = useState(false);
  const [invitationForm, setInvitationForm] = useState({
    email: '',
    role: 'Volunteer'
  });
  const [adminForm, setAdminForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    password: ''
  });
  const [showAdminPassword, setShowAdminPassword] = useState(false);
  const [createAdminDialog, setCreateAdminDialog] = useState(false);
  const [adminCreateLoading, setAdminCreateLoading] = useState(false);
  const [volunteerCreateLoading, setVolunteerCreateLoading] = useState(false);
  const [volunteerCreateResult, setVolunteerCreateResult] = useState<any>(null);
  const [adminCreateResult, setAdminCreateResult] = useState<any>(null);
  const [geocodingStatus, setGeocodingStatus] = useState<any>(null);
  const [geocodingLoading, setGeocodingLoading] = useState(false);
  const [resetPasswordDialog, setResetPasswordDialog] = useState(false);
  const [selectedVolunteer, setSelectedVolunteer] = useState<any>(null);
  const [resetPasswordResult, setResetPasswordResult] = useState<any>(null);
  const [resetPasswordLoading, setResetPasswordLoading] = useState(false);
  const [useCustomPassword, setUseCustomPassword] = useState(false);
  const [customPassword, setCustomPassword] = useState('');
  const [geocodingResult, setGeocodingResult] = useState<any>(null);
  const [changePasswordDialog, setChangePasswordDialog] = useState(false);
  const [leaderboard, setLeaderboard] = useState<any>(null);
  const [leaderboardTab, setLeaderboardTab] = useState(0);
  
  // Tags state
  const [tags, setTags] = useState<VoterTagDetail[]>([]);
  const [tagDialog, setTagDialog] = useState(false);
  const [editingTag, setEditingTag] = useState<VoterTagDetail | null>(null);
  const [tagForm, setTagForm] = useState({
    tagName: '',
    description: '',
    color: '#2196F3'
  });
  const [tagLoading, setTagLoading] = useState(false);
  const [changeRoleDialog, setChangeRoleDialog] = useState(false);
  const [selectedUserForRole, setSelectedUserForRole] = useState<any>(null);
  const [newRole, setNewRole] = useState('');
  const [roleChangeLoading, setRoleChangeLoading] = useState(false);
  const [roleChangeResult, setRoleChangeResult] = useState<any>(null);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [passwordChangeLoading, setPasswordChangeLoading] = useState(false);
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [showCustomPassword, setShowCustomPassword] = useState(false);
  const [nearestVoter, setNearestVoter] = useState<{ voter: Voter; distance: number } | null>(null);
  const [location, setLocation] = useState<{ latitude: number; longitude: number } | null>(null);
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [phoneContactModalOpen, setPhoneContactModalOpen] = useState(false);
  const [selectedVoterForContact, setSelectedVoterForContact] = useState<Voter | null>(null);
  const [locationLoading, setLocationLoading] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [resourcesDialog, setResourcesDialog] = useState(false);
  const [toggleStatusLoading, setToggleStatusLoading] = useState<string | null>(null);
  const [confirmDialog, setConfirmDialog] = useState(false);
  const [actionToConfirm, setActionToConfirm] = useState<any>(null);
  
  // Engagement tab state
  const [emailSubject, setEmailSubject] = useState('');
  const [emailContent, setEmailContent] = useState('');
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [recipientType, setRecipientType] = useState('selected'); // 'selected' or 'all'
  const [emailSending, setEmailSending] = useState(false);
  const [emailResult, setEmailResult] = useState<any>(null);

  // Pending volunteers state
  const [pendingVolunteers, setPendingVolunteers] = useState<any[]>([]);
  const [pendingLoading, setPendingLoading] = useState(false);
  const [approveDialog, setApproveDialog] = useState(false);
  const [rejectDialog, setRejectDialog] = useState(false);
  const [selectedPendingVolunteer, setSelectedPendingVolunteer] = useState<any>(null);
  const [adminNotes, setAdminNotes] = useState('');
  const [approvalLoading, setApprovalLoading] = useState(false);
  const [approvalResult, setApprovalResult] = useState<any>(null);

  // Approved volunteers state
  const [approvedVolunteers, setApprovedVolunteers] = useState<any[]>([]);
  const [approvedLoading, setApprovedLoading] = useState(false);

  // Delete user state
  const [deleteDialog, setDeleteDialog] = useState(false);
  const [selectedUserForDelete, setSelectedUserForDelete] = useState<any>(null);
  const [deleteConfirmText, setDeleteConfirmText] = useState('');
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [deleteResult, setDeleteResult] = useState<any>(null);

  // Volunteer resources state
  const [volunteerResources, setVolunteerResources] = useState<{
    quickTips: string;
    script: string;
  }>({
    quickTips: '',
    script: ''
  });
  const [editResourceDialog, setEditResourceDialog] = useState(false);
  const [editingResourceType, setEditingResourceType] = useState<'quickTips' | 'script'>('quickTips');
  const [editResourceContent, setEditResourceContent] = useState('');
  const [resourceSaving, setResourceSaving] = useState(false);

  // Invitations state
  const [invitations, setInvitations] = useState<any[]>([]);
  const [invitationsLoading, setInvitationsLoading] = useState(false);
  const [invitationsDialog, setInvitationsDialog] = useState(false);
  const [resendingInvitation, setResendingInvitation] = useState<string | null>(null);

  useEffect(() => {
    if (currentTab === getTabIndex('analytics')) {
      fetchAnalytics();
      fetchLeaderboard();
    } else if (currentTab === getTabIndex('users')) {
      fetchVolunteers();
    } else if (currentTab === getTabIndex('pending')) {
      fetchPendingVolunteers();
      fetchApprovedVolunteers();
    } else if (currentTab === getTabIndex('engagement')) {
      // Engagement tab - fetch volunteers for recipient selection
      fetchVolunteers();
    } else if (currentTab === getTabIndex('tags') && (user.role === 'admin' || user.role === 'superadmin')) {
      fetchTags();
    } else if (currentTab === getTabIndex('resources')) {
      fetchVolunteerResources();
    } else if (currentTab === getTabIndex('dataManagement') && (user.role === 'admin' || user.role === 'superadmin')) {
      fetchGeocodingStatus();
    }
  }, [currentTab, user.role]);

  useEffect(() => {
    // Get location on mount for nearest voter
    getCurrentLocation();
  }, []);

  const fetchAnalytics = async () => {
    setLoading(true);
    try {
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/admin/analytics`
      );
      setAnalytics(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      console.error('Failed to fetch analytics:', error instanceof ApiError ? error.message : error);
    } finally {
      setLoading(false);
    }
  };

  const fetchLeaderboard = async () => {
    try {
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/admin/leaderboard`
      );
      setLeaderboard(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      console.error('Failed to fetch leaderboard:', error instanceof ApiError ? error.message : error);
    }
  };

  const fetchTags = async () => {
    setTagLoading(true);
    try {
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/votertags`
      );
      setTags(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        return;
      }
      console.error('Failed to fetch tags:', error instanceof ApiError ? error.message : error);
    } finally {
      setTagLoading(false);
    }
  };

  const handleCreateTag = async () => {
    if (!tagForm.tagName.trim()) {
      setImportResult({ error: 'Tag name is required' });
      return;
    }

    setTagLoading(true);
    try {
      const requestBody = {
        tagName: tagForm.tagName.trim(),
        description: tagForm.description.trim() || null,
        color: tagForm.color
      };
      
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/votertags`,
        {
          method: 'POST',
          body: JSON.stringify(requestBody)
        }
      );

      setImportResult({ success: 'Tag created successfully' });
      setTagDialog(false);
      setTagForm({ tagName: '', description: '', color: '#2196F3' });
      fetchTags();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        return;
      }
      setImportResult({ error: error instanceof ApiError ? error.message : 'Error creating tag' });
    } finally {
      setTagLoading(false);
    }
  };

  const handleUpdateTag = async () => {
    if (!editingTag || !tagForm.tagName.trim()) {
      setImportResult({ error: 'Tag name is required' });
      return;
    }

    setTagLoading(true);
    try {
      const requestBody = {
        tagName: tagForm.tagName.trim(),
        description: tagForm.description.trim() || null,
        color: tagForm.color
      };
      
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/votertags/${editingTag.id}`,
        {
          method: 'PUT',
          body: JSON.stringify(requestBody)
        }
      );

      setImportResult({ success: 'Tag updated successfully' });
      setTagDialog(false);
      setEditingTag(null);
      setTagForm({ tagName: '', description: '', color: '#2196F3' });
      fetchTags();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        return;
      }
      setImportResult({ error: error instanceof ApiError ? error.message : 'Error updating tag' });
    } finally {
      setTagLoading(false);
    }
  };

  const handleDeleteTag = async (tag: VoterTagDetail) => {
    if (!window.confirm(`Are you sure you want to delete the tag "${tag.tagName}"? This will remove the tag from all ${tag.voterCount} associated voters.`)) {
      return;
    }

    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/votertags/${tag.id}`,
        {
          method: 'DELETE'
        }
      );

      setImportResult({ success: 'Tag deleted successfully' });
      fetchTags();
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        return;
      }
      setImportResult({ error: error instanceof ApiError ? error.message : 'Error deleting tag' });
    }
  };

  const openCreateTagDialog = () => {
    setEditingTag(null);
    setTagForm({ tagName: '', description: '', color: '#2196F3' });
    setTagDialog(true);
  };

  const openEditTagDialog = (tag: VoterTagDetail) => {
    setEditingTag(tag);
    setTagForm({
      tagName: tag.tagName,
      description: tag.description || '',
      color: tag.color || '#2196F3'
    });
    setTagDialog(true);
  };

  const fetchVolunteers = async () => {
    setLoading(true);
    try {
      const data = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/admin/volunteers`
      );
      setVolunteers(data);
    } catch (error) {
      if (error instanceof ApiError && error.isAuthError) {
        // Auth error is already handled by ApiErrorHandler (user redirected to login)
        return;
      }
      console.error('Failed to fetch volunteers:', error instanceof ApiError ? error.message : error);
    } finally {
      setLoading(false);
    }
  };

  const fetchPendingVolunteers = async () => {
    setPendingLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/pending-volunteers`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setPendingVolunteers(data);
      }
    } catch (error) {
      console.error('Failed to fetch pending volunteers:', error);
    } finally {
      setPendingLoading(false);
    }
  };

  const fetchApprovedVolunteers = async () => {
    setApprovedLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/approved-volunteers`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setApprovedVolunteers(data);
      }
    } catch (error) {
      console.error('Failed to fetch approved volunteers:', error);
    } finally {
      setApprovedLoading(false);
    }
  };

  const handleApproveVolunteer = async (volunteer: any, notes?: string) => {
    setApprovalLoading(true);
    setApprovalResult(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/approve-volunteer/${volunteer.id}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({ adminNotes: notes || null })
      });

      if (response.ok) {
        const result = await response.json();
        setApprovalResult({ success: result.message });
        // Refresh both pending and approved volunteers lists
        fetchPendingVolunteers();
        fetchApprovedVolunteers();
        setApproveDialog(false);
        setAdminNotes('');
      } else {
        const error = await response.json();
        setApprovalResult({ error: error.error || 'Failed to approve volunteer' });
      }
    } catch (error) {
      setApprovalResult({ error: 'Failed to approve volunteer: ' + (error as Error).message });
    } finally {
      setApprovalLoading(false);
    }
  };

  const handleRejectVolunteer = async (volunteer: any, notes: string) => {
    if (!notes.trim()) {
      setApprovalResult({ error: 'Please provide a reason for rejection' });
      return;
    }

    setApprovalLoading(true);
    setApprovalResult(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/reject-volunteer/${volunteer.id}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({ adminNotes: notes })
      });

      if (response.ok) {
        const result = await response.json();
        setApprovalResult({ success: result.message });
        // Refresh pending volunteers list
        fetchPendingVolunteers();
        setRejectDialog(false);
        setAdminNotes('');
      } else {
        const error = await response.json();
        setApprovalResult({ error: error.error || 'Failed to reject volunteer' });
      }
    } catch (error) {
      setApprovalResult({ error: 'Failed to reject volunteer: ' + (error as Error).message });
    } finally {
      setApprovalLoading(false);
    }
  };

  const handleDeleteUser = async (userToDelete: any) => {
    // Check if confirmation text matches
    if (deleteConfirmText !== userToDelete.email) {
      setDeleteResult({ error: 'Please type the email address exactly to confirm deletion' });
      return;
    }

    setDeleteLoading(true);
    setDeleteResult(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/delete-user/${userToDelete.id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        const result = await response.json();
        setDeleteResult({ success: result.message });
        // Refresh volunteers list
        fetchVolunteers();
        setDeleteDialog(false);
        setDeleteConfirmText('');
        setSelectedUserForDelete(null);
      } else {
        const error = await response.json();
        setDeleteResult({ error: error.error || 'Failed to delete user' });
      }
    } catch (error) {
      setDeleteResult({ error: 'Failed to delete user: ' + (error as Error).message });
    } finally {
      setDeleteLoading(false);
    }
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setCurrentTab(newValue);
  };

  const handleImportVoters = async () => {
    if (!importFile) {
      alert('Please select a file first');
      return;
    }

    console.log('Importing file:', importFile.name, 'Size:', importFile.size, 'Geocoding:', enableGeocoding);
    setImportLoading(true);
    try {
      const formData = new FormData();
      formData.append('file', importFile);
      formData.append('enableGeocoding', enableGeocoding.toString());

      const response = await fetch(`${API_BASE_URL}/api/admin/import-voters`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${user.token}`
        },
        body: formData
      });

      if (response.ok) {
        const result = await response.json();
        setImportResult(result);
        setImportDialog(false);
        setImportFile(null);
        // Refresh analytics if we're on that tab
        if (currentTab === getTabIndex('analytics')) {
          fetchAnalytics();
        }
      } else {
        const error = await response.json();
        setImportResult({ error: error.message || 'Import failed' });
      }
    } catch (error) {
      setImportResult({ error: 'Import failed: ' + (error as Error).message });
    } finally {
      setImportLoading(false);
    }
  };

  const handleExportAnalytics = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/export-analytics`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = url;
        a.download = `analytics_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
      }
    } catch (error) {
      console.error('Failed to export analytics:', error);
    }
  };

  const handleSendInvitation = async () => {
    if (!invitationForm.email) {
      setVolunteerCreateResult({ error: 'Email address is required' });
      return;
    }

    setVolunteerCreateLoading(true);
    setVolunteerCreateResult(null);
    
    try {
      const result = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/registration/send-invitation`,
        {
          method: 'POST',
          body: JSON.stringify(invitationForm)
        }
      );

      setVolunteerCreateResult({ success: `Invitation sent successfully to ${invitationForm.email}!` });
      setCreateVolunteerDialog(false);
      setInvitationForm({ email: '', role: 'Volunteer' });
      
      // Refresh volunteers list if we're on that tab
      if (currentTab === getTabIndex('users')) {
        fetchVolunteers();
      }
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.isAuthError) {
          // Auth error is already handled by ApiErrorHandler (user redirected to login)
          return;
        }
        // Try to extract more specific error message
        const errorMessage = error.getDetailedMessage();
        setVolunteerCreateResult({ error: errorMessage });
      } else {
        setVolunteerCreateResult({ error: 'Failed to send invitation: ' + (error as Error).message });
      }
    } finally {
      setVolunteerCreateLoading(false);
    }
  };

  const handleCreateAdmin = async () => {
    if (!adminForm.firstName || !adminForm.lastName || !adminForm.email || !adminForm.password) {
      setAdminCreateResult({ error: 'All fields are required' });
      return;
    }

    setAdminCreateLoading(true);
    setAdminCreateResult(null);
    
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/create-admin`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify(adminForm)
      });
      
      if (response.ok) {
        const result = await response.json();
        setAdminCreateResult({ success: `Admin ${adminForm.firstName} ${adminForm.lastName} created successfully!` });
        setCreateAdminDialog(false);
        setAdminForm({ firstName: '', lastName: '', email: '', phoneNumber: '', password: '' });
        setShowAdminPassword(false);
        // Refresh the volunteers list to show the new admin
        if (currentTab === 1) {
          fetchVolunteers();
        }
      } else {
        const error = await response.json();
        setAdminCreateResult({ error: error.error || 'Failed to create admin' });
      }
    } catch (error) {
      setAdminCreateResult({ error: 'Failed to create admin: ' + (error as Error).message });
    } finally {
      setAdminCreateLoading(false);
    }
  };

  const handleChangePassword = async () => {
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setImportResult({ error: 'New passwords do not match' });
      return;
    }

    const passwordValidation = validatePassword(passwordForm.newPassword);
    if (!passwordValidation.isValid) {
      setImportResult({ error: passwordValidation.errors[0] });
      return;
    }

    setPasswordChangeLoading(true);
    setImportResult(null);
    
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/change-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          currentPassword: passwordForm.currentPassword,
          newPassword: passwordForm.newPassword
        })
      });
      
      if (response.ok) {
        setImportResult({ success: 'Password changed successfully!' });
        setChangePasswordDialog(false);
        setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      } else {
        const error = await response.json();
        setImportResult({ error: error.error || 'Failed to change password' });
      }
    } catch (error) {
      setImportResult({ error: 'Failed to change password: ' + (error as Error).message });
    } finally {
      setPasswordChangeLoading(false);
    }
  };

  const fetchGeocodingStatus = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/geocoding-status`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setGeocodingStatus(data);
      }
    } catch (error) {
      console.error('Failed to fetch geocoding status:', error);
    }
  };

  const handleDownloadSampleCsv = () => {
    const csvContent = `FirstName,LastName,AddressLine,City,State,Zip,Age,Gender,CellPhone,Email,VoteFrequency,PartyAffiliation
John,Doe,123 Main St,Birmingham,AL,35201,45,Male,+12055551234,john.doe@email.com,Frequent,Democrat
Jane,Smith,456 Oak Ave,Birmingham,AL,35202,38,Female,+12055555678,jane.smith@email.com,Infrequent,Republican
Robert,Johnson,789 Pine Rd,Birmingham,AL,35203,62,Male,,,NonVoter,Non-Partisan`;

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', 'voter_import_sample.csv');
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const handleGeocodeVoters = async () => {
    setGeocodingLoading(true);
    setGeocodingResult(null);
    
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/geocode-voters`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setGeocodingResult(data);
        // Refresh the geocoding status
        await fetchGeocodingStatus();
      } else {
        const error = await response.json();
        setGeocodingResult({ error: error.error || 'Failed to start geocoding' });
      }
    } catch (error) {
      setGeocodingResult({ error: 'Failed to start geocoding: ' + (error as Error).message });
    } finally {
      setGeocodingLoading(false);
    }
  };

  const handleResetPassword = (volunteer: any) => {
    setSelectedVolunteer(volunteer);
    setResetPasswordDialog(true);
    setResetPasswordResult(null);
    setUseCustomPassword(false);
    setCustomPassword('');
  };

  const confirmResetPassword = async () => {
    if (!selectedVolunteer) return;

    // Validate custom password if using one
    if (useCustomPassword) {
      if (customPassword.length < 6) {
        setResetPasswordResult({
          success: false,
          error: 'Password must be at least 6 characters long'
        });
        return;
      }
      
      if (!/\d/.test(customPassword)) {
        setResetPasswordResult({
          success: false,
          error: 'Password must contain at least one digit'
        });
        return;
      }
      
      if (!/[a-z]/.test(customPassword)) {
        setResetPasswordResult({
          success: false,
          error: 'Password must contain at least one lowercase letter'
        });
        return;
      }
    }

    setResetPasswordLoading(true);
    setResetPasswordResult(null);

    try {
      const requestBody = {
        volunteerId: selectedVolunteer.id,
        customPassword: useCustomPassword ? customPassword : null
      };
      
      console.log('Sending password reset request:', {
        useCustomPassword,
        customPasswordLength: customPassword?.length,
        customPasswordValue: customPassword,
        requestBody
      });
      
      const response = await fetch(`${API_BASE_URL}/api/admin/reset-volunteer-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify(requestBody)
      });

      const data = await response.json();

      if (response.ok) {
        setResetPasswordResult({
          success: true,
          message: data.message,
          temporaryPassword: data.temporaryPassword,
          volunteerEmail: data.volunteerEmail
        });
      } else {
        setResetPasswordResult({
          success: false,
          error: data.error || 'Failed to reset password'
        });
      }
    } catch (error) {
      setResetPasswordResult({
        success: false,
        error: 'Network error occurred'
      });
    } finally {
      setResetPasswordLoading(false);
    }
  };

  const handleChangeRole = (volunteer: any) => {
    setSelectedUserForRole(volunteer);
    setNewRole(volunteer.role.toLowerCase());
    setChangeRoleDialog(true);
    setRoleChangeResult(null);
  };

  const confirmRoleChange = async () => {
    if (!selectedUserForRole || !newRole) return;

    setRoleChangeLoading(true);
    setRoleChangeResult(null);

    try {
      const roleMapping: { [key: string]: number } = {
        'volunteer': 0,
        'admin': 1,
        'superadmin': 2
      };

      const response = await fetch(`${API_BASE_URL}/api/admin/change-user-role`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          userId: selectedUserForRole.id,
          newRole: roleMapping[newRole]
        })
      });

      const data = await response.json();

      if (response.ok) {
        setRoleChangeResult({
          success: true,
          message: data.message,
          oldRole: data.oldRole,
          newRole: data.newRole,
          userName: data.userName
        });
        // Refresh the volunteers list
        if (currentTab === 1) {
          fetchVolunteers();
        }
      } else {
        setRoleChangeResult({
          success: false,
          error: data.error || 'Failed to change role'
        });
      }
    } catch (error) {
      setRoleChangeResult({
        success: false,
        error: 'Network error occurred'
      });
    } finally {
      setRoleChangeLoading(false);
    }
  };

  const getCurrentLocation = () => {
    console.log('AdminDashboard: Getting current location for user role:', user.role);
    setLocationLoading(true);
    setLocationError(null);
    
    if (!navigator.geolocation) {
      setLocationError('Geolocation is not supported by this browser');
      setLocationLoading(false);
      console.log('AdminDashboard: Geolocation not supported');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const coords = {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude
        };
        console.log('AdminDashboard: Got location coordinates:', coords);
        setLocation(coords);
        setLocationLoading(false);
        findNearestVoter(coords);
      },
      (error) => {
        const errorMessages = {
          1: 'Location access denied. Please enable location permissions in your browser.',
          2: 'Location information unavailable. Please check your connection and try again.',
          3: 'Location request timed out. Please try again.',
        };
        
        const errorMessage = errorMessages[error.code as keyof typeof errorMessages] || 'Unknown location error occurred';
        setLocationError(errorMessage);
        setLocationLoading(false);
        
        console.log('AdminDashboard: Geolocation error details:', {
          code: error.code,
          message: error.message,
          errorName: error.code === 1 ? 'PERMISSION_DENIED' : 
                    error.code === 2 ? 'POSITION_UNAVAILABLE' : 
                    error.code === 3 ? 'TIMEOUT' : 'UNKNOWN'
        });
      },
      {
        enableHighAccuracy: false,
        timeout: 30000, // Increased to 30 seconds
        maximumAge: 300000 // 5 minutes
      }
    );
  };

  const findNearestVoter = async (coords: { latitude: number; longitude: number }) => {
    try {
      console.log('AdminDashboard: Finding nearest voter for coords:', coords, 'user role:', user.role);
      const response = await fetch(
        `${API_BASE_URL}/api/voters/nearest?latitude=${coords.latitude}&longitude=${coords.longitude}`,
        {
          headers: {
            'Authorization': `Bearer ${user.token}`
          }
        }
      );
      
      console.log('AdminDashboard: Nearest voter API response status:', response.status);
      
      if (response.ok) {
        const data = await response.json();
        console.log('AdminDashboard: Nearest voter data received:', data);
        setNearestVoter(data);
      } else if (response.status === 404) {
        console.log('AdminDashboard: No nearest voter found (404)');
        setNearestVoter(null);
      } else {
        console.log('AdminDashboard: Nearest voter API error:', response.status, await response.text());
      }
    } catch (error) {
      console.error('AdminDashboard: Failed to find nearest voter:', error);
    }
  };

  const openInMaps = (voter: Voter) => {
    const address = `${voter.addressLine}, ${voter.city}, ${voter.state} ${voter.zip}`;
    const encodedAddress = encodeURIComponent(address);
    
    const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent);
    const isSafari = /^((?!chrome|android).)*safari/i.test(navigator.userAgent);
    
    let mapUrl;
    if (isIOS || isSafari) {
      mapUrl = `maps://maps.apple.com/?q=${encodedAddress}`;
    } else {
      mapUrl = `https://www.google.com/maps/search/?api=1&query=${encodedAddress}`;
    }
    
    window.open(mapUrl, '_blank');
  };

  const handleContactVoter = (voter: Voter) => {
    setSelectedVoterForContact(voter);
    if (currentTab === getTabIndex('phoneBanking')) {
      setPhoneContactModalOpen(true);
    } else {
      setContactModalOpen(true);
    }
  };

  const handleNearestVoterContact = (voter: Voter) => {
    setSelectedVoterForContact(voter);
    if (currentTab === getTabIndex('phoneBanking')) {
      setPhoneContactModalOpen(true);
    } else {
      setContactModalOpen(true);
    }
  };

  const handleContactSubmit = async (status: ContactStatus, notes: string, voterSupport?: VoterSupport, audioUrl?: string, audioDuration?: number, photoUrl?: string) => {
    if (!selectedVoterForContact) return;

    try {
      const response = await fetch(`${API_BASE_URL}/api/contacts`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          voterId: selectedVoterForContact.lalVoterId,
          status,
          voterSupport,
          notes,
          audioFileUrl: audioUrl,
          audioDurationSeconds: audioDuration,
          photoUrl: photoUrl,
          location: location
        })
      });

      if (!response.ok) {
        throw new Error('Failed to log contact');
      }

      setContactModalOpen(false);
      setSelectedVoterForContact(null);
      
      // Refresh stats and find new nearest voter
      fetchAnalytics();
      if (location) {
        findNearestVoter(location);
      }
    } catch (err) {
      console.error('Failed to log contact:', err);
    }
  };

  const handlePhoneContactSubmit = async (
    status: PhoneContactStatus, 
    notes: string, 
    voterSupport?: VoterSupport, 
    callDuration?: number,
    audioUrl?: string, 
    audioDuration?: number
  ) => {
    if (!selectedVoterForContact) return;

    try {
      await ApiErrorHandler.makeAuthenticatedRequest(`${API_BASE_URL}/api/phonecontacts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          voterId: selectedVoterForContact.lalVoterId,
          status,
          voterSupport,
          notes,
          audioFileUrl: audioUrl,
          audioDurationSeconds: audioDuration,
          callDurationSeconds: callDuration
        })
      });

      setPhoneContactModalOpen(false);
      setSelectedVoterForContact(null);
      
      // Refresh stats
      fetchAnalytics();
    } catch (err) {
      console.error('Failed to save phone contact:', err);
    }
  };

  const fetchVolunteerResources = async () => {
    try {
      const [quickTipsResponse, scriptResponse] = await Promise.all([
        fetch(`${API_BASE_URL}/api/volunteerresources/QuickTips`, {
          headers: { 'Authorization': `Bearer ${user.token}` }
        }),
        fetch(`${API_BASE_URL}/api/volunteerresources/Script`, {
          headers: { 'Authorization': `Bearer ${user.token}` }
        })
      ]);

      if (quickTipsResponse.ok && scriptResponse.ok) {
        const [quickTipsData, scriptData] = await Promise.all([
          quickTipsResponse.json(),
          scriptResponse.json()
        ]);

        setVolunteerResources({
          quickTips: quickTipsData.content,
          script: scriptData.content
        });
      }
    } catch (error) {
      console.error('Failed to fetch volunteer resources:', error);
    }
  };

  const handleEditResource = (resourceType: 'quickTips' | 'script') => {
    setEditingResourceType(resourceType);
    setEditResourceContent(volunteerResources[resourceType]);
    setEditResourceDialog(true);
  };

  const handleSaveResource = async () => {
    setResourceSaving(true);
    try {
      const apiResourceType = editingResourceType === 'quickTips' ? 'QuickTips' : 'Script';
      const response = await fetch(`${API_BASE_URL}/api/volunteerresources/${apiResourceType}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          content: editResourceContent
        })
      });

      if (response.ok) {
        setVolunteerResources(prev => ({
          ...prev,
          [editingResourceType]: editResourceContent
        }));
        setEditResourceDialog(false);
      }
    } catch (error) {
      console.error('Failed to save resource:', error);
    } finally {
      setResourceSaving(false);
    }
  };

  const fetchInvitations = async () => {
    setInvitationsLoading(true);
    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/registration/invitations`
      );
      setInvitations(response);
    } catch (error) {
      console.error('Failed to fetch invitations:', error);
    } finally {
      setInvitationsLoading(false);
    }
  };

  const handleResendInvitation = async (invitationId: string) => {
    setResendingInvitation(invitationId);
    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/registration/resend-invitation/${invitationId}`,
        { method: 'POST' }
      );
      // Show success message
      setVolunteerCreateResult({ success: 'Invitation resent successfully!' });
    } catch (error) {
      if (error instanceof ApiError) {
        setVolunteerCreateResult({ error: error.getDetailedMessage() });
      } else {
        setVolunteerCreateResult({ error: 'Failed to resend invitation' });
      }
    } finally {
      setResendingInvitation(null);
    }
  };

  const handleDeleteInvitation = async (invitationId: string, email: string) => {
    if (!window.confirm(`Are you sure you want to delete the invitation for ${email}? This will allow you to send a new invitation to this email.`)) {
      return;
    }
    
    try {
      await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/registration/invitation/${invitationId}`,
        { method: 'DELETE' }
      );
      // Refresh the invitations list
      fetchInvitations();
      setVolunteerCreateResult({ success: `Invitation for ${email} deleted successfully. You can now send a new invitation.` });
    } catch (error) {
      if (error instanceof ApiError) {
        setVolunteerCreateResult({ error: error.getDetailedMessage() });
      } else {
        setVolunteerCreateResult({ error: 'Failed to delete invitation' });
      }
    }
  };

  const handleSendEngagementEmail = async () => {
    if (!emailSubject.trim() || !emailContent.trim()) {
      setEmailResult({ error: 'Subject and content are required' });
      return;
    }

    if (recipientType === 'selected' && selectedUsers.length === 0) {
      setEmailResult({ error: 'Please select at least one recipient' });
      return;
    }

    setEmailSending(true);
    setEmailResult(null);

    try {
      const requestBody = {
        subject: emailSubject,
        content: emailContent,
        recipientType,
        selectedUserIds: recipientType === 'selected' ? selectedUsers : []
      };

      const response = await fetch(`${API_BASE_URL}/api/admin/send-engagement-email`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify(requestBody)
      });

      if (response.ok) {
        const result = await response.json();
        setEmailResult({ 
          success: `Email sent successfully to ${result.recipientCount} recipient(s)` 
        });
        // Clear form on success
        setEmailSubject('');
        setEmailContent('');
        setSelectedUsers([]);
      } else {
        const error = await response.json();
        setEmailResult({ error: error.error || 'Failed to send email' });
      }
    } catch (error) {
      setEmailResult({ error: 'Failed to send email: ' + (error as Error).message });
    } finally {
      setEmailSending(false);
    }
  };

  const handleToggleUserStatus = async (targetUser: any) => {
    const action = targetUser.isActive ? 'deactivate' : 'activate';
    const actionText = targetUser.isActive ? 'Deactivate' : 'Activate';
    
    setActionToConfirm({
      type: 'toggle-status',
      user: targetUser,
      action,
      actionText,
      message: `Are you sure you want to ${action} ${targetUser.firstName} ${targetUser.lastName}?`
    });
    setConfirmDialog(true);
  };

  const executeToggleStatus = async (targetUser: any) => {
    setToggleStatusLoading(targetUser.id);
    
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/toggle-user-status`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          userId: targetUser.id
        })
      });
      
      if (response.ok) {
        const result = await response.json();
        // Refresh volunteers list to show updated status
        await fetchVolunteers();
        
        // Show success message
        const action = targetUser.isActive ? 'deactivated' : 'activated';
        alert(`User ${targetUser.firstName} ${targetUser.lastName} has been ${action} successfully.`);
      } else {
        const error = await response.json();
        alert(`Failed to toggle user status: ${error.error || 'Unknown error'}`);
      }
    } catch (error) {
      alert(`Failed to toggle user status: ${(error as Error).message}`);
    } finally {
      setToggleStatusLoading(null);
    }
  };

  const handleUserSelection = (userId: string) => {
    setSelectedUsers(prev => {
      if (prev.includes(userId)) {
        return prev.filter(id => id !== userId);
      } else {
        return [...prev, userId];
      }
    });
  };

  const handleSelectAllUsers = () => {
    if (selectedUsers.length === volunteers.length) {
      setSelectedUsers([]);
    } else {
      setSelectedUsers(volunteers.map(v => v.id));
    }
  };

  return (
    <Box sx={{ flexGrow: 1 }}>
      {/* App Bar */}
      <AppBar position="static">
        <Toolbar>
          <img 
            src={customerConfig.logoUrl} 
            alt={customerConfig.logoAlt} 
            style={{ 
              height: '40px', 
              marginRight: '16px'
            }} 
          />
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h6" component="div" sx={{ color: 'white' }}>
              Admin Portal
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.8, color: 'white' }}>
              Logged in as {user.firstName} {user.lastName} ({user.role === 'superadmin' ? 'Super Admin' : 'Admin'})
            </Typography>
          </Box>
          
          <IconButton
            size="large"
            aria-label="account of current user"
            aria-controls="menu-appbar"
            aria-haspopup="true"
            onClick={handleMenuOpen}
            color="inherit"
          >
            <AccountCircle />
          </IconButton>
          
          <Menu
            id="menu-appbar"
            anchorEl={anchorEl}
            anchorOrigin={{
              vertical: 'top',
              horizontal: 'right',
            }}
            keepMounted
            transformOrigin={{
              vertical: 'top',
              horizontal: 'right',
            }}
            open={Boolean(anchorEl)}
            onClose={handleMenuClose}
          >
            <MenuItem disabled>
              <Typography variant="body2">
                {user.firstName} {user.lastName} (Admin)
              </Typography>
            </MenuItem>
            <MenuItem disabled>
              <Typography variant="body2" color="text.secondary">
                {user.email}
              </Typography>
            </MenuItem>
            <MenuItem onClick={() => { setChangePasswordDialog(true); handleMenuClose(); }}>
              <Lock sx={{ mr: 1 }} />
              Change Password
            </MenuItem>
            <MenuItem disabled>
              <VersionInfo />
            </MenuItem>
            <MenuItem onClick={onLogout}>
              <ExitToApp sx={{ mr: 1 }} />
              Logout
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs 
          value={currentTab} 
          onChange={handleTabChange} 
          aria-label="admin tabs"
          variant="scrollable"
          scrollButtons="auto"
          allowScrollButtonsMobile
          sx={{
            '& .MuiTab-root': {
              minWidth: { xs: 80, sm: 120 },
              fontSize: { xs: '0.75rem', sm: '0.875rem' },
              padding: { xs: '6px 8px', sm: '12px 16px' },
            },
            '& .MuiTab-iconWrapper': {
              marginBottom: { xs: '2px', sm: '4px' },
            }
          }}
        >
          <Tab label="Analytics" icon={<Analytics />} />
          <Tab label="Users" icon={<People />} />
          <Tab label="Pending" icon={<Schedule />} />
          <Tab label="Door to Door" icon={<LocationOn />} />
          <Tab label="Phone Banking" icon={<PhoneInTalk />} />
          <Tab label="Phone History" icon={<History />} />
          {(user.role === 'admin' || user.role === 'superadmin') && (
            <Tab label="Campaigns" icon={<Campaign />} />
          )}
          {(user.role === 'admin' || user.role === 'superadmin') && (
            <Tab label="Voice Recordings" icon={<Phone />} />
          )}
          {(user.role === 'admin' || user.role === 'superadmin') && (
            <Tab label="Tags" icon={<LocalOffer />} />
          )}
          <Tab label="Resources" icon={<MenuBook />} />
          <Tab 
            label="Engagement" 
            icon={<Email />} 
            sx={{ 
              backgroundColor: { xs: 'action.hover', sm: 'transparent' },
              borderRadius: { xs: 1, sm: 0 },
              margin: { xs: '0 2px', sm: 0 }
            }}
          />
          {(user.role === 'admin' || user.role === 'superadmin') && (
            <Tab label="Data Mgmt" icon={<Upload />} />
          )}
          {(user.role === 'admin' || user.role === 'superadmin') && (
            <Tab label="Settings" icon={<SettingsIcon />} />
          )}
        </Tabs>
      </Box>

      <Container 
        maxWidth={false} 
        sx={{ 
          mt: 3, 
          mb: 3,
          px: { xs: 1, sm: 2, md: 3 }, // Slightly more padding for better content spacing
          maxWidth: { xs: '100%', lg: '1400px' }, // Increased max width to reduce white space
          mx: 'auto' // Center the container
        }}
      >
        {/* Result Alert */}
        {importResult && (
          <Alert 
            severity={importResult.error ? 'error' : 'success'} 
            sx={{ mb: 3 }}
            onClose={() => setImportResult(null)}
          >
            {importResult.error ? (
              <>Error: {importResult.error}</>
            ) : importResult.success ? (
              <>{importResult.success}</>
            ) : (
              <>
                Import completed! Imported: {importResult.importedCount}, 
                Errors: {importResult.errorCount}, 
                Skipped: {importResult.skippedCount}
              </>
            )}
          </Alert>
        )}

        {/* Nearest Voter Card - Only show in Voters tab */}
        {currentTab === getTabIndex('doorToDoor') && nearestVoter && (
          <Alert 
            severity="info" 
            sx={{ mb: 3 }}
            action={
              <Button 
                color="inherit" 
                size="small" 
                onClick={() => location && findNearestVoter(location)}
                startIcon={<Refresh />}
              >
                Refresh
              </Button>
            }
          >
            <Typography variant="subtitle2" gutterBottom>
              <LocationOn sx={{ verticalAlign: 'middle', mr: 1 }} />
              Nearest Uncontacted Voter
            </Typography>
            <Typography variant="body2">
              <strong>{nearestVoter.voter.firstName} {nearestVoter.voter.lastName}</strong>
            </Typography>
            <Typography variant="caption" color="text.secondary">
              📍 {(nearestVoter.distance * 0.621371).toFixed(2)} miles away
            </Typography>
            <Box 
              sx={{ 
                display: 'flex', 
                alignItems: 'center', 
                gap: 1,
                cursor: 'pointer',
                '&:hover': {
                  backgroundColor: 'action.hover',
                  borderRadius: 1,
                },
                p: 1,
                mt: 0.5,
                borderRadius: 1
              }}
              onClick={() => openInMaps(nearestVoter.voter)}
              title="Click to open in maps for directions"
            >
              <LocationOn fontSize="small" color="primary" />
              <Typography variant="body2" color="primary" sx={{ fontWeight: 'medium' }}>
                {nearestVoter.voter.addressLine}, {nearestVoter.voter.city}, {nearestVoter.voter.state} {nearestVoter.voter.zip}
              </Typography>
            </Box>
            <Box sx={{ mt: 1 }}>
              <Chip 
                label={`Age: ${nearestVoter.voter.age}`} 
                size="small" 
                sx={{ mr: 1, mb: 1 }} 
              />
              <Chip 
                label={`Sex: ${nearestVoter.voter.gender}`} 
                size="small" 
                sx={{ mr: 1, mb: 1 }} 
              />
              {nearestVoter.voter.partyAffiliation && (
                <Chip 
                  label={nearestVoter.voter.partyAffiliation} 
                  size="small" 
                  color="secondary"
                  sx={{ mr: 1, mb: 1 }}
                />
              )}
            </Box>
            <Box sx={{ mt: 2, display: 'flex', gap: 1, flexDirection: 'column' }}>
              <Button
                variant="contained"
                startIcon={<ContactPhone />}
                onClick={() => {
                  setSelectedVoterForContact(nearestVoter.voter);
                  setContactModalOpen(true);
                }}
                fullWidth
              >
                Contact Voter
              </Button>
              <Button
                variant="outlined"
                startIcon={<Phone />}
                onClick={() => {
                  setSelectedVoterForContact(nearestVoter.voter);
                  setPhoneContactModalOpen(true);
                }}
                disabled={!nearestVoter.voter.cellPhone}
                fullWidth
              >
                {nearestVoter.voter.cellPhone ? 'Call Voter' : 'No Phone Number'}
              </Button>
            </Box>
          </Alert>
        )}

        {/* Location Enable Card - Only show in Voters tab */}
        {currentTab === getTabIndex('doorToDoor') && !nearestVoter && !location && (
          <Alert severity={locationError ? "error" : "warning"} sx={{ mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>
              📍 Location needed for nearest voter
            </Typography>
            <Typography variant="body2" sx={{ mb: 2 }}>
              {locationError || "To see your nearest uncontacted voter, we need your location."}
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              <Button
                variant="contained"
                startIcon={locationLoading ? <CircularProgress size={20} color="inherit" /> : <LocationOn />}
                onClick={getCurrentLocation}
                size="small"
                disabled={locationLoading}
              >
                {locationLoading ? "Getting Location..." : locationError ? "Try Again" : "Enable Location"}
              </Button>
              {locationError && (
                <Button
                  variant="outlined"
                  size="small"
                  onClick={() => {
                    setLocationError(null);
                    getCurrentLocation();
                  }}
                  disabled={locationLoading}
                >
                  Retry
                </Button>
              )}
            </Box>
            <Box sx={{ mt: 2 }}>
              <Typography variant="body2" color="text.secondary">
                💡 Tip: Make sure to allow location permissions when your browser asks. Location requests may take up to 30 seconds in some cases.
              </Typography>
            </Box>
          </Alert>
        )}

        {/* Analytics Tab */}
        <TabPanel value={currentTab} index={getTabIndex('analytics')}>
          <AnalyticsComponent user={user} />
        </TabPanel>

        {/* Users Tab */}
        <TabPanel value={currentTab} index={getTabIndex('users')}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Typography variant="h5">
              User Management
            </Typography>
            <Box sx={{ display: 'flex', gap: 2 }}>
              {user.role === 'superadmin' && (
                <Button
                  variant="contained"
                  startIcon={<People />}
                  onClick={() => {
                    setCreateAdminDialog(true);
                    setAdminCreateResult(null);
                  }}
                  color="secondary"
                >
                  Create Admin
                </Button>
              )}
              <Button
                variant="contained"
                startIcon={<People />}
                onClick={() => {
                  setCreateVolunteerDialog(true);
                  setVolunteerCreateResult(null);
                }}
              >
                Invite Team Member
              </Button>
              <Button
                variant="outlined"
                startIcon={<Email />}
                onClick={() => {
                  fetchInvitations();
                  setInvitationsDialog(true);
                }}
              >
                View Invitations
              </Button>
            </Box>
          </Box>
          
          {loading ? (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              {/* SuperAdmins Table - Only visible to SuperAdmins */}
              {user.role === 'superadmin' && (
                <Box>
                  <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Star fontSize="small" />
                    Super Administrators ({volunteers.filter(v => v.role === 'SuperAdmin').length})
                  </Typography>
                  <TableContainer component={Paper}>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell>Name</TableCell>
                          <TableCell>Email</TableCell>
                          <TableCell>Phone</TableCell>
                          <TableCell>Status</TableCell>
                          <TableCell align="right">Logins</TableCell>
                          <TableCell>Last Login</TableCell>
                          <TableCell>Last Activity</TableCell>
                          <TableCell>Joined</TableCell>
                          <TableCell>Actions</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {volunteers.filter(volunteer => volunteer.role === 'SuperAdmin').map((superAdmin) => (
                          <TableRow key={superAdmin.id}>
                            <TableCell>
                              {superAdmin.firstName} {superAdmin.lastName}
                            </TableCell>
                            <TableCell>{superAdmin.email}</TableCell>
                            <TableCell>{superAdmin.phoneNumber || '-'}</TableCell>
                            <TableCell>
                              <Chip 
                                label={superAdmin.isActive ? 'Active' : 'Inactive'}
                                color={superAdmin.isActive ? 'success' : 'error'}
                                size="small"
                                variant="outlined"
                              />
                            </TableCell>
                            <TableCell align="right">
                              {superAdmin.loginCount || 0}
                            </TableCell>
                            <TableCell>
                              {superAdmin.lastLoginAt 
                                ? new Date(superAdmin.lastLoginAt).toLocaleDateString() 
                                : 'Never'}
                            </TableCell>
                            <TableCell>
                              {superAdmin.lastActivity 
                                ? new Date(superAdmin.lastActivity).toLocaleString() 
                                : 'Never'}
                            </TableCell>
                            <TableCell>
                              {new Date(superAdmin.createdAt).toLocaleDateString()}
                            </TableCell>
                            <TableCell>
                              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                                <Button
                                  size="small"
                                  variant="outlined"
                                  startIcon={<VpnKey />}
                                  onClick={() => handleResetPassword(superAdmin)}
                                  disabled={!superAdmin.isActive || superAdmin.id === user.id}
                                >
                                  Reset Password
                                </Button>
                                {user.role === 'superadmin' && superAdmin.id !== user.id && (
                                  <Button
                                    size="small"
                                    variant="outlined"
                                    color={superAdmin.isActive ? 'error' : 'success'}
                                    onClick={() => handleToggleUserStatus(superAdmin)}
                                    disabled={toggleStatusLoading === superAdmin.id}
                                    startIcon={toggleStatusLoading === superAdmin.id ? <CircularProgress size={16} /> : null}
                                  >
                                    {superAdmin.isActive ? 'Deactivate' : 'Activate'}
                                  </Button>
                                )}
                              </Box>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Box>
              )}

              {/* Admins Table - Only visible to SuperAdmins */}
              {user.role === 'superadmin' && (
                <Box>
                  <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <People fontSize="small" />
                    Administrators ({volunteers.filter(v => v.role === 'Admin').length})
                  </Typography>
                  <TableContainer component={Paper}>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell>Name</TableCell>
                          <TableCell>Email</TableCell>
                          <TableCell>Phone</TableCell>
                          <TableCell>Status</TableCell>
                          <TableCell align="right">Logins</TableCell>
                          <TableCell>Last Login</TableCell>
                          <TableCell>Last Activity</TableCell>
                          <TableCell>Joined</TableCell>
                          <TableCell>Actions</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {volunteers.filter(volunteer => volunteer.role === 'Admin').map((admin) => (
                          <TableRow key={admin.id}>
                            <TableCell>
                              {admin.firstName} {admin.lastName}
                            </TableCell>
                            <TableCell>{admin.email}</TableCell>
                            <TableCell>{admin.phoneNumber || '-'}</TableCell>
                            <TableCell>
                              <Chip 
                                label={admin.isActive ? 'Active' : 'Inactive'}
                                color={admin.isActive ? 'success' : 'error'}
                                size="small"
                                variant="outlined"
                              />
                            </TableCell>
                            <TableCell align="right">
                              {admin.loginCount || 0}
                            </TableCell>
                            <TableCell>
                              {admin.lastLoginAt 
                                ? new Date(admin.lastLoginAt).toLocaleDateString() 
                                : 'Never'}
                            </TableCell>
                            <TableCell>
                              {admin.lastActivity 
                                ? new Date(admin.lastActivity).toLocaleString() 
                                : 'Never'}
                            </TableCell>
                            <TableCell>
                              {new Date(admin.createdAt).toLocaleDateString()}
                            </TableCell>
                            <TableCell>
                              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                                <Button
                                  size="small"
                                  variant="outlined"
                                  startIcon={<VpnKey />}
                                  onClick={() => handleResetPassword(admin)}
                                  disabled={!admin.isActive}
                                >
                                  Reset Password
                                </Button>
                                <Button
                                  size="small"
                                  variant="outlined"
                                  startIcon={<SwapHoriz />}
                                  onClick={() => handleChangeRole(admin)}
                                  disabled={!admin.isActive}
                                >
                                  Change Role
                                </Button>
                                {user.role === 'superadmin' && (
                                  <Button
                                    size="small"
                                    variant="outlined"
                                    color={admin.isActive ? 'error' : 'success'}
                                    onClick={() => handleToggleUserStatus(admin)}
                                    disabled={toggleStatusLoading === admin.id}
                                    startIcon={toggleStatusLoading === admin.id ? <CircularProgress size={16} /> : null}
                                  >
                                    {admin.isActive ? 'Deactivate' : 'Activate'}
                                  </Button>
                                )}
                              </Box>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Box>
              )}

              {/* Volunteers Table */}
              <Box>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <HowToVote fontSize="small" />
                  Volunteers ({volunteers.filter(v => v.role === 'Volunteer').length})
                </Typography>
                <TableContainer component={Paper}>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Name</TableCell>
                        <TableCell>Email</TableCell>
                        <TableCell>Phone</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell align="right">Contacts Made</TableCell>
                        <TableCell align="right">Logins</TableCell>
                        <TableCell>Last Login</TableCell>
                        <TableCell>Last Activity</TableCell>
                        <TableCell>Joined</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {volunteers.filter(volunteer => volunteer.role === 'Volunteer').map((volunteer) => (
                        <TableRow key={volunteer.id}>
                          <TableCell>
                            {volunteer.firstName} {volunteer.lastName}
                          </TableCell>
                          <TableCell>{volunteer.email}</TableCell>
                          <TableCell>{volunteer.phoneNumber || '-'}</TableCell>
                          <TableCell>
                            <Chip 
                              label={volunteer.isActive ? 'Active' : 'Inactive'}
                              color={volunteer.isActive ? 'success' : 'error'}
                              size="small"
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell align="right">{volunteer.contactCount}</TableCell>
                          <TableCell align="right">
                            {volunteer.loginCount || 0}
                          </TableCell>
                          <TableCell>
                            {volunteer.lastLoginAt 
                              ? new Date(volunteer.lastLoginAt).toLocaleDateString() 
                              : 'Never'}
                          </TableCell>
                          <TableCell>
                            {volunteer.lastActivity 
                              ? new Date(volunteer.lastActivity).toLocaleString() 
                              : 'Never'}
                          </TableCell>
                          <TableCell>
                            {new Date(volunteer.createdAt).toLocaleDateString()}
                          </TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                              <Button
                                size="small"
                                variant="outlined"
                                startIcon={<VpnKey />}
                                onClick={() => handleResetPassword(volunteer)}
                                disabled={!volunteer.isActive}
                              >
                                Reset Password
                              </Button>
                              {user.role === 'superadmin' && (
                                <Button
                                  size="small"
                                  variant="outlined"
                                  startIcon={<SwapHoriz />}
                                  onClick={() => handleChangeRole(volunteer)}
                                  disabled={!volunteer.isActive}
                                  color="secondary"
                                >
                                  Change Role
                                </Button>
                              )}
                              <Button
                                size="small"
                                variant="outlined"
                                color={volunteer.isActive ? 'error' : 'success'}
                                onClick={() => handleToggleUserStatus(volunteer)}
                                disabled={toggleStatusLoading === volunteer.id}
                                startIcon={toggleStatusLoading === volunteer.id ? <CircularProgress size={16} /> : null}
                              >
                                {volunteer.isActive ? 'Deactivate' : 'Activate'}
                              </Button>
                              {user.role === 'superadmin' && volunteer.contactCount === 0 && (
                                <Button
                                  size="small"
                                  variant="outlined"
                                  color="error"
                                  onClick={() => {
                                    setSelectedUserForDelete(volunteer);
                                    setDeleteDialog(true);
                                    setDeleteConfirmText('');
                                    setDeleteResult(null);
                                  }}
                                  startIcon={<Delete />}
                                >
                                  Delete
                                </Button>
                              )}
                            </Box>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Box>
            </Box>
          )}
        </TabPanel>

        {/* Pending Volunteers Tab */}
        <TabPanel value={currentTab} index={getTabIndex('pending')}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Typography variant="h5">
              Pending Volunteer Registrations
            </Typography>
            <Button
              startIcon={<Refresh />}
              variant="outlined"
              onClick={fetchPendingVolunteers}
              disabled={pendingLoading}
            >
              {pendingLoading ? 'Loading...' : 'Refresh'}
            </Button>
          </Box>

          {approvalResult && approvalResult.success && (
            <Alert severity="success" sx={{ mb: 3 }} onClose={() => setApprovalResult(null)}>
              {approvalResult.success}
            </Alert>
          )}

          {approvalResult && approvalResult.error && (
            <Alert severity="error" sx={{ mb: 3 }} onClose={() => setApprovalResult(null)}>
              {approvalResult.error}
            </Alert>
          )}

          {pendingLoading ? (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          ) : pendingVolunteers.length === 0 ? (
            <Alert severity="info">
              <Typography variant="body1">
                🎉 No pending registrations! All volunteers have been processed.
              </Typography>
            </Alert>
          ) : (
            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Email</TableCell>
                    <TableCell>Phone</TableCell>
                    <TableCell>Role</TableCell>
                    <TableCell>Registered</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {pendingVolunteers.map((volunteer) => (
                    <TableRow key={volunteer.id}>
                      <TableCell>
                        {volunteer.firstName} {volunteer.lastName}
                      </TableCell>
                      <TableCell>{volunteer.email}</TableCell>
                      <TableCell>{volunteer.phoneNumber}</TableCell>
                      <TableCell>
                        <Chip 
                          label={volunteer.requestedRole} 
                          color="primary" 
                          size="small" 
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>
                        {new Date(volunteer.createdAt).toLocaleDateString()}
                      </TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', gap: 1 }}>
                          <Button
                            size="small"
                            variant="contained"
                            color="success"
                            onClick={() => {
                              setSelectedPendingVolunteer(volunteer);
                              setApproveDialog(true);
                              setAdminNotes('');
                              setApprovalResult(null);
                            }}
                            disabled={approvalLoading}
                          >
                            Approve
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="error"
                            onClick={() => {
                              setSelectedPendingVolunteer(volunteer);
                              setRejectDialog(true);
                              setAdminNotes('');
                              setApprovalResult(null);
                            }}
                            disabled={approvalLoading}
                          >
                            Reject
                          </Button>
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {/* Approved Volunteers Section */}
          <Box mt={4}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
              <Typography variant="h5">
                Approved Volunteers
              </Typography>
              <Button
                startIcon={<Refresh />}
                variant="outlined"
                onClick={fetchApprovedVolunteers}
                disabled={approvedLoading}
              >
                {approvedLoading ? 'Loading...' : 'Refresh'}
              </Button>
            </Box>

            {approvedLoading ? (
              <Box display="flex" justifyContent="center" py={4}>
                <CircularProgress />
              </Box>
            ) : approvedVolunteers.length === 0 ? (
              <Alert severity="info">
                <Typography variant="body1">
                  No approved volunteers to display.
                </Typography>
              </Alert>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Name</TableCell>
                      <TableCell>Email</TableCell>
                      <TableCell>Phone</TableCell>
                      <TableCell>Role</TableCell>
                      <TableCell>Requested Date</TableCell>
                      <TableCell>Approved Date</TableCell>
                      <TableCell>Approved By</TableCell>
                      <TableCell>Review Notes</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {approvedVolunteers.map((volunteer) => (
                      <TableRow key={volunteer.id}>
                        <TableCell>
                          {volunteer.firstName} {volunteer.lastName}
                        </TableCell>
                        <TableCell>{volunteer.email}</TableCell>
                        <TableCell>{volunteer.phoneNumber}</TableCell>
                        <TableCell>
                          <Chip 
                            label={volunteer.requestedRole} 
                            color="success" 
                            size="small" 
                            variant="outlined"
                          />
                        </TableCell>
                        <TableCell>
                          {new Date(volunteer.createdAt).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          {volunteer.approvedAt ? new Date(volunteer.approvedAt).toLocaleDateString() : '-'}
                        </TableCell>
                        <TableCell>
                          <Box>
                            <Typography variant="body2">{volunteer.approvedBy}</Typography>
                            {volunteer.approvedByEmail && (
                              <Typography variant="caption" color="text.secondary">
                                {volunteer.approvedByEmail}
                              </Typography>
                            )}
                          </Box>
                        </TableCell>
                        <TableCell>
                          {volunteer.reviewNotes ? (
                            <Tooltip title={volunteer.reviewNotes}>
                              <Typography variant="body2" sx={{ 
                                maxWidth: 200, 
                                overflow: 'hidden', 
                                textOverflow: 'ellipsis',
                                whiteSpace: 'nowrap'
                              }}>
                                {volunteer.reviewNotes}
                              </Typography>
                            </Tooltip>
                          ) : (
                            '-'
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Box>
        </TabPanel>

        {/* Voters Tab */}
        <TabPanel value={currentTab} index={getTabIndex('doorToDoor')}>
          {/* Leaderboard & Achievements */}
          {leaderboard && (
            <Card sx={{ mb: 3 }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <EmojiEvents sx={{ mr: 1, color: '#ffd700' }} />
                  <Typography variant="h6">
                    Leaderboard & Achievements
                  </Typography>
                </Box>

                {/* User Achievements */}
                {leaderboard.currentUserAchievements && leaderboard.currentUserAchievements.length > 0 && (
                  <Box sx={{ mb: 3 }}>
                    <Typography variant="subtitle2" sx={{ mb: 1, display: 'flex', alignItems: 'center' }}>
                      <Star sx={{ mr: 1, fontSize: 16 }} />
                      Your Achievements
                    </Typography>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                      {leaderboard.currentUserAchievements.map((achievement: any, index: number) => (
                        <Chip
                          key={index}
                          icon={<span style={{ fontSize: '16px' }}>{achievement.icon}</span>}
                          label={`${achievement.name} - ${achievement.description}`}
                          color="primary"
                          variant="outlined"
                          size="small"
                        />
                      ))}
                    </Box>
                  </Box>
                )}

                {/* Leaderboard Tabs */}
                <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
                  <Tabs value={leaderboardTab} onChange={(e, newValue) => setLeaderboardTab(newValue)}>
                    <Tab label="This Week" />
                    <Tab label="This Month" />
                  </Tabs>
                </Box>

                {/* Weekly Leaderboard */}
                {leaderboardTab === 0 && leaderboard.weeklyLeaderboard && (
                  <Box>
                    <Typography variant="subtitle2" sx={{ mb: 2 }}>
                      Top Volunteers This Week
                    </Typography>
                    {leaderboard.weeklyLeaderboard.slice(0, 5).map((entry: any, index: number) => (
                      <Box
                        key={entry.volunteerId}
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                          p: 1,
                          borderRadius: 1,
                          backgroundColor: entry.isCurrentUser ? 'action.selected' : 'transparent',
                          border: entry.isCurrentUser ? '1px solid' : 'none',
                          borderColor: entry.isCurrentUser ? 'primary.main' : 'transparent',
                          mb: 1
                        }}
                      >
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="body2" sx={{ minWidth: '20px', fontWeight: 'bold' }}>
                            #{entry.position}
                          </Typography>
                          {entry.badge && (
                            <span style={{ fontSize: '18px' }}>{entry.badge}</span>
                          )}
                          <Typography variant="body2" sx={{ fontWeight: entry.isCurrentUser ? 'bold' : 'normal' }}>
                            {entry.isCurrentUser ? 'You' : entry.volunteerName}
                          </Typography>
                        </Box>
                        <Chip
                          label={`${entry.contactCount} contacts`}
                          size="small"
                          color={entry.isCurrentUser ? 'primary' : 'default'}
                        />
                      </Box>
                    ))}
                  </Box>
                )}

                {/* Monthly Leaderboard */}
                {leaderboardTab === 1 && leaderboard.monthlyLeaderboard && (
                  <Box>
                    <Typography variant="subtitle2" sx={{ mb: 2 }}>
                      Top Volunteers This Month
                    </Typography>
                    {leaderboard.monthlyLeaderboard.slice(0, 5).map((entry: any, index: number) => (
                      <Box
                        key={entry.volunteerId}
                        sx={{
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                          p: 1,
                          borderRadius: 1,
                          backgroundColor: entry.isCurrentUser ? 'action.selected' : 'transparent',
                          border: entry.isCurrentUser ? '1px solid' : 'none',
                          borderColor: entry.isCurrentUser ? 'primary.main' : 'transparent',
                          mb: 1
                        }}
                      >
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="body2" sx={{ minWidth: '20px', fontWeight: 'bold' }}>
                            #{entry.position}
                          </Typography>
                          {entry.badge && (
                            <span style={{ fontSize: '18px' }}>{entry.badge}</span>
                          )}
                          <Typography variant="body2" sx={{ fontWeight: entry.isCurrentUser ? 'bold' : 'normal' }}>
                            {entry.isCurrentUser ? 'You' : entry.volunteerName}
                          </Typography>
                        </Box>
                        <Chip
                          label={`${entry.contactCount} contacts`}
                          size="small"
                          color={entry.isCurrentUser ? 'primary' : 'default'}
                        />
                      </Box>
                    ))}
                  </Box>
                )}

                <Box sx={{ mt: 2, p: 2, backgroundColor: 'action.hover', borderRadius: 1 }}>
                  <Typography variant="body2" sx={{ fontSize: '12px', textAlign: 'center', color: 'text.secondary' }}>
                    💝 Top volunteers each month receive lunch gift cards! Keep up the great work!
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          )}

          {/* Door to Door Content */}
          <Box sx={{ mb: 2 }}>
            <Typography variant="h5" gutterBottom>
              Door to Door Canvassing
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              View voters in your area and track door-to-door visits
            </Typography>
          </Box>
          <VoterList onContactVoter={handleContactVoter} user={user} />
        </TabPanel>

        {/* Phone Banking Tab */}
        <TabPanel value={currentTab} index={getTabIndex('phoneBanking')}>
          <Box sx={{ mb: 2 }}>
            <Typography variant="h5" gutterBottom>
              Phone Banking
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Call voters with phone numbers and record the results
            </Typography>
          </Box>
          <VoterList onContactVoter={handleContactVoter} user={user} mode="phone-banking" />
        </TabPanel>

        {/* Phone History Tab */}
        <TabPanel value={currentTab} index={getTabIndex('history')}>
          <Box textAlign="center" py={4}>
            <History sx={{ fontSize: 80, color: 'primary.main', mb: 2 }} />
            <Typography variant="h5" gutterBottom>
              Phone Contact History
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              View all your phone contact history and statistics
            </Typography>
            <Button
              variant="contained"
              startIcon={<History />}
              onClick={() => navigate('/phone-contacts')}
            >
              View Full History
            </Button>
          </Box>
        </TabPanel>

        {/* Campaigns Tab - For Admins and SuperAdmins */}
        {(user.role === 'admin' || user.role === 'superadmin') && (
          <TabPanel value={currentTab} index={getTabIndex('campaigns')}>
            <CampaignDashboard user={user} />
          </TabPanel>
        )}

        {/* Voice Recordings Tab - For Admins and SuperAdmins */}
        {(user.role === 'admin' || user.role === 'superadmin') && (
          <TabPanel value={currentTab} index={getTabIndex('voiceRecordings')}>
            <VoiceRecordings user={user} />
          </TabPanel>
        )}


        {/* Tags Tab - For Admins and SuperAdmins */}
        {(user.role === 'admin' || user.role === 'superadmin') && (
          <TabPanel value={currentTab} index={getTabIndex('tags')}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
              <Typography variant="h5">
                Tag Management
              </Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={openCreateTagDialog}
              >
                Create Tag
              </Button>
            </Box>
            
            <Typography variant="body2" color="text.secondary" paragraph>
              Create and manage tags to organize voters for targeted campaigns.
            </Typography>

            {tagLoading ? (
              <Box display="flex" justifyContent="center" py={4}>
                <CircularProgress />
              </Box>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Tag Name</TableCell>
                      <TableCell>Description</TableCell>
                      <TableCell>Color</TableCell>
                      <TableCell align="right">Voters</TableCell>
                      <TableCell>Created</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {tags.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={6} align="center">
                          <Typography variant="body2" color="text.secondary" sx={{ py: 4 }}>
                            No tags created yet. Create your first tag to get started.
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ) : (
                      tags.map((tag) => (
                        <TableRow key={tag.id}>
                          <TableCell>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Box
                                sx={{
                                  width: 12,
                                  height: 12,
                                  borderRadius: '50%',
                                  backgroundColor: tag.color || '#2196F3'
                                }}
                              />
                              <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                                {tag.tagName}
                              </Typography>
                            </Box>
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2" color="text.secondary">
                              {tag.description || '-'}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={tag.color || '#2196F3'}
                              size="small"
                              sx={{
                                backgroundColor: tag.color || '#2196F3',
                                color: 'white',
                                fontFamily: 'monospace',
                                fontSize: '0.7rem'
                              }}
                            />
                          </TableCell>
                          <TableCell align="right">
                            <Chip
                              label={tag.voterCount}
                              size="small"
                              color="primary"
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2" color="text.secondary">
                              {new Date(tag.createdAt).toLocaleDateString()}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', gap: 1 }}>
                              <Button
                                size="small"
                                variant="outlined"
                                startIcon={<Edit />}
                                onClick={() => openEditTagDialog(tag)}
                              >
                                Edit
                              </Button>
                              <Button
                                size="small"
                                variant="outlined"
                                color="error"
                                startIcon={<Delete />}
                                onClick={() => handleDeleteTag(tag)}
                              >
                                Delete
                              </Button>
                            </Box>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </TabPanel>
        )}

        {/* Resources Tab */}
        <TabPanel value={currentTab} index={getTabIndex('resources')}>
          <Typography variant="h5" gutterBottom>
            Volunteer Resources
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            Campaign information, resources, and support for volunteers.
          </Typography>

          {/* Additional Resource Links - Admins can manage these */}
          <ResourceLinksSection user={user} isAdmin={user.role === 'admin' || user.role === 'superadmin'} />

          {/* Use the shared VolunteerResourcesSection component */}
          <VolunteerResourcesSection showQuickTips={false} showQRCode={true} />

          {/* Quick Tips */}
          <Card sx={{ mb: 3 }}>
            <CardContent sx={{ 
              background: 'rgba(47, 28, 106, 0.05)',
              border: '1px solid rgba(47, 28, 106, 0.1)'
            }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#2f1c6a' }}>
                  Canvassing Quick Tips
                </Typography>
                {user.role === 'superadmin' && (
                  <IconButton 
                    size="small" 
                    onClick={() => handleEditResource('quickTips')}
                    sx={{ color: '#2f1c6a' }}
                  >
                    <Edit fontSize="small" />
                  </IconButton>
                )}
              </Box>
              <Typography 
                variant="body2" 
                sx={{ color: '#2f1c6a', lineHeight: 1.6, whiteSpace: 'pre-line' }}
              >
                {volunteerResources.quickTips || 'No quick tips available yet.'}
              </Typography>
            </CardContent>
          </Card>

          {/* Volunteer Script */}
          <Card>
            <CardContent sx={{ 
              background: 'rgba(47, 28, 106, 0.05)',
              border: '1px solid rgba(47, 28, 106, 0.1)'
            }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#2f1c6a' }}>
                  Volunteer Script
                </Typography>
                {user.role === 'superadmin' && (
                  <IconButton 
                    size="small" 
                    onClick={() => handleEditResource('script')}
                    sx={{ color: '#2f1c6a' }}
                  >
                    <Edit fontSize="small" />
                  </IconButton>
                )}
              </Box>
              <Typography 
                variant="body2" 
                sx={{ color: '#2f1c6a', lineHeight: 1.6, whiteSpace: 'pre-line' }}
              >
                {volunteerResources.script ? 
                  volunteerResources.script.replace('[Volunteer Name]', `${user.firstName} ${user.lastName}`) : 
                  'No volunteer script available yet.'}
              </Typography>
            </CardContent>
          </Card>
        </TabPanel>

        {/* Engagement Tab */}
        <TabPanel value={currentTab} index={getTabIndex('engagement')}>
          <Typography variant="h5" gutterBottom>
            User Engagement
          </Typography>
          <Typography variant="body2" color="text.secondary" paragraph>
            Send emails to your volunteers and admins to keep them engaged and informed.
          </Typography>

          {emailResult && emailResult.success && (
            <Alert severity="success" sx={{ mb: 2 }}>
              {emailResult.success}
            </Alert>
          )}
          {emailResult && emailResult.error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {emailResult.error}
            </Alert>
          )}

          <Box sx={{ display: 'grid', gap: 3 }}>
            {/* Email Composition */}
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Compose Email
                </Typography>
                
                <TextField
                  label="Subject"
                  fullWidth
                  margin="normal"
                  value={emailSubject}
                  onChange={(e) => setEmailSubject(e.target.value)}
                  disabled={emailSending}
                  required
                  placeholder="Enter email subject..."
                />
                
                <TextField
                  label="Message"
                  fullWidth
                  multiline
                  rows={8}
                  margin="normal"
                  value={emailContent}
                  onChange={(e) => setEmailContent(e.target.value)}
                  disabled={emailSending}
                  required
                  placeholder="Enter your message here..."
                  helperText="You can use plain text or basic HTML formatting"
                />
              </CardContent>
            </Card>

            {/* Recipient Selection */}
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Select Recipients
                </Typography>
                
                <FormControl fullWidth sx={{ mb: 2 }}>
                  <InputLabel>Recipient Type</InputLabel>
                  <Select
                    value={recipientType}
                    label="Recipient Type"
                    onChange={(e) => {
                      setRecipientType(e.target.value);
                      setSelectedUsers([]);
                    }}
                    disabled={emailSending}
                  >
                    <MenuItem value="selected">Selected Users</MenuItem>
                    <MenuItem value="all">All Active Users</MenuItem>
                    <MenuItem value="volunteers">All Volunteers</MenuItem>
                    <MenuItem value="admins">All Admins</MenuItem>
                  </Select>
                </FormControl>

                {recipientType === 'selected' && (
                  <Box>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Typography variant="subtitle2">
                        Select Users ({selectedUsers.length} selected)
                      </Typography>
                      <Button
                        size="small"
                        onClick={handleSelectAllUsers}
                        disabled={emailSending}
                      >
                        {selectedUsers.length === volunteers.length ? 'Deselect All' : 'Select All'}
                      </Button>
                    </Box>
                    
                    <TableContainer component={Paper} sx={{ maxHeight: 300 }}>
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            <TableCell padding="checkbox"></TableCell>
                            <TableCell>Name</TableCell>
                            <TableCell>Email</TableCell>
                            <TableCell>Role</TableCell>
                            <TableCell>Status</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {volunteers.map((volunteer) => (
                            <TableRow key={volunteer.id}>
                              <TableCell padding="checkbox">
                                <Checkbox
                                  checked={selectedUsers.includes(volunteer.id)}
                                  onChange={() => handleUserSelection(volunteer.id)}
                                  disabled={emailSending}
                                />
                              </TableCell>
                              <TableCell>
                                {volunteer.firstName} {volunteer.lastName}
                              </TableCell>
                              <TableCell>{volunteer.email}</TableCell>
                              <TableCell>
                                <Chip 
                                  label={volunteer.role} 
                                  size="small"
                                  color={volunteer.role === 'SuperAdmin' ? 'error' : volunteer.role === 'Admin' ? 'warning' : 'default'}
                                />
                              </TableCell>
                              <TableCell>
                                <Chip 
                                  label={volunteer.isActive ? 'Active' : 'Inactive'} 
                                  size="small"
                                  color={volunteer.isActive ? 'success' : 'default'}
                                />
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </Box>
                )}

                {recipientType !== 'selected' && (
                  <Alert severity="info" sx={{ mt: 2 }}>
                    {recipientType === 'all' && `Email will be sent to all ${volunteers.filter(v => v.isActive).length} active users.`}
                    {recipientType === 'volunteers' && `Email will be sent to all ${volunteers.filter(v => v.isActive && v.role === 'Volunteer').length} active volunteers.`}
                    {recipientType === 'admins' && `Email will be sent to all ${volunteers.filter(v => v.isActive && (v.role === 'Admin' || v.role === 'SuperAdmin')).length} active admins.`}
                  </Alert>
                )}
              </CardContent>
            </Card>

            {/* Send Button */}
            <Box sx={{ display: 'flex', justifyContent: 'center' }}>
              <Button
                variant="contained"
                size="large"
                startIcon={emailSending ? <CircularProgress size={20} /> : <Email />}
                onClick={handleSendEngagementEmail}
                disabled={emailSending || !emailSubject.trim() || !emailContent.trim()}
                sx={{ minWidth: 200 }}
              >
                {emailSending ? 'Sending...' : 'Send Email'}
              </Button>
            </Box>
          </Box>
        </TabPanel>

        {/* Data Management Tab - For Admins and SuperAdmins */}
        {(user.role === 'admin' || user.role === 'superadmin') && (
          <TabPanel value={currentTab} index={getTabIndex('dataManagement')}>
          <Typography variant="h5" gutterBottom>
            Data Management
          </Typography>
          
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 3, mb: 4 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Import Voters
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                  Upload a CSV file with voter data. The system will automatically geocode addresses using OpenStreetMap.
                </Typography>
                <Box sx={{ display: 'flex', gap: 2 }}>
                  <Button
                    variant="contained"
                    startIcon={<Upload />}
                    onClick={() => setImportDialog(true)}
                  >
                    Simple Import
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<TableIcon />}
                    onClick={() => setStagingImportDialog(true)}
                  >
                    Advanced Import
                  </Button>
                </Box>
              </CardContent>
            </Card>
            
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Geocode Addresses
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                  Convert voter addresses to coordinates for location-based features.
                </Typography>
                {geocodingStatus && (
                  <Box sx={{ mb: 2 }}>
                    <Typography variant="body2">
                      {geocodingStatus.geocodedVoters} / {geocodingStatus.totalVoters} geocoded 
                      ({geocodingStatus.geocodingPercentage.toFixed(1)}%)
                    </Typography>
                    {geocodingStatus.pendingVoters > 0 && (
                      <Typography variant="body2" color="warning.main">
                        {geocodingStatus.pendingVoters} addresses pending
                      </Typography>
                    )}
                  </Box>
                )}
                <Button
                  variant="contained"
                  startIcon={<LocationOn />}
                  onClick={handleGeocodeVoters}
                  disabled={geocodingLoading || (geocodingStatus?.pendingVoters === 0)}
                >
                  {geocodingLoading ? 'Geocoding...' : 'Start Geocoding'}
                </Button>
                {geocodingResult && (
                  <Box sx={{ mt: 2 }}>
                    {geocodingResult.error ? (
                      <Alert severity="error">
                        {geocodingResult.error}
                      </Alert>
                    ) : (
                      <Alert severity="success">
                        Geocoded {geocodingResult.geocoded} of {geocodingResult.processed} addresses
                        {geocodingResult.failed > 0 && ` (${geocodingResult.failed} failed)`}
                      </Alert>
                    )}
                  </Box>
                )}
              </CardContent>
            </Card>
          </Box>
          </TabPanel>
        )}

        {/* Settings Tab - For Admins and SuperAdmins */}
        {(user.role === 'admin' || user.role === 'superadmin') && (
          <TabPanel value={currentTab} index={getTabIndex('settings')}>
            <Typography variant="h5" gutterBottom>
              System Settings
            </Typography>
            
            <Box sx={{ mb: 4 }}>
              <TwilioSettings />
            </Box>
            
            <Box sx={{ mb: 4 }}>
              <PhoneNumberManagement />
            </Box>
          </TabPanel>
        )}
      </Container>

      {/* Import Dialog */}
      <Dialog open={importDialog} onClose={() => {
        if (!importLoading) {
          setImportDialog(false);
          setImportFile(null);
          setEnableGeocoding(false);
        }
      }} maxWidth="sm" fullWidth>
        <DialogTitle>Import Voter Data</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            Upload a CSV file with voter data. The file should have the following columns:
          </Typography>
          
          <Box sx={{ mb: 2, p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
            <Typography variant="caption" component="div" sx={{ fontFamily: 'monospace' }}>
              FirstName, LastName, AddressLine, City, State, Zip, Age, Gender, CellPhone, Email, VoteFrequency, PartyAffiliation
            </Typography>
          </Box>
          
          <Box sx={{ mb: 3 }}>
            <Button
              variant="outlined"
              startIcon={<GetApp />}
              onClick={handleDownloadSampleCsv}
              size="small"
            >
              Download Sample CSV
            </Button>
          </Box>
          
          <Typography variant="body2" color="text.secondary" paragraph>
            <strong>Note:</strong> VoteFrequency should be one of: Frequent, Infrequent, or NonVoter
          </Typography>
          
          {importLoading && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" gutterBottom>
                Importing voters...
              </Typography>
              <CircularProgress size={24} sx={{ mr: 1 }} />
              <Typography variant="caption" color="text.secondary">
                This may take a few moments depending on file size
              </Typography>
            </Box>
          )}
          
          <TextField
            type="file"
            fullWidth
            margin="normal"
            inputProps={{ accept: '.csv' }}
            onChange={(e) => {
              const file = (e.target as HTMLInputElement).files?.[0];
              console.log('File selected:', file?.name, 'Size:', file?.size);
              setImportFile(file || null);
            }}
            disabled={importLoading}
          />
          {importFile && (
            <Typography variant="body2" sx={{ mt: 1 }}>
              Selected: {importFile.name} ({Math.round(importFile.size / 1024)} KB)
            </Typography>
          )}
          <FormControlLabel
            control={
              <Checkbox
                checked={enableGeocoding}
                onChange={(e) => setEnableGeocoding(e.target.checked)}
                disabled={importLoading}
              />
            }
            label="Enable geocoding (slower but adds coordinates for location-based features)"
            sx={{ mt: 2 }}
          />
          {enableGeocoding && (
            <Alert severity="warning" sx={{ mt: 1 }}>
              Geocoding will significantly slow down the import process as it looks up coordinates for each address.
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setImportDialog(false);
            setImportFile(null);
            setEnableGeocoding(false);
          }} disabled={importLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleImportVoters} 
            variant="contained" 
            disabled={!importFile || importLoading}
            startIcon={importLoading ? <CircularProgress size={20} /> : <Upload />}
          >
            {importLoading ? 'Importing...' : 'Import'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Invite Team Member Dialog */}
      <Dialog open={createVolunteerDialog} onClose={() => !volunteerCreateLoading && setCreateVolunteerDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>📧 Invite Team Member</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            Send an invitation email to add a new team member. They'll receive a secure link to complete their registration with their personal details.
          </Typography>
          {volunteerCreateResult && volunteerCreateResult.success && (
            <Alert severity="success" sx={{ mb: 2 }}>
              {volunteerCreateResult.success}
            </Alert>
          )}
          {volunteerCreateResult && volunteerCreateResult.error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {volunteerCreateResult.error}
            </Alert>
          )}
          <TextField
            label="Email Address"
            type="email"
            fullWidth
            margin="normal"
            value={invitationForm.email}
            onChange={(e) => setInvitationForm({ ...invitationForm, email: e.target.value })}
            disabled={volunteerCreateLoading}
            required
            helperText="The person will receive an invitation email at this address"
            placeholder="volunteer@example.com"
          />
          <FormControl fullWidth margin="normal">
            <InputLabel>Role</InputLabel>
            <Select
              value={invitationForm.role}
              onChange={(e) => setInvitationForm({ ...invitationForm, role: e.target.value })}
              disabled={volunteerCreateLoading}
              label="Role"
            >
              <MenuItem value="Volunteer">Volunteer</MenuItem>
              {user.role === 'superadmin' && (
                <MenuItem value="Admin">Admin</MenuItem>
              )}
            </Select>
          </FormControl>
          <Box sx={{ mt: 2, p: 2, bgcolor: 'info.light', borderRadius: 1 }}>
            <Typography variant="body2" color="info.contrastText">
              💡 <strong>How it works:</strong>
              <br />• They'll get an email with a secure registration link
              <br />• Link expires in 7 days for security
              <br />• They complete their name, phone, and password
              <br />• Account is immediately active once completed
            </Typography>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateVolunteerDialog(false)} disabled={volunteerCreateLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleSendInvitation} 
            variant="contained" 
            disabled={volunteerCreateLoading || !invitationForm.email}
            startIcon={volunteerCreateLoading ? <CircularProgress size={20} /> : <Email />}
          >
            {volunteerCreateLoading ? 'Sending...' : 'Send Invitation'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Invitations Dialog */}
      <Dialog open={invitationsDialog} onClose={() => setInvitationsDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          <Box display="flex" alignItems="center" justifyContent="space-between">
            <Box display="flex" alignItems="center" gap={1}>
              <Email />
              Invitations
            </Box>
            <IconButton onClick={() => setInvitationsDialog(false)} size="small">
              <CloseIcon />
            </IconButton>
          </Box>
        </DialogTitle>
        <DialogContent>
          {invitationsLoading ? (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          ) : invitations.length === 0 ? (
            <Alert severity="info">No invitations found.</Alert>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Email</TableCell>
                    <TableCell>Role</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Created</TableCell>
                    <TableCell>Expires</TableCell>
                    <TableCell>Created By</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {invitations.map((invitation) => (
                    <TableRow key={invitation.id}>
                      <TableCell>{invitation.email}</TableCell>
                      <TableCell>
                        <Chip 
                          label={invitation.role} 
                          size="small" 
                          color={invitation.role === 'Admin' ? 'secondary' : 'default'}
                        />
                      </TableCell>
                      <TableCell>
                        {invitation.isUsed ? (
                          <Chip label="Used" color="success" size="small" />
                        ) : invitation.isExpired ? (
                          <Chip label="Expired" color="error" size="small" />
                        ) : (
                          <Chip label="Pending" color="warning" size="small" />
                        )}
                      </TableCell>
                      <TableCell>{new Date(invitation.createdAt).toLocaleDateString()}</TableCell>
                      <TableCell>{new Date(invitation.expiresAt).toLocaleDateString()}</TableCell>
                      <TableCell>{invitation.createdBy || '-'}</TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', gap: 1 }}>
                          {!invitation.isUsed && !invitation.isExpired && (
                            <Button
                              size="small"
                              variant="outlined"
                              startIcon={resendingInvitation === invitation.id ? <CircularProgress size={16} /> : <Refresh />}
                              onClick={() => handleResendInvitation(invitation.id)}
                              disabled={resendingInvitation === invitation.id}
                            >
                              Resend
                            </Button>
                          )}
                          {(invitation.isUsed || invitation.isExpired) && (
                            <Button
                              size="small"
                              variant="outlined"
                              color="error"
                              startIcon={<Delete />}
                              onClick={() => handleDeleteInvitation(invitation.id, invitation.email)}
                            >
                              Delete
                            </Button>
                          )}
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
          {volunteerCreateResult && volunteerCreateResult.success && (
            <Alert severity="success" sx={{ mt: 2 }}>
              {volunteerCreateResult.success}
            </Alert>
          )}
          {volunteerCreateResult && volunteerCreateResult.error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {volunteerCreateResult.error}
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setInvitationsDialog(false)}>
            Close
          </Button>
        </DialogActions>
      </Dialog>

      {/* Create Admin Dialog */}
      <Dialog open={createAdminDialog} onClose={() => !adminCreateLoading && setCreateAdminDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Admin</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            Create a new administrator account with elevated privileges.
          </Typography>
          {adminCreateResult && adminCreateResult.success && (
            <Alert severity="success" sx={{ mb: 2 }}>
              {adminCreateResult.success}
            </Alert>
          )}
          {adminCreateResult && adminCreateResult.error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {adminCreateResult.error}
            </Alert>
          )}
          <TextField
            label="First Name"
            fullWidth
            margin="normal"
            value={adminForm.firstName}
            onChange={(e) => setAdminForm({ ...adminForm, firstName: e.target.value })}
            disabled={adminCreateLoading}
            required
          />
          <TextField
            label="Last Name"
            fullWidth
            margin="normal"
            value={adminForm.lastName}
            onChange={(e) => setAdminForm({ ...adminForm, lastName: e.target.value })}
            disabled={adminCreateLoading}
            required
          />
          <TextField
            label="Email Address"
            type="email"
            fullWidth
            margin="normal"
            value={adminForm.email}
            onChange={(e) => setAdminForm({ ...adminForm, email: e.target.value })}
            disabled={adminCreateLoading}
            required
          />
          <TextField
            label="Phone Number"
            type="tel"
            fullWidth
            margin="normal"
            value={adminForm.phoneNumber}
            onChange={(e) => setAdminForm({ ...adminForm, phoneNumber: e.target.value })}
            disabled={adminCreateLoading}
            placeholder="(555) 123-4567"
            helperText="Optional - for emergency contact"
          />
          <TextField
            label="Password"
            type={showAdminPassword ? 'text' : 'password'}
            fullWidth
            margin="normal"
            value={adminForm.password}
            onChange={(e) => setAdminForm({ ...adminForm, password: e.target.value })}
            disabled={adminCreateLoading}
            required
            helperText="Minimum 6 characters with at least one digit and lowercase letter"
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label="toggle password visibility"
                    onClick={() => setShowAdminPassword(!showAdminPassword)}
                    edge="end"
                    disabled={adminCreateLoading}
                  >
                    {showAdminPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateAdminDialog(false)} disabled={adminCreateLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleCreateAdmin} 
            variant="contained" 
            disabled={adminCreateLoading || !adminForm.firstName || !adminForm.lastName || !adminForm.email || !adminForm.password}
            startIcon={adminCreateLoading ? <CircularProgress size={20} /> : <People />}
            color="secondary"
          >
            {adminCreateLoading ? 'Creating...' : 'Create Admin'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Change Password Dialog */}
      <Dialog open={changePasswordDialog} onClose={() => !passwordChangeLoading && setChangePasswordDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Change Password</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            Enter your current password and choose a new password.
          </Typography>
          <TextField
            label="Current Password"
            type={showCurrentPassword ? 'text' : 'password'}
            fullWidth
            margin="normal"
            value={passwordForm.currentPassword}
            onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
            disabled={passwordChangeLoading}
            required
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label="toggle password visibility"
                    onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                    edge="end"
                    disabled={passwordChangeLoading}
                  >
                    {showCurrentPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />
          <TextField
            label="New Password"
            type={showNewPassword ? 'text' : 'password'}
            fullWidth
            margin="normal"
            value={passwordForm.newPassword}
            onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
            disabled={passwordChangeLoading}
            required
            error={passwordForm.newPassword.length > 0 && !validatePassword(passwordForm.newPassword).isValid}
            helperText={getPasswordHelperText(passwordForm.newPassword)}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label="toggle password visibility"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                    edge="end"
                    disabled={passwordChangeLoading}
                  >
                    {showNewPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />
          <TextField
            label="Confirm New Password"
            type={showConfirmPassword ? 'text' : 'password'}
            fullWidth
            margin="normal"
            value={passwordForm.confirmPassword}
            onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
            disabled={passwordChangeLoading}
            required
            error={passwordForm.confirmPassword !== '' && passwordForm.newPassword !== passwordForm.confirmPassword}
            helperText={passwordForm.confirmPassword !== '' && passwordForm.newPassword !== passwordForm.confirmPassword ? "Passwords do not match" : ""}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label="toggle password visibility"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    edge="end"
                    disabled={passwordChangeLoading}
                  >
                    {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setChangePasswordDialog(false)} disabled={passwordChangeLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleChangePassword} 
            variant="contained" 
            disabled={passwordChangeLoading || !passwordForm.currentPassword || !passwordForm.newPassword || !passwordForm.confirmPassword || passwordForm.newPassword !== passwordForm.confirmPassword}
            startIcon={passwordChangeLoading ? <CircularProgress size={20} /> : <Lock />}
          >
            {passwordChangeLoading ? 'Changing...' : 'Change Password'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Reset Password Dialog */}
      <Dialog open={resetPasswordDialog} onClose={() => !resetPasswordLoading && setResetPasswordDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Reset User Password</DialogTitle>
        <DialogContent>
          {selectedVolunteer && (
            <>
              <Typography variant="body1" paragraph>
                Are you sure you want to reset the password for:
              </Typography>
              <Typography variant="h6" sx={{ mb: 2 }}>
                {selectedVolunteer.firstName} {selectedVolunteer.lastName}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Email: {selectedVolunteer.email}
              </Typography>
              
              {!resetPasswordResult && (
                <>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={useCustomPassword}
                        onChange={(e) => {
                          setUseCustomPassword(e.target.checked);
                          setCustomPassword('');
                        }}
                        disabled={resetPasswordLoading}
                      />
                    }
                    label="Set a specific password"
                    sx={{ mb: 2 }}
                  />
                  
                  {useCustomPassword ? (
                    <TextField
                      label="New Password"
                      type={showCustomPassword ? 'text' : 'password'}
                      fullWidth
                      margin="normal"
                      value={customPassword}
                      onChange={(e) => setCustomPassword(e.target.value)}
                      disabled={resetPasswordLoading}
                      required
                      error={customPassword.length > 0 && !validatePassword(customPassword).isValid}
                      helperText={getPasswordHelperText(customPassword)}
                      sx={{ mb: 2 }}
                      InputProps={{
                        endAdornment: (
                          <InputAdornment position="end">
                            <IconButton
                              aria-label="toggle password visibility"
                              onClick={() => setShowCustomPassword(!showCustomPassword)}
                              edge="end"
                              disabled={resetPasswordLoading}
                            >
                              {showCustomPassword ? <VisibilityOff /> : <Visibility />}
                            </IconButton>
                          </InputAdornment>
                        ),
                      }}
                    />
                  ) : (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                      A new temporary password will be generated. Please share this password securely with the user.
                    </Alert>
                  )}
                </>
              )}

              {resetPasswordResult && resetPasswordResult.success && (
                <Alert severity="success" sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Password reset successful!
                  </Typography>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    <strong>{useCustomPassword ? 'New Password:' : 'Temporary Password:'}</strong> 
                    <Box component="code" sx={{ 
                      bgcolor: 'background.paper', 
                      p: 1, 
                      borderRadius: 1, 
                      ml: 1,
                      fontFamily: 'monospace',
                      fontSize: '1.1em',
                      fontWeight: 'bold'
                    }}>
                      {useCustomPassword ? customPassword : resetPasswordResult.temporaryPassword}
                    </Box>
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {useCustomPassword 
                      ? `The password has been set as specified for ${selectedVolunteer.firstName}.`
                      : `Please share this password securely with ${selectedVolunteer.firstName}. They should change it immediately after logging in.`}
                  </Typography>
                </Alert>
              )}

              {resetPasswordResult && !resetPasswordResult.success && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {resetPasswordResult.error}
                </Alert>
              )}
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResetPasswordDialog(false)} disabled={resetPasswordLoading}>
            {resetPasswordResult?.success ? 'Close' : 'Cancel'}
          </Button>
          {!resetPasswordResult?.success && (
            <Button 
              onClick={confirmResetPassword} 
              variant="contained" 
              color="warning"
              disabled={resetPasswordLoading}
              startIcon={resetPasswordLoading ? <CircularProgress size={20} /> : <VpnKey />}
            >
              {resetPasswordLoading ? 'Resetting...' : 'Reset Password'}
            </Button>
          )}
        </DialogActions>
      </Dialog>

      {/* Change Role Dialog */}
      <Dialog open={changeRoleDialog} onClose={() => !roleChangeLoading && setChangeRoleDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Change User Role</DialogTitle>
        <DialogContent>
          {selectedUserForRole && (
            <>
              <Typography variant="body1" paragraph>
                Change role for:
              </Typography>
              <Typography variant="h6" sx={{ mb: 2 }}>
                {selectedUserForRole.firstName} {selectedUserForRole.lastName}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Email: {selectedUserForRole.email}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Current Role: <strong>{selectedUserForRole.role}</strong>
              </Typography>
              
              {!roleChangeResult && (
                <>
                  <FormControl fullWidth sx={{ mb: 2 }}>
                    <InputLabel>New Role</InputLabel>
                    <Select
                      value={newRole}
                      label="New Role"
                      onChange={(e) => setNewRole(e.target.value)}
                      disabled={roleChangeLoading}
                    >
                      <MenuItem value="volunteer">Volunteer</MenuItem>
                      <MenuItem value="admin">Admin</MenuItem>
                      <MenuItem value="superadmin">Super Admin</MenuItem>
                    </Select>
                  </FormControl>
                  
                  {newRole !== selectedUserForRole.role.toLowerCase() && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                      <Typography variant="body2">
                        <strong>Warning:</strong> Changing a user's role will immediately affect their access permissions.
                        {newRole === 'superadmin' && (
                          <><br/>Super Admin role grants full system access including the ability to manage all users and data.</>
                        )}
                      </Typography>
                    </Alert>
                  )}
                </>
              )}

              {roleChangeResult && roleChangeResult.success && (
                <Alert severity="success" sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Role changed successfully!
                  </Typography>
                  <Typography variant="body2">
                    {roleChangeResult.userName}'s role has been changed from {roleChangeResult.oldRole} to {roleChangeResult.newRole}.
                  </Typography>
                </Alert>
              )}

              {roleChangeResult && !roleChangeResult.success && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {roleChangeResult.error}
                </Alert>
              )}
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setChangeRoleDialog(false)} disabled={roleChangeLoading}>
            {roleChangeResult?.success ? 'Close' : 'Cancel'}
          </Button>
          {!roleChangeResult?.success && newRole !== selectedUserForRole?.role.toLowerCase() && (
            <Button 
              onClick={confirmRoleChange} 
              variant="contained" 
              color="warning"
              disabled={roleChangeLoading}
              startIcon={roleChangeLoading ? <CircularProgress size={20} /> : <SwapHoriz />}
            >
              {roleChangeLoading ? 'Changing...' : 'Change Role'}
            </Button>
          )}
        </DialogActions>
      </Dialog>

      {/* Contact Modal for Nearest Voter */}
      <ContactModal
        open={contactModalOpen}
        voter={selectedVoterForContact}
        onClose={() => {
          setContactModalOpen(false);
          setSelectedVoterForContact(null);
        }}
        onSubmit={handleContactSubmit}
        user={user}
      />

      {/* Phone Contact Modal */}
      <PhoneContactModal
        open={phoneContactModalOpen}
        voter={selectedVoterForContact}
        onClose={() => {
          setPhoneContactModalOpen(false);
          setSelectedVoterForContact(null);
        }}
        onSubmit={handlePhoneContactSubmit}
        user={user}
      />

      {/* Resources Dialog */}
      <Dialog open={resourcesDialog} onClose={() => setResourcesDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Volunteer Resources</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
            Campaign information, resources, and support for volunteers.
          </Typography>
          <VolunteerResourcesSection showQuickTips={true} showQRCode={true} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResourcesDialog(false)}>
            Close
          </Button>
        </DialogActions>
      </Dialog>

      {/* Confirmation Dialog */}
      <Dialog open={confirmDialog} onClose={() => setConfirmDialog(false)}>
        <DialogTitle>Confirm Action</DialogTitle>
        <DialogContent>
          <Typography>
            {actionToConfirm?.message}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmDialog(false)}>
            Cancel
          </Button>
          <Button 
            onClick={() => {
              setConfirmDialog(false);
              if (actionToConfirm?.type === 'toggle-status') {
                executeToggleStatus(actionToConfirm.user);
              }
            }}
            variant="contained"
            color={actionToConfirm?.action === 'deactivate' ? 'error' : 'success'}
          >
            {actionToConfirm?.actionText}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Approve Volunteer Dialog */}
      <Dialog open={approveDialog} onClose={() => !approvalLoading && setApproveDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Approve Volunteer Registration</DialogTitle>
        <DialogContent>
          {selectedPendingVolunteer && (
            <>
              <Typography variant="body1" paragraph>
                Are you sure you want to approve the volunteer registration for:
              </Typography>
              <Typography variant="h6" sx={{ mb: 2 }}>
                {selectedPendingVolunteer.firstName} {selectedPendingVolunteer.lastName}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Email: {selectedPendingVolunteer.email}
                <br />
                Phone: {selectedPendingVolunteer.phoneNumber}
                <br />
                Requested Role: {selectedPendingVolunteer.requestedRole}
              </Typography>
              
              <TextField
                label="Welcome Message (Optional)"
                multiline
                rows={3}
                fullWidth
                value={adminNotes}
                onChange={(e) => setAdminNotes(e.target.value)}
                disabled={approvalLoading}
                placeholder="Welcome to the team! We're excited to have you on board..."
                helperText="This message will be included in the approval email"
              />
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setApproveDialog(false)} disabled={approvalLoading}>
            Cancel
          </Button>
          <Button 
            onClick={() => handleApproveVolunteer(selectedPendingVolunteer, adminNotes)} 
            variant="contained" 
            color="success"
            disabled={approvalLoading}
            startIcon={approvalLoading ? <CircularProgress size={20} /> : undefined}
          >
            {approvalLoading ? 'Approving...' : 'Approve'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Reject Volunteer Dialog */}
      <Dialog open={rejectDialog} onClose={() => !approvalLoading && setRejectDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Reject Volunteer Registration</DialogTitle>
        <DialogContent>
          {selectedPendingVolunteer && (
            <>
              <Typography variant="body1" paragraph>
                Are you sure you want to reject the volunteer registration for:
              </Typography>
              <Typography variant="h6" sx={{ mb: 2 }}>
                {selectedPendingVolunteer.firstName} {selectedPendingVolunteer.lastName}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Email: {selectedPendingVolunteer.email}
                <br />
                Phone: {selectedPendingVolunteer.phoneNumber}
                <br />
                Requested Role: {selectedPendingVolunteer.requestedRole}
              </Typography>
              
              <TextField
                label="Reason for Rejection"
                multiline
                rows={3}
                fullWidth
                value={adminNotes}
                onChange={(e) => setAdminNotes(e.target.value)}
                disabled={approvalLoading}
                required
                placeholder="Please provide a reason for rejecting this application..."
                helperText="This message will be included in the rejection email"
                error={!adminNotes.trim() && adminNotes !== ''}
              />
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRejectDialog(false)} disabled={approvalLoading}>
            Cancel
          </Button>
          <Button 
            onClick={() => handleRejectVolunteer(selectedPendingVolunteer, adminNotes)} 
            variant="contained" 
            color="error"
            disabled={approvalLoading || !adminNotes.trim()}
            startIcon={approvalLoading ? <CircularProgress size={20} /> : undefined}
          >
            {approvalLoading ? 'Rejecting...' : 'Reject'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Mobile Engagement FAB */}
      {isMobile && currentTab !== getTabIndex('history') && (
        <Fab
          color="primary"
          aria-label="engagement"
          onClick={() => setCurrentTab(getTabIndex('history'))}
          sx={{
            position: 'fixed',
            bottom: 16,
            right: 16,
            zIndex: 1000,
          }}
        >
          <Email />
        </Fab>
      )}

      {/* Delete User Dialog */}
      <Dialog open={deleteDialog} onClose={() => !deleteLoading && setDeleteDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ color: 'error.main' }}>⚠️ Delete User</DialogTitle>
        <DialogContent>
          {selectedUserForDelete && (
            <>
              <Alert severity="error" sx={{ mb: 3 }}>
                <Typography variant="body2">
                  <strong>This action cannot be undone!</strong>
                  <br />
                  This will permanently delete the user and all associated data.
                </Typography>
              </Alert>
              
              <Typography variant="body1" paragraph>
                You are about to permanently delete:
              </Typography>
              <Typography variant="h6" sx={{ mb: 2 }}>
                {selectedUserForDelete.firstName} {selectedUserForDelete.lastName}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Email: {selectedUserForDelete.email}
                <br />
                Role: {selectedUserForDelete.role}
                <br />
                Contacts Made: {selectedUserForDelete.contactCount || 0}
              </Typography>
              
              {selectedUserForDelete.contactCount > 0 && (
                <Alert severity="warning" sx={{ mb: 3 }}>
                  <Typography variant="body2">
                    This user has made {selectedUserForDelete.contactCount} contacts. 
                    Deletion is not allowed for users with contact history.
                  </Typography>
                </Alert>
              )}
              
              {selectedUserForDelete.contactCount === 0 && (
                <>
                  <Typography variant="body2" paragraph sx={{ mt: 3 }}>
                    To confirm deletion, type the user's email address exactly:
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace', mb: 1, display: 'block' }}>
                    {selectedUserForDelete.email}
                  </Typography>
                  <TextField
                    fullWidth
                    label="Type email to confirm"
                    value={deleteConfirmText}
                    onChange={(e) => setDeleteConfirmText(e.target.value)}
                    disabled={deleteLoading}
                    error={deleteResult?.error ? true : false}
                    helperText={deleteResult?.error}
                    autoFocus
                  />
                </>
              )}
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialog(false)} disabled={deleteLoading}>
            Cancel
          </Button>
          {selectedUserForDelete?.contactCount === 0 && (
            <Button 
              onClick={() => handleDeleteUser(selectedUserForDelete)} 
              variant="contained" 
              color="error"
              disabled={deleteLoading || deleteConfirmText !== selectedUserForDelete?.email}
              startIcon={deleteLoading ? <CircularProgress size={20} /> : <Delete />}
            >
              {deleteLoading ? 'Deleting...' : 'Delete User'}
            </Button>
          )}
        </DialogActions>
      </Dialog>
      
      {/* Tag Dialog */}
      <Dialog open={tagDialog} onClose={() => {
        if (!tagLoading) {
          setTagDialog(false);
          setEditingTag(null);
          setTagForm({ tagName: '', description: '', color: '#2196F3' });
        }
      }} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingTag ? 'Edit Tag' : 'Create New Tag'}
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            {editingTag ? 'Update the tag details below.' : 'Create a new tag to organize voters for targeted campaigns.'}
          </Typography>
          
          <TextField
            label="Tag Name"
            fullWidth
            margin="normal"
            value={tagForm.tagName}
            onChange={(e) => setTagForm({ ...tagForm, tagName: e.target.value })}
            disabled={tagLoading}
            required
            placeholder="e.g., High Priority, Young Voters, etc."
            autoFocus
          />
          
          <TextField
            label="Description"
            fullWidth
            margin="normal"
            multiline
            rows={3}
            value={tagForm.description}
            onChange={(e) => setTagForm({ ...tagForm, description: e.target.value })}
            disabled={tagLoading}
            placeholder="Optional description of what this tag represents..."
          />
          
          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" gutterBottom>
              Tag Color
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <TextField
                type="color"
                value={tagForm.color}
                onChange={(e) => setTagForm({ ...tagForm, color: e.target.value })}
                disabled={tagLoading}
                sx={{ width: 80 }}
              />
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                {[
                  '#2196F3', '#4CAF50', '#FF9800', '#F44336', '#9C27B0',
                  '#607D8B', '#795548', '#E91E63', '#00BCD4', '#8BC34A'
                ].map((color) => (
                  <Box
                    key={color}
                    sx={{
                      width: 24,
                      height: 24,
                      borderRadius: '50%',
                      backgroundColor: color,
                      cursor: 'pointer',
                      border: tagForm.color === color ? '2px solid #000' : '1px solid #ddd'
                    }}
                    onClick={() => setTagForm({ ...tagForm, color })}
                  />
                ))}
              </Box>
            </Box>
          </Box>
          
          <Box sx={{ mt: 2, p: 2, bgcolor: 'action.hover', borderRadius: 1 }}>
            <Typography variant="body2" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Box
                sx={{
                  width: 12,
                  height: 12,
                  borderRadius: '50%',
                  backgroundColor: tagForm.color
                }}
              />
              Preview: {tagForm.tagName || 'Tag Name'}
            </Typography>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setTagDialog(false);
            setEditingTag(null);
            setTagForm({ tagName: '', description: '', color: '#2196F3' });
          }} disabled={tagLoading}>
            Cancel
          </Button>
          <Button 
            onClick={editingTag ? handleUpdateTag : handleCreateTag}
            variant="contained" 
            disabled={tagLoading || !tagForm.tagName.trim()}
            startIcon={tagLoading ? <CircularProgress size={20} /> : (editingTag ? <Edit /> : <Add />)}
          >
            {tagLoading ? (editingTag ? 'Updating...' : 'Creating...') : (editingTag ? 'Update Tag' : 'Create Tag')}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Resource Dialog */}
      <Dialog open={editResourceDialog} onClose={() => setEditResourceDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          Edit {editingResourceType === 'quickTips' ? 'Quick Tips' : 'Volunteer Script'}
        </DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            multiline
            rows={10}
            value={editResourceContent}
            onChange={(e) => setEditResourceContent(e.target.value)}
            placeholder={editingResourceType === 'quickTips' 
              ? 'Enter quick tips for volunteers...'
              : 'Enter the volunteer script...'
            }
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditResourceDialog(false)} disabled={resourceSaving}>
            Cancel
          </Button>
          <Button 
            onClick={handleSaveResource} 
            variant="contained" 
            disabled={resourceSaving}
            startIcon={resourceSaving ? <CircularProgress size={20} /> : undefined}
          >
            {resourceSaving ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Staging Import Dialog */}
      <Dialog 
        open={stagingImportDialog} 
        onClose={() => setStagingImportDialog(false)} 
        maxWidth="lg" 
        fullWidth
      >
        <DialogTitle>
          Advanced CSV Import
          <IconButton
            onClick={() => setStagingImportDialog(false)}
            sx={{ position: 'absolute', right: 8, top: 8 }}
          >
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent>
          <VoterStagingImport 
            onComplete={() => {
              setStagingImportDialog(false);
              // Optionally refresh voter data
              if (currentTab === 3) {
                // Trigger refresh of voter list if on voters tab
                window.location.reload();
              }
            }}
          />
        </DialogContent>
      </Dialog>

      {/* Version Information */}
      <Container maxWidth="lg" sx={{ pb: 2 }}>
        <VersionInfo />
      </Container>
    </Box>
  );
};

export default AdminDashboard;