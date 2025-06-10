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
  Checkbox
} from '@mui/material';
import {
  ExitToApp,
  AccountCircle,
  HowToVote,
  Upload,
  Analytics,
  People,
  Assignment,
  GetApp,
  LocationOn,
  Lock,
  History,
  VpnKey
} from '@mui/icons-material';
import { AuthUser, Voter, ContactStatus, VoterSupport } from '../types';
import VoterList from './VoterList';
import VoterContactHistory from './VoterContactHistory';
import { API_BASE_URL } from '../config';

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
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const AdminDashboard: React.FC<AdminDashboardProps> = ({ user, onLogout }) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [currentTab, setCurrentTab] = useState(0);
  const [analytics, setAnalytics] = useState<any>(null);
  const [volunteers, setVolunteers] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [importDialog, setImportDialog] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);
  const [importLoading, setImportLoading] = useState(false);
  const [importResult, setImportResult] = useState<any>(null);
  const [enableGeocoding, setEnableGeocoding] = useState(false);
  const [createVolunteerDialog, setCreateVolunteerDialog] = useState(false);
  const [volunteerForm, setVolunteerForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    password: ''
  });
  const [adminForm, setAdminForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    password: ''
  });
  const [createAdminDialog, setCreateAdminDialog] = useState(false);
  const [adminCreateLoading, setAdminCreateLoading] = useState(false);
  const [volunteerCreateLoading, setVolunteerCreateLoading] = useState(false);
  const [geocodingStatus, setGeocodingStatus] = useState<any>(null);
  const [geocodingLoading, setGeocodingLoading] = useState(false);
  const [resetPasswordDialog, setResetPasswordDialog] = useState(false);
  const [selectedVolunteer, setSelectedVolunteer] = useState<any>(null);
  const [resetPasswordResult, setResetPasswordResult] = useState<any>(null);
  const [resetPasswordLoading, setResetPasswordLoading] = useState(false);
  const [geocodingResult, setGeocodingResult] = useState<any>(null);
  const [changePasswordDialog, setChangePasswordDialog] = useState(false);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [passwordChangeLoading, setPasswordChangeLoading] = useState(false);

  useEffect(() => {
    if (currentTab === 0) {
      fetchAnalytics();
    } else if (currentTab === 1) {
      fetchVolunteers();
    } else if (currentTab === 4 && user.role === 'superadmin') {
      fetchGeocodingStatus();
    }
  }, [currentTab, user.role]);

  const fetchAnalytics = async () => {
    setLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/analytics`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setAnalytics(data);
      }
    } catch (error) {
      console.error('Failed to fetch analytics:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchVolunteers = async () => {
    setLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/volunteers`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setVolunteers(data);
      }
    } catch (error) {
      console.error('Failed to fetch volunteers:', error);
    } finally {
      setLoading(false);
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
        if (currentTab === 0) {
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

  const handleCreateVolunteer = async () => {
    if (!volunteerForm.firstName || !volunteerForm.lastName || !volunteerForm.email || !volunteerForm.password) {
      setImportResult({ error: 'All fields are required' });
      return;
    }

    setVolunteerCreateLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify(volunteerForm)
      });

      if (response.ok) {
        const result = await response.json();
        setImportResult({ success: `Volunteer ${volunteerForm.firstName} ${volunteerForm.lastName} created successfully!` });
        setCreateVolunteerDialog(false);
        setVolunteerForm({ firstName: '', lastName: '', email: '', phoneNumber: '', password: '' });
        // Refresh volunteers list if we're on that tab
        if (currentTab === 1) {
          fetchVolunteers();
        }
      } else {
        const error = await response.json();
        setImportResult({ error: error.error || 'Failed to create volunteer' });
      }
    } catch (error) {
      setImportResult({ error: 'Failed to create volunteer: ' + (error as Error).message });
    } finally {
      setVolunteerCreateLoading(false);
    }
  };

  const handleCreateAdmin = async () => {
    setAdminCreateLoading(true);
    setImportResult(null);
    
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
        setImportResult({ success: 'Admin created successfully!' });
        setCreateAdminDialog(false);
        setAdminForm({ firstName: '', lastName: '', email: '', phoneNumber: '', password: '' });
        // Refresh the volunteers list to show the new admin
        if (currentTab === 1) {
          fetchVolunteers();
        }
      } else {
        const error = await response.json();
        setImportResult({ error: error.error || 'Failed to create admin' });
      }
    } catch (error) {
      setImportResult({ error: 'Failed to create admin: ' + (error as Error).message });
    } finally {
      setAdminCreateLoading(false);
    }
  };

  const handleChangePassword = async () => {
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setImportResult({ error: 'New passwords do not match' });
      return;
    }

    if (passwordForm.newPassword.length < 6) {
      setImportResult({ error: 'New password must be at least 6 characters long' });
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
  };

  const confirmResetPassword = async () => {
    if (!selectedVolunteer) return;

    setResetPasswordLoading(true);
    setResetPasswordResult(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/reset-volunteer-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          volunteerId: selectedVolunteer.id
        })
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
              Admin Portal
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.8 }}>
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
            <MenuItem onClick={onLogout}>
              <ExitToApp sx={{ mr: 1 }} />
              Logout
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs value={currentTab} onChange={handleTabChange} aria-label="admin tabs">
          <Tab label="Analytics" icon={<Analytics />} />
          <Tab label="Users" icon={<People />} />
          <Tab label="Voters" icon={<HowToVote />} />
          <Tab label="Contact History" icon={<History />} />
          {user.role === 'superadmin' && (
            <Tab label="Data Management" icon={<Upload />} />
          )}
        </Tabs>
      </Box>

      <Container maxWidth="lg" sx={{ mt: 3, mb: 3 }}>
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

        {/* Analytics Tab */}
        <TabPanel value={currentTab} index={0}>
          {loading ? (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          ) : analytics ? (
            <>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Typography variant="h5">Campaign Analytics</Typography>
                <Button
                  startIcon={<GetApp />}
                  variant="outlined"
                  onClick={handleExportAnalytics}
                >
                  Export CSV
                </Button>
              </Box>

              <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: 3, mb: 3 }}>
                <Card>
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Total Voters
                    </Typography>
                    <Typography variant="h4">
                      {analytics.totalVoters}
                    </Typography>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Contacted
                    </Typography>
                    <Typography variant="h4">
                      {analytics.totalContacted}
                    </Typography>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Contact Rate
                    </Typography>
                    <Typography variant="h4">
                      {analytics.totalVoters > 0 
                        ? Math.round((analytics.totalContacted / analytics.totalVoters) * 100)
                        : 0}%
                    </Typography>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Active Volunteers
                    </Typography>
                    <Typography variant="h4">
                      {analytics.volunteerActivity?.length || 0}
                    </Typography>
                  </CardContent>
                </Card>
              </Box>

              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 3 }}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>
                      Contact Status Breakdown
                    </Typography>
                    <Table size="small">
                      <TableBody>
                        <TableRow>
                          <TableCell>Reached</TableCell>
                          <TableCell align="right">{analytics.contactStatusBreakdown.reached}</TableCell>
                        </TableRow>
                        <TableRow>
                          <TableCell>Not Home</TableCell>
                          <TableCell align="right">{analytics.contactStatusBreakdown.notHome}</TableCell>
                        </TableRow>
                        <TableRow>
                          <TableCell>Refused</TableCell>
                          <TableCell align="right">{analytics.contactStatusBreakdown.refused}</TableCell>
                        </TableRow>
                        <TableRow>
                          <TableCell>Needs Follow-up</TableCell>
                          <TableCell align="right">{analytics.contactStatusBreakdown.needsFollowUp}</TableCell>
                        </TableRow>
                      </TableBody>
                    </Table>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>
                      Volunteer Activity
                    </Typography>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Volunteer</TableCell>
                          <TableCell align="right">Today</TableCell>
                          <TableCell align="right">Total</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {analytics.volunteerActivity?.slice(0, 5).map((volunteer: any) => (
                          <TableRow key={volunteer.volunteerId}>
                            <TableCell>{volunteer.volunteerName}</TableCell>
                            <TableCell align="right">{volunteer.contactsToday}</TableCell>
                            <TableCell align="right">{volunteer.contactsTotal}</TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </CardContent>
                </Card>
              </Box>
            </>
          ) : (
            <Typography>No analytics data available</Typography>
          )}
        </TabPanel>

        {/* Users Tab */}
        <TabPanel value={currentTab} index={1}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Typography variant="h5">
              User Management
            </Typography>
            <Box sx={{ display: 'flex', gap: 2 }}>
              {user.role === 'superadmin' && (
                <Button
                  variant="contained"
                  startIcon={<People />}
                  onClick={() => setCreateAdminDialog(true)}
                  color="secondary"
                >
                  Create Admin
                </Button>
              )}
              <Button
                variant="contained"
                startIcon={<People />}
                onClick={() => setCreateVolunteerDialog(true)}
              >
                Create Volunteer
              </Button>
            </Box>
          </Box>
          
          {loading ? (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
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
                          <TableCell>Joined</TableCell>
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
                              <span style={{ 
                                color: admin.isActive ? 'green' : 'red',
                                fontWeight: 'bold' 
                              }}>
                                {admin.isActive ? 'Active' : 'Inactive'}
                              </span>
                            </TableCell>
                            <TableCell>
                              {new Date(admin.createdAt).toLocaleDateString()}
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
                            <span style={{ 
                              color: volunteer.isActive ? 'green' : 'red',
                              fontWeight: 'bold' 
                            }}>
                              {volunteer.isActive ? 'Active' : 'Inactive'}
                            </span>
                          </TableCell>
                          <TableCell align="right">{volunteer.contactCount}</TableCell>
                          <TableCell>
                            {new Date(volunteer.createdAt).toLocaleDateString()}
                          </TableCell>
                          <TableCell>
                            <Button
                              size="small"
                              variant="outlined"
                              startIcon={<VpnKey />}
                              onClick={() => handleResetPassword(volunteer)}
                              disabled={!volunteer.isActive}
                            >
                              Reset Password
                            </Button>
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

        {/* Voters Tab */}
        <TabPanel value={currentTab} index={2}>
          <Typography variant="h5" gutterBottom>
            Voter Management
          </Typography>
          <VoterList onContactVoter={() => {}} user={user} />
        </TabPanel>

        {/* Contact History Tab */}
        <TabPanel value={currentTab} index={3}>
          <VoterContactHistory user={user} />
        </TabPanel>

        {/* Data Management Tab - Only for SuperAdmins */}
        {user.role === 'superadmin' && (
          <TabPanel value={currentTab} index={4}>
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
                <Button
                  variant="contained"
                  startIcon={<Upload />}
                  onClick={() => setImportDialog(true)}
                >
                  Import CSV
                </Button>
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
            Select a CSV file with voter data. The file should include columns for voter ID, names, addresses, and other voter information.
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

      {/* Create Volunteer Dialog */}
      <Dialog open={createVolunteerDialog} onClose={() => !volunteerCreateLoading && setCreateVolunteerDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Volunteer</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            Create a new volunteer account for canvassing activities.
          </Typography>
          <TextField
            label="First Name"
            fullWidth
            margin="normal"
            value={volunteerForm.firstName}
            onChange={(e) => setVolunteerForm({ ...volunteerForm, firstName: e.target.value })}
            disabled={volunteerCreateLoading}
            required
          />
          <TextField
            label="Last Name"
            fullWidth
            margin="normal"
            value={volunteerForm.lastName}
            onChange={(e) => setVolunteerForm({ ...volunteerForm, lastName: e.target.value })}
            disabled={volunteerCreateLoading}
            required
          />
          <TextField
            label="Email Address"
            type="email"
            fullWidth
            margin="normal"
            value={volunteerForm.email}
            onChange={(e) => setVolunteerForm({ ...volunteerForm, email: e.target.value })}
            disabled={volunteerCreateLoading}
            required
          />
          <TextField
            label="Phone Number"
            type="tel"
            fullWidth
            margin="normal"
            value={volunteerForm.phoneNumber}
            onChange={(e) => setVolunteerForm({ ...volunteerForm, phoneNumber: e.target.value })}
            disabled={volunteerCreateLoading}
            placeholder="(555) 123-4567"
            helperText="Optional - for emergency contact"
          />
          <TextField
            label="Password"
            type="password"
            fullWidth
            margin="normal"
            value={volunteerForm.password}
            onChange={(e) => setVolunteerForm({ ...volunteerForm, password: e.target.value })}
            disabled={volunteerCreateLoading}
            required
            helperText="Minimum 6 characters with at least one digit and lowercase letter"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateVolunteerDialog(false)} disabled={volunteerCreateLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleCreateVolunteer} 
            variant="contained" 
            disabled={volunteerCreateLoading || !volunteerForm.firstName || !volunteerForm.lastName || !volunteerForm.email || !volunteerForm.password}
            startIcon={volunteerCreateLoading ? <CircularProgress size={20} /> : <People />}
          >
            {volunteerCreateLoading ? 'Creating...' : 'Create Volunteer'}
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
            type="password"
            fullWidth
            margin="normal"
            value={adminForm.password}
            onChange={(e) => setAdminForm({ ...adminForm, password: e.target.value })}
            disabled={adminCreateLoading}
            required
            helperText="Minimum 6 characters with at least one digit and lowercase letter"
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

      {/* Reset Password Dialog */}
      <Dialog open={resetPasswordDialog} onClose={() => !resetPasswordLoading && setResetPasswordDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Reset Volunteer Password</DialogTitle>
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
                <Alert severity="warning" sx={{ mb: 2 }}>
                  A new temporary password will be generated. Please share this password securely with the volunteer.
                </Alert>
              )}

              {resetPasswordResult && resetPasswordResult.success && (
                <Alert severity="success" sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Password reset successful!
                  </Typography>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    <strong>Temporary Password:</strong> 
                    <Box component="code" sx={{ 
                      bgcolor: 'background.paper', 
                      p: 1, 
                      borderRadius: 1, 
                      ml: 1,
                      fontFamily: 'monospace',
                      fontSize: '1.1em',
                      fontWeight: 'bold'
                    }}>
                      {resetPasswordResult.temporaryPassword}
                    </Box>
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Please share this password securely with {selectedVolunteer.firstName}. 
                    They should change it immediately after logging in.
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
    </Box>
  );
};

export default AdminDashboard;