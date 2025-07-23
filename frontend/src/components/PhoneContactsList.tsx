import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Chip,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Tooltip,
  TextField,
  InputAdornment,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  CircularProgress,
  Alert
} from '@mui/material';
import {
  Phone,
  Search,
  Timer,
  Person,
  CalendarToday,
  AudioFile,
  PhoneInTalk,
  PhoneMissed,
  Voicemail,
  WrongLocation,
  PhoneDisabled,
  Block,
  CallReceived,
  RemoveCircle,
  Refresh
} from '@mui/icons-material';
import { PhoneContactStatus } from './PhoneContactModal';
import { VoterSupport } from '../types';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler } from '../utils/apiErrorHandler';
import dayjs from 'dayjs';

interface PhoneContactDetail {
  id: string;
  voterId: string;
  voterName: string;
  voterAddress: string;
  voterPhone?: string;
  status: PhoneContactStatus;
  voterSupport?: VoterSupport;
  notes?: string;
  timestamp: string;
  callDurationSeconds?: number;
  phoneNumberUsed?: string;
  audioFileUrl?: string;
  audioDurationSeconds?: number;
}

interface PhoneContactsSummary {
  totalContacts: number;
  contactsByStatus: { [key: string]: number };
  contactsBySupport: { [key: string]: number };
  totalCallDuration: number;
  contacts: PhoneContactDetail[];
}

const PhoneContactsList: React.FC = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<PhoneContactsSummary | null>(null);
  const [selectedDate, setSelectedDate] = useState<string>(
    new Date().toISOString().split('T')[0]
  );
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const fetchPhoneContacts = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/phonecontacts/my-contacts?date=${selectedDate}`,
        { method: 'GET' }
      );
      setSummary(response);
    } catch (err) {
      setError('Failed to load phone contacts');
      console.error('Error fetching phone contacts:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPhoneContacts();
  }, [selectedDate]);

  const getStatusIcon = (status: PhoneContactStatus) => {
    switch (status) {
      case PhoneContactStatus.Reached:
        return <PhoneInTalk fontSize="small" />;
      case PhoneContactStatus.NoAnswer:
        return <PhoneMissed fontSize="small" />;
      case PhoneContactStatus.VoiceMail:
        return <Voicemail fontSize="small" />;
      case PhoneContactStatus.WrongNumber:
        return <WrongLocation fontSize="small" />;
      case PhoneContactStatus.Disconnected:
        return <PhoneDisabled fontSize="small" />;
      case PhoneContactStatus.Refused:
        return <Block fontSize="small" />;
      case PhoneContactStatus.Callback:
        return <CallReceived fontSize="small" />;
      case PhoneContactStatus.DoNotCall:
        return <RemoveCircle fontSize="small" />;
      default:
        return <Phone fontSize="small" />;
    }
  };

  const getStatusColor = (status: PhoneContactStatus): "success" | "error" | "warning" | "default" => {
    switch (status) {
      case PhoneContactStatus.Reached:
        return "success";
      case PhoneContactStatus.NoAnswer:
      case PhoneContactStatus.VoiceMail:
        return "warning";
      case PhoneContactStatus.WrongNumber:
      case PhoneContactStatus.Disconnected:
      case PhoneContactStatus.Refused:
      case PhoneContactStatus.DoNotCall:
        return "error";
      case PhoneContactStatus.Callback:
        return "default";
      default:
        return "default";
    }
  };

  const getSupportColor = (support: VoterSupport): "success" | "error" | "warning" | "default" => {
    switch (support) {
      case 'StrongYes':
      case 'LeaningYes':
        return "success";
      case 'Undecided':
        return "warning";
      case 'LeaningNo':
      case 'StrongNo':
        return "error";
      default:
        return "default";
    }
  };

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const filteredContacts = summary?.contacts.filter(contact => {
    const matchesSearch = searchTerm === '' || 
      contact.voterName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      contact.voterAddress.toLowerCase().includes(searchTerm.toLowerCase()) ||
      contact.notes?.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesStatus = statusFilter === 'all' || contact.status === statusFilter;
    
    return matchesSearch && matchesStatus;
  }) || [];

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="400px">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        {error}
      </Alert>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Phone Contacts
      </Typography>

      {/* Summary Cards */}
      <Box sx={{ display: 'flex', gap: 3, mb: 3, flexWrap: 'wrap' }}>
        <Box sx={{ flex: '1 1 250px' }}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    Total Calls
                  </Typography>
                  <Typography variant="h4">
                    {summary?.totalContacts || 0}
                  </Typography>
                </Box>
                <Phone color="primary" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 250px' }}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    Reached
                  </Typography>
                  <Typography variant="h4">
                    {summary?.contactsByStatus['Reached'] || 0}
                  </Typography>
                </Box>
                <PhoneInTalk color="success" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 250px' }}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    Total Duration
                  </Typography>
                  <Typography variant="h4">
                    {formatDuration(summary?.totalCallDuration || 0)}
                  </Typography>
                </Box>
                <Timer color="info" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 250px' }}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom>
                    Support Rate
                  </Typography>
                  <Typography variant="h4">
                    {summary?.contactsBySupport['StrongYes'] || 0} / {summary?.contactsBySupport['LeanYes'] || 0}
                  </Typography>
                  <Typography variant="caption" color="textSecondary">
                    Strong / Lean Yes
                  </Typography>
                </Box>
                <Person color="secondary" sx={{ fontSize: 40 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* Filters */}
      <Box sx={{ mb: 3, display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        <TextField
          type="date"
          label="Date"
          value={selectedDate}
          onChange={(e) => setSelectedDate(e.target.value)}
          InputLabelProps={{ shrink: true }}
          size="small"
        />

        <TextField
          placeholder="Search contacts..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          size="small"
          sx={{ minWidth: 250 }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search />
              </InputAdornment>
            ),
          }}
        />

        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Status</InputLabel>
          <Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            label="Status"
          >
            <MenuItem value="all">All Status</MenuItem>
            <MenuItem value="Reached">Reached</MenuItem>
            <MenuItem value="NoAnswer">No Answer</MenuItem>
            <MenuItem value="VoiceMail">Voicemail</MenuItem>
            <MenuItem value="WrongNumber">Wrong Number</MenuItem>
            <MenuItem value="Disconnected">Disconnected</MenuItem>
            <MenuItem value="Refused">Refused</MenuItem>
            <MenuItem value="Callback">Callback</MenuItem>
            <MenuItem value="DoNotCall">Do Not Call</MenuItem>
          </Select>
        </FormControl>

        <Button
          startIcon={<Refresh />}
          onClick={fetchPhoneContacts}
          variant="outlined"
          size="small"
        >
          Refresh
        </Button>
      </Box>

      {/* Contacts Table */}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Time</TableCell>
              <TableCell>Voter</TableCell>
              <TableCell>Phone</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Support</TableCell>
              <TableCell>Duration</TableCell>
              <TableCell>Notes</TableCell>
              <TableCell>Audio</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredContacts.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  <Typography color="textSecondary" sx={{ py: 3 }}>
                    No phone contacts found for the selected criteria
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              filteredContacts.map((contact) => (
                <TableRow key={contact.id}>
                  <TableCell>
                    {dayjs(contact.timestamp).format('h:mm A')}
                  </TableCell>
                  <TableCell>
                    <Box>
                      <Typography variant="body2" fontWeight="bold">
                        {contact.voterName}
                      </Typography>
                      <Typography variant="caption" color="textSecondary">
                        {contact.voterAddress}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>{contact.voterPhone || '-'}</TableCell>
                  <TableCell>
                    <Chip
                      icon={getStatusIcon(contact.status)}
                      label={contact.status}
                      size="small"
                      color={getStatusColor(contact.status)}
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>
                    {contact.voterSupport ? (
                      <Chip
                        label={contact.voterSupport}
                        size="small"
                        color={getSupportColor(contact.voterSupport)}
                      />
                    ) : (
                      '-'
                    )}
                  </TableCell>
                  <TableCell>
                    {contact.callDurationSeconds ? formatDuration(contact.callDurationSeconds) : '-'}
                  </TableCell>
                  <TableCell>
                    <Tooltip title={contact.notes || 'No notes'}>
                      <Typography
                        variant="body2"
                        sx={{
                          maxWidth: 200,
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap'
                        }}
                      >
                        {contact.notes || '-'}
                      </Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell>
                    {contact.audioFileUrl ? (
                      <Tooltip title={`Audio recording (${formatDuration(contact.audioDurationSeconds || 0)})`}>
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => window.open(contact.audioFileUrl, '_blank')}
                        >
                          <AudioFile />
                        </IconButton>
                      </Tooltip>
                    ) : (
                      '-'
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};

export default PhoneContactsList;