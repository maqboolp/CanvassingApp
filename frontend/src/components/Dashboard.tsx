import React, { useState, useEffect } from 'react';
import QRCode from 'react-qr-code';
import VersionInfo from './VersionInfo';
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
  Chip,
  Badge,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Avatar,
  Tabs,
  Tab,
  InputAdornment
} from '@mui/material';
import {
  ExitToApp,
  AccountCircle,
  ContactPhone,
  LocationOn,
  Refresh,
  Lock,
  EmojiEvents,
  Star,
  Language,
  Visibility,
  VisibilityOff,
  Edit,
  VideoLibrary,
  Payment,
  HowToReg,
  Phone,
  Help,
  OpenInNew
} from '@mui/icons-material';
import { AuthUser, Voter, ContactStatus, VoterSupport } from '../types';
import VoterList from './VoterList';
import ContactModal from './ContactModal';
import VolunteerResourcesSection from './VolunteerResourcesSection';
import { API_BASE_URL } from '../config';
import { customerConfig, campaignConfig } from '../config/customerConfig';

// Get customer-specific campaign info
const campaignWebsite = process.env.REACT_APP_CAMPAIGN_WEBSITE;
const campaignVenmo = process.env.REACT_APP_CAMPAIGN_VENMO;
const campaignYoutube = process.env.REACT_APP_CAMPAIGN_YOUTUBE;

// Get customer-specific voter resources
const voterRegistrationUrl = process.env.REACT_APP_VOTER_REGISTRATION_URL;
const volunteerHotline = process.env.REACT_APP_VOLUNTEER_HOTLINE;

interface DashboardProps {
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
      id={`dashboard-tabpanel-${index}`}
      aria-labelledby={`dashboard-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ py: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

const Dashboard: React.FC<DashboardProps> = ({ user, onLogout }) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [stats, setStats] = useState({
    totalAssigned: 0,
    contacted: 0,
    contactsToday: 0
  });
  const [nearestVoter, setNearestVoter] = useState<{ voter: Voter; distance: number } | null>(null);
  const [location, setLocation] = useState<{ latitude: number; longitude: number } | null>(null);
  const [locationLoading, setLocationLoading] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [changePasswordDialog, setChangePasswordDialog] = useState(false);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [passwordChangeLoading, setPasswordChangeLoading] = useState(false);
  const [passwordResult, setPasswordResult] = useState<any>(null);
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [selectedVoterForContact, setSelectedVoterForContact] = useState<Voter | null>(null);
  const [leaderboard, setLeaderboard] = useState<any>(null);
  const [leaderboardTab, setLeaderboardTab] = useState(0);
  const [avatarInfoDialog, setAvatarInfoDialog] = useState(false);
  const [avatarInfo, setAvatarInfo] = useState<any>(null);
  const [debugStats, setDebugStats] = useState<any>(null);
  const [resourcesDialog, setResourcesDialog] = useState(false);
  const [currentTab, setCurrentTab] = useState(0);
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

  useEffect(() => {
    fetchStats();
    fetchLeaderboard();
    getCurrentLocation();
    fetchVolunteerResources();
    if (user.role === 'admin' || user.role === 'superadmin') {
      fetchDebugStats();
    }
  }, []);

  const fetchStats = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/volunteers/stats`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setStats(data);
      }
    } catch (error) {
      console.error('Failed to fetch stats:', error);
    }
  };

  const fetchLeaderboard = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/volunteers/leaderboard`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setLeaderboard(data);
      }
    } catch (error) {
      console.error('Failed to fetch leaderboard:', error);
    }
  };

  const fetchDebugStats = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/voters/debug-stats`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        console.log('Debug stats:', data);
        setDebugStats(data);
      }
    } catch (error) {
      console.error('Failed to fetch debug stats:', error);
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

  const getCurrentLocation = () => {
    console.log('Dashboard: Getting current location for user role:', user.role);
    setLocationLoading(true);
    setLocationError(null);
    
    if (!navigator.geolocation) {
      setLocationError('Geolocation is not supported by this browser');
      setLocationLoading(false);
      console.log('Dashboard: Geolocation not supported');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const coords = {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude
        };
        console.log('Dashboard: Got location coordinates:', coords);
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
        
        console.log('Dashboard: Geolocation error details:', {
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

  const openInMaps = (voter: Voter) => {
    const address = `${voter.addressLine}, ${voter.city}, ${voter.state} ${voter.zip}`;
    const encodedAddress = encodeURIComponent(address);
    
    // Detect if it's iOS/Safari for Apple Maps, otherwise use Google Maps
    const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent);
    const isSafari = /^((?!chrome|android).)*safari/i.test(navigator.userAgent);
    
    let mapUrl;
    if (isIOS || isSafari) {
      // Use Apple Maps for iOS/Safari
      mapUrl = `maps://maps.apple.com/?q=${encodedAddress}`;
    } else {
      // Use Google Maps for other browsers
      mapUrl = `https://www.google.com/maps/search/?api=1&query=${encodedAddress}`;
    }
    
    window.open(mapUrl, '_blank');
  };

  const findNearestVoter = async (coords: { latitude: number; longitude: number }) => {
    try {
      console.log('Dashboard: Finding nearest voter for coords:', coords, 'user role:', user.role);
      const response = await fetch(
        `${API_BASE_URL}/api/voters/nearest?latitude=${coords.latitude}&longitude=${coords.longitude}`,
        {
          headers: {
            'Authorization': `Bearer ${user.token}`
          }
        }
      );
      
      console.log('Dashboard: Nearest voter API response status:', response.status);
      
      if (response.ok) {
        const data = await response.json();
        console.log('Dashboard: Nearest voter data received:', data);
        setNearestVoter(data);
      } else if (response.status === 404) {
        // No voters found is a normal case, not an error
        console.log('Dashboard: No nearest voter found (404)');
        setNearestVoter(null);
      } else {
        console.log('Dashboard: Nearest voter API error:', response.status, await response.text());
      }
    } catch (error) {
      console.error('Dashboard: Failed to find nearest voter:', error);
    }
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleContactVoter = (voter: Voter) => {
    // Refresh stats after contact is logged
    fetchStats();
    
    // If this was the nearest voter, find a new one
    if (nearestVoter && voter.lalVoterId === nearestVoter.voter.lalVoterId && location) {
      findNearestVoter(location);
    }
  };

  const handleChangePassword = async () => {
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordResult({ error: 'New passwords do not match' });
      return;
    }

    if (passwordForm.newPassword.length < 6) {
      setPasswordResult({ error: 'New password must be at least 6 characters long' });
      return;
    }

    setPasswordChangeLoading(true);
    setPasswordResult(null);
    
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
        setPasswordResult({ success: 'Password changed successfully!' });
        setChangePasswordDialog(false);
        setPasswordForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
      } else {
        const error = await response.json();
        setPasswordResult({ error: error.error || 'Failed to change password' });
      }
    } catch (error) {
      setPasswordResult({ error: 'Failed to change password: ' + (error as Error).message });
    } finally {
      setPasswordChangeLoading(false);
    }
  };

  const handleNearestVoterContact = (voter: Voter) => {
    setSelectedVoterForContact(voter);
    setContactModalOpen(true);
  };

  const fetchAvatarInfo = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/avatar-info`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setAvatarInfo(data);
      }
    } catch (error) {
      console.error('Failed to fetch avatar info:', error);
    }
  };

  const handleAvatarInfoOpen = () => {
    fetchAvatarInfo();
    setAvatarInfoDialog(true);
    handleMenuClose();
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
      fetchStats();
      if (location) {
        findNearestVoter(location);
      }
    } catch (err) {
      console.error('Failed to log contact:', err);
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
              Canvassing Portal
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.8, color: 'white' }}>
              Logged in as {user.firstName} {user.lastName} ({user.role.charAt(0).toUpperCase() + user.role.slice(1)})
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
            <Avatar 
              src={user.avatarUrl} 
              alt={`${user.firstName} ${user.lastName}`}
              sx={{ width: 32, height: 32 }}
            />
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
                {user.firstName} {user.lastName}
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
            <MenuItem onClick={handleAvatarInfoOpen}>
              <AccountCircle sx={{ mr: 1 }} />
              Change Avatar
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

      <Container maxWidth="lg" sx={{ mt: 3, mb: 3 }}>
        {/* Stats Cards */}
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: 3, mb: 3 }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="body2">
                    Contacted
                  </Typography>
                  <Typography variant="h4">
                    {stats.contacted}
                  </Typography>
                </Box>
                <ContactPhone sx={{ fontSize: 40, color: '#00b090' }} />
              </Box>
            </CardContent>
          </Card>
          
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="body2">
                    Today's Contacts
                  </Typography>
                  <Typography variant="h4">
                    {stats.contactsToday}
                  </Typography>
                </Box>
                <Badge badgeContent={stats.contactsToday} color="secondary">
                  <ContactPhone sx={{ fontSize: 40, color: '#ffcd35' }} />
                </Badge>
              </Box>
            </CardContent>
          </Card>
        </Box>

        {/* Debug Stats Card (Admin/SuperAdmin only) */}
        {debugStats && (user.role === 'admin' || user.role === 'superadmin') && (
          <Alert severity="info" sx={{ mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>
              🔍 Debug Info (Admin Only)
            </Typography>
            <Typography variant="body2">
              Total voters: {debugStats.totalVoters} | 
              With coordinates: {debugStats.votersWithCoordinates} | 
              Uncontacted: {debugStats.uncontactedVoters} | 
              Uncontacted with coords: {debugStats.uncontactedVotersWithCoordinates}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {debugStats.message}
            </Typography>
          </Alert>
        )}

        {/* Location Debug Card */}
        {!nearestVoter && !location && (
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
            {locationError && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  💡 Tip: Make sure to allow location permissions when your browser asks. Location requests may take up to 30 seconds in some cases.
                </Typography>
              </Box>
            )}
          </Alert>
        )}

        {/* Nearest Voter Card Debug */}
        {console.log('Dashboard render - nearestVoter:', nearestVoter, 'location:', location, 'user role:', user.role)}
        
        {/* Nearest Voter Card */}
        {nearestVoter && (
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
              📍 {nearestVoter.distance.toFixed(2)} km away
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
              {nearestVoter.voter.cellPhone && (
                <Chip 
                  label={nearestVoter.voter.cellPhone} 
                  size="small" 
                  color="primary"
                  sx={{ mb: 1 }}
                />
              )}
            </Box>
            <Box sx={{ mt: 2 }}>
              <Button
                variant="contained"
                startIcon={<ContactPhone />}
                onClick={() => handleNearestVoterContact(nearestVoter.voter)}
                fullWidth
              >
                Contact Voter
              </Button>
            </Box>
          </Alert>
        )}

        {/* Password Change Result Alert */}
        {passwordResult && (
          <Alert 
            severity={passwordResult.error ? 'error' : 'success'} 
            sx={{ mb: 3 }}
            onClose={() => setPasswordResult(null)}
          >
            {passwordResult.error || passwordResult.success}
          </Alert>
        )}

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

        {/* Tabs */}
        <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
          <Tabs value={currentTab} onChange={(e, newValue) => setCurrentTab(newValue)}>
            <Tab label="Voters" />
            <Tab label="Resources" />
          </Tabs>
        </Box>

        {/* Voters Tab */}
        <TabPanel value={currentTab} index={0}>
          <VoterList onContactVoter={handleContactVoter} user={user} />
        </TabPanel>

        {/* Resources Tab */}
        <TabPanel value={currentTab} index={1}>
          <Typography variant="h5" gutterBottom>
            Volunteer Resources
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            Campaign information, resources, and support for volunteers.
          </Typography>
          
          {/* Additional Resource Links */}
          <ResourceLinksSection user={user} isAdmin={false} />
          
          {/* Use the shared VolunteerResourcesSection component */}
          <VolunteerResourcesSection showQuickTips={false} showQRCode={true} />
          
          {/* Quick Tips with Edit capability */}
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
                {volunteerResources.quickTips}
              </Typography>
            </CardContent>
          </Card>

          {/* Script with Edit capability */}
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
                {volunteerResources.script.replace('[Volunteer Name]', `${user.firstName} ${user.lastName}`).replace('[Your Name]', `${user.firstName} ${user.lastName}`)}
              </Typography>
            </CardContent>
          </Card>
        </TabPanel>
      </Container>

      {/* Avatar Info Dialog */}
      <Dialog open={avatarInfoDialog} onClose={() => setAvatarInfoDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Change Your Avatar</DialogTitle>
        <DialogContent>
          {avatarInfo && (
            <>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                <Avatar 
                  src={avatarInfo.avatarUrl} 
                  alt={`${user.firstName} ${user.lastName}`}
                  sx={{ width: 80, height: 80, mr: 2 }}
                />
                <Box>
                  <Typography variant="h6">{user.firstName} {user.lastName}</Typography>
                  <Typography variant="body2" color="text.secondary">{avatarInfo.email}</Typography>
                </Box>
              </Box>
              
              <Typography variant="body1" paragraph>
                {avatarInfo.gravatarInfo.message}
              </Typography>
              
              <Typography variant="body2" color="text.secondary" paragraph>
                Your avatar is automatically generated based on your email address: <strong>{avatarInfo.gravatarInfo.emailUsed}</strong>
              </Typography>
              
              <Button
                variant="contained"
                startIcon={<AccountCircle />}
                href={avatarInfo.gravatarInfo.gravatarUrl}
                target="_blank"
                rel="noopener noreferrer"
                fullWidth
                sx={{ mb: 2 }}
              >
                Visit Gravatar.com
              </Button>
              
              <Typography variant="caption" color="text.secondary">
                After updating your Gravatar, it may take a few minutes for changes to appear. You can refresh the page to see updates sooner.
              </Typography>
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAvatarInfoDialog(false)}>
            Close
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
            helperText="Minimum 6 characters"
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

      {/* Version Information */}
      <Container maxWidth="sm" sx={{ pb: 2 }}>
        <VersionInfo />
      </Container>
    </Box>
  );
};

export default Dashboard;