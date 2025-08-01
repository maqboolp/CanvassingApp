import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Typography,
  Card,
  CardContent,
  Button,
  Chip,
  Alert,
  IconButton,
  TextField,
  InputAdornment,
  CircularProgress,
  Tooltip,
  Paper,
  Divider,
  AppBar,
  Toolbar
} from '@mui/material';
import {
  Phone,
  NavigateNext,
  LocationOn,
  Person,
  Search,
  Timer,
  CheckCircle,
  Cancel,
  PhoneInTalk,
  History,
  Refresh,
  ArrowBack,
  Dashboard as DashboardIcon
} from '@mui/icons-material';
import { Voter, AuthUser } from '../types';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';
import PhoneContactModal, { PhoneContactStatus } from './PhoneContactModal';
import dayjs from 'dayjs';
import { customerConfig } from '../config/customerConfig';

interface PhoneBankingProps {
  user: AuthUser;
}

const PhoneBanking: React.FC<PhoneBankingProps> = ({ user }) => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentVoter, setCurrentVoter] = useState<Voter | null>(null);
  const [recentContacts, setRecentContacts] = useState<any[]>([]);
  const [stats, setStats] = useState({
    todayCalls: 0,
    todayReached: 0,
    todayVoicemails: 0
  });
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [searchZip, setSearchZip] = useState('');
  const [noMoreVoters, setNoMoreVoters] = useState(false);

  const fetchNextVoter = async () => {
    setLoading(true);
    setError(null);
    setNoMoreVoters(false);
    
    try {
      let url = `${API_BASE_URL}/api/voters/next-to-call`;
      if (searchZip) {
        url += `?zip=${searchZip}`;
      }
      
      const response = await ApiErrorHandler.makeAuthenticatedRequest(url, {
        method: 'GET'
      });
      
      if (response) {
        setCurrentVoter(response);
      } else {
        setNoMoreVoters(true);
      }
    } catch (err: any) {
      if (err.status === 404) {
        setNoMoreVoters(true);
      } else {
        setError('Failed to fetch next voter');
        console.error('Error fetching next voter:', err);
      }
    } finally {
      setLoading(false);
    }
  };

  const fetchStats = async () => {
    try {
      const today = new Date().toISOString().split('T')[0];
      const response = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/phonecontacts/my-contacts?date=${today}`,
        { method: 'GET' }
      );
      
      if (response) {
        setStats({
          todayCalls: response.totalContacts,
          todayReached: response.contactsByStatus['Reached'] || 0,
          todayVoicemails: response.contactsByStatus['VoiceMail'] || 0
        });
        setRecentContacts(response.contacts.slice(0, 5));
      }
    } catch (err) {
      console.error('Error fetching stats:', err);
    }
  };

  useEffect(() => {
    fetchNextVoter();
    fetchStats();
  }, []);

  const handleContactSubmit = async (
    status: PhoneContactStatus, 
    notes: string, 
    voterSupport?: any, 
    callDuration?: number,
    audioUrl?: string, 
    audioDuration?: number
  ) => {
    if (!currentVoter) return;

    try {
      await ApiErrorHandler.makeAuthenticatedRequest(`${API_BASE_URL}/api/phonecontacts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          voterId: currentVoter.lalVoterId,
          status,
          voterSupport,
          notes,
          audioFileUrl: audioUrl,
          audioDurationSeconds: audioDuration,
          callDurationSeconds: callDuration
        })
      });

      // Refresh stats and get next voter
      await fetchStats();
      await fetchNextVoter();
    } catch (err) {
      setError('Failed to save contact');
      console.error('Error saving contact:', err);
    }
  };

  const skipVoter = () => {
    fetchNextVoter();
  };

  const formatPhoneNumber = (phone: string | null | undefined): string => {
    if (!phone) return 'No phone number';
    const cleaned = phone.replace(/\D/g, '');
    if (cleaned.length === 10) {
      return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    }
    return phone;
  };

  return (
    <Box sx={{ flexGrow: 1 }}>
      {/* App Bar */}
      <AppBar position="static">
        <Toolbar>
          <IconButton
            size="large"
            edge="start"
            color="inherit"
            aria-label="back to dashboard"
            onClick={() => navigate('/')}
            sx={{ mr: 2 }}
          >
            <ArrowBack />
          </IconButton>
          
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
              Phone Banking
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.8, color: 'white' }}>
              {user.firstName} {user.lastName} - Making calls
            </Typography>
          </Box>
          
          <Button
            color="inherit"
            startIcon={<DashboardIcon />}
            onClick={() => navigate('/dashboard')}
            sx={{ mr: 2 }}
          >
            Dashboard
          </Button>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 3 }}>
        <Typography variant="h4" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Phone color="primary" />
          Phone Banking
        </Typography>

      {/* Stats Cards */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        <Box sx={{ flex: '1 1 300px' }}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <PhoneInTalk color="primary" sx={{ fontSize: 40 }} />
              <Typography variant="h4">{stats.todayCalls}</Typography>
              <Typography color="textSecondary">Calls Today</Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: '1 1 300px' }}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <CheckCircle color="success" sx={{ fontSize: 40 }} />
              <Typography variant="h4">{stats.todayReached}</Typography>
              <Typography color="textSecondary">Reached Today</Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: '1 1 300px' }}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Timer color="warning" sx={{ fontSize: 40 }} />
              <Typography variant="h4">{stats.todayVoicemails}</Typography>
              <Typography color="textSecondary">Voicemails Today</Typography>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* Search Box */}
      <Box sx={{ mb: 3 }}>
        <TextField
          placeholder="Search by ZIP code (optional)"
          value={searchZip}
          onChange={(e) => setSearchZip(e.target.value)}
          onKeyPress={(e) => {
            if (e.key === 'Enter') {
              fetchNextVoter();
            }
          }}
          size="small"
          sx={{ width: 300 }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search />
              </InputAdornment>
            ),
            endAdornment: searchZip && (
              <InputAdornment position="end">
                <IconButton size="small" onClick={() => { setSearchZip(''); fetchNextVoter(); }}>
                  <Cancel />
                </IconButton>
              </InputAdornment>
            ),
          }}
        />
      </Box>

      {/* Current Voter Card */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {loading ? (
        <Box display="flex" justifyContent="center" py={4}>
          <CircularProgress />
        </Box>
      ) : noMoreVoters ? (
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 4 }}>
            <Typography variant="h6" gutterBottom>
              No more voters to call
            </Typography>
            <Typography color="textSecondary" gutterBottom>
              {searchZip ? `No voters found in ZIP ${searchZip}` : 'All voters have been contacted'}
            </Typography>
            <Button
              startIcon={<Refresh />}
              onClick={() => { setSearchZip(''); fetchNextVoter(); }}
              variant="outlined"
              sx={{ mt: 2 }}
            >
              Reset Search
            </Button>
          </CardContent>
        </Card>
      ) : currentVoter ? (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
              <Box sx={{ flex: '1 1 300px' }}>
                <Box display="flex" alignItems="center" gap={1} mb={2}>
                  <Person />
                  <Typography variant="h5">
                    {currentVoter.firstName} {currentVoter.lastName}
                  </Typography>
                  {currentVoter.age > 0 && (
                    <Chip label={`Age ${currentVoter.age}`} size="small" />
                  )}
                  {currentVoter.gender && (
                    <Chip label={currentVoter.gender} size="small" />
                  )}
                </Box>

                <Box display="flex" alignItems="center" gap={1} mb={2}>
                  <Phone />
                  <Typography variant="h6" color="primary">
                    {formatPhoneNumber(currentVoter.cellPhone)}
                  </Typography>
                </Box>

                <Box display="flex" alignItems="start" gap={1} mb={2}>
                  <LocationOn fontSize="small" />
                  <Box>
                    <Typography variant="body2">
                      {currentVoter.addressLine}
                    </Typography>
                    <Typography variant="body2">
                      {currentVoter.city}, {currentVoter.state} {currentVoter.zip}
                    </Typography>
                  </Box>
                </Box>

                {currentVoter.partyAffiliation && (
                  <Box mb={2}>
                    <Chip 
                      label={`Party: ${currentVoter.partyAffiliation}`} 
                      size="small" 
                      variant="outlined" 
                    />
                  </Box>
                )}
              </Box>

              <Box sx={{ flex: '1 1 300px' }}>
                <Box display="flex" flexDirection="column" gap={2}>
                  <Button
                    variant="contained"
                    size="large"
                    startIcon={<Phone />}
                    onClick={() => {
                      // Initiate phone call
                      if (currentVoter.cellPhone) {
                        window.location.href = `tel:${currentVoter.cellPhone}`;
                      }
                    }}
                    fullWidth
                    sx={{ py: 2 }}
                    disabled={!currentVoter.cellPhone}
                  >
                    Call {formatPhoneNumber(currentVoter.cellPhone)}
                  </Button>
                  
                  <Button
                    variant="outlined"
                    size="large"
                    onClick={() => setContactModalOpen(true)}
                    fullWidth
                    sx={{ py: 1 }}
                  >
                    Record Contact
                  </Button>
                  
                  <Button
                    variant="text"
                    startIcon={<NavigateNext />}
                    onClick={skipVoter}
                    fullWidth
                  >
                    Skip to Next
                  </Button>
                </Box>
              </Box>
            </Box>

            {/* Previous Contact History */}
            {currentVoter.isContacted && (
              <Box mt={3} p={2} bgcolor="grey.100" borderRadius={1}>
                <Typography variant="subtitle2" gutterBottom>
                  Previous Contact
                </Typography>
                <Typography variant="body2">
                  Status: {currentVoter.lastContactStatus}
                  {currentVoter.voterSupport && ` • Support: ${currentVoter.voterSupport}`}
                </Typography>
              </Box>
            )}
          </CardContent>
        </Card>
      ) : null}

      {/* Recent Contacts */}
      {recentContacts.length > 0 && (
        <Paper sx={{ p: 2 }}>
          <Box display="flex" alignItems="center" gap={1} mb={2}>
            <History />
            <Typography variant="h6">Recent Calls</Typography>
          </Box>
          <Divider sx={{ mb: 2 }} />
          {recentContacts.map((contact) => (
            <Box key={contact.id} sx={{ mb: 2 }}>
              <Box display="flex" justifyContent="space-between" alignItems="start">
                <Box>
                  <Typography variant="body2" fontWeight="bold">
                    {contact.voterName}
                  </Typography>
                  <Typography variant="caption" color="textSecondary">
                    {dayjs(contact.timestamp).format('h:mm A')} • {contact.status}
                    {contact.voterSupport && ` • ${contact.voterSupport}`}
                  </Typography>
                </Box>
                {contact.callDurationSeconds && (
                  <Chip 
                    label={`${Math.floor(contact.callDurationSeconds / 60)}:${(contact.callDurationSeconds % 60).toString().padStart(2, '0')}`}
                    size="small"
                    icon={<Timer />}
                  />
                )}
              </Box>
              {contact.notes && (
                <Typography variant="caption" sx={{ mt: 0.5, display: 'block' }}>
                  {contact.notes}
                </Typography>
              )}
            </Box>
          ))}
        </Paper>
      )}

        {/* Phone Contact Modal */}
        <PhoneContactModal
          open={contactModalOpen}
          voter={currentVoter}
          onClose={() => setContactModalOpen(false)}
          onSubmit={handleContactSubmit}
          user={user}
        />
      </Container>
    </Box>
  );
};

export default PhoneBanking;