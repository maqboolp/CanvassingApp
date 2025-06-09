import React, { useState, useEffect } from 'react';
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
  Tab
} from '@mui/material';
import {
  ExitToApp,
  AccountCircle,
  HowToVote,
  ContactPhone,
  Analytics,
  LocationOn,
  Refresh,
  Lock,
  EmojiEvents,
  Star
} from '@mui/icons-material';
import { AuthUser, Voter, ContactStatus, VoterSupport } from '../types';
import VoterList from './VoterList';
import ContactModal from './ContactModal';
import { API_BASE_URL } from '../config';

interface DashboardProps {
  user: AuthUser;
  onLogout: () => void;
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
  const [changePasswordDialog, setChangePasswordDialog] = useState(false);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [passwordChangeLoading, setPasswordChangeLoading] = useState(false);
  const [passwordResult, setPasswordResult] = useState<any>(null);
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [selectedVoterForContact, setSelectedVoterForContact] = useState<Voter | null>(null);
  const [leaderboard, setLeaderboard] = useState<any>(null);
  const [leaderboardTab, setLeaderboardTab] = useState(0);
  const [avatarInfoDialog, setAvatarInfoDialog] = useState(false);
  const [avatarInfo, setAvatarInfo] = useState<any>(null);
  const [debugStats, setDebugStats] = useState<any>(null);

  useEffect(() => {
    fetchStats();
    fetchLeaderboard();
    getCurrentLocation();
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

  const getCurrentLocation = () => {
    console.log('Dashboard: Getting current location for user role:', user.role);
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          const coords = {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude
          };
          console.log('Dashboard: Got location coordinates:', coords);
          setLocation(coords);
          findNearestVoter(coords);
        },
        (error) => {
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
          timeout: 10000,
          maximumAge: 300000 // 5 minutes
        }
      );
    } else {
      console.log('Dashboard: Geolocation not supported');
    }
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

  const handleContactSubmit = async (status: ContactStatus, notes: string, voterSupport?: VoterSupport) => {
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
            src="/campaign-logo.png" 
            alt="Tanveer Patel for Hoover City Council" 
            style={{ 
              height: '40px', 
              marginRight: '16px'
            }} 
          />
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h6" component="div">
              Canvassing Portal
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.8 }}>
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
          <Alert severity="warning" sx={{ mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>
              📍 Location needed for nearest voter
            </Typography>
            <Typography variant="body2" sx={{ mb: 2 }}>
              To see your nearest uncontacted voter, we need your location.
            </Typography>
            <Button
              variant="contained"
              startIcon={<LocationOn />}
              onClick={getCurrentLocation}
              size="small"
            >
              Enable Location
            </Button>
          </Alert>
        )}

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
                sx={{ mr: 1 }} 
              />
              <Chip 
                label={`ZIP: ${nearestVoter.voter.zip}`} 
                size="small" 
                sx={{ mr: 1 }} 
              />
              {nearestVoter.voter.cellPhone && (
                <Chip 
                  label={nearestVoter.voter.cellPhone} 
                  size="small" 
                  color="primary"
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

        {/* Voter List */}
        <VoterList onContactVoter={handleContactVoter} user={user} />
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
            type="password"
            fullWidth
            margin="normal"
            value={passwordForm.currentPassword}
            onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
            disabled={passwordChangeLoading}
            required
          />
          <TextField
            label="New Password"
            type="password"
            fullWidth
            margin="normal"
            value={passwordForm.newPassword}
            onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
            disabled={passwordChangeLoading}
            required
            helperText="Minimum 6 characters"
          />
          <TextField
            label="Confirm New Password"
            type="password"
            fullWidth
            margin="normal"
            value={passwordForm.confirmPassword}
            onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
            disabled={passwordChangeLoading}
            required
            error={passwordForm.confirmPassword !== '' && passwordForm.newPassword !== passwordForm.confirmPassword}
            helperText={passwordForm.confirmPassword !== '' && passwordForm.newPassword !== passwordForm.confirmPassword ? "Passwords do not match" : ""}
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
      />
    </Box>
  );
};

export default Dashboard;