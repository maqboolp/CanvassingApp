import React, { useState, useEffect, useCallback } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  TablePagination,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  Box,
  Typography,
  CircularProgress,
  Alert,
  Chip
} from '@mui/material';
import {
  Search,
  Clear,
  FilterList,
  Person,
  CalendarToday,
  VolumeUp
} from '@mui/icons-material';
import { AuthUser, ContactStatus, VoterSupport } from '../types';
import { API_BASE_URL } from '../config';

interface VoterContactHistoryProps {
  user: AuthUser;
}

interface ContactHistoryItem {
  id: string;
  voterId: string;
  voterName: string;
  voterAddress: string;
  volunteerId: string;
  volunteerName: string;
  contactDate: string;
  status: ContactStatus;
  voterSupport?: VoterSupport;
  notes?: string;
  audioFileUrl?: string;
  audioDurationSeconds?: number;
}

interface ContactHistoryResponse {
  contacts: ContactHistoryItem[];
  total: number;
  page: number;
  totalPages: number;
}

interface VolunteerOption {
  id: string;
  name: string;
}

const VoterContactHistory: React.FC<VoterContactHistoryProps> = ({ user }) => {
  const [contacts, setContacts] = useState<ContactHistoryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [volunteers, setVolunteers] = useState<VolunteerOption[]>([]);
  
  // Filter states
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedVolunteer, setSelectedVolunteer] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [showFilters, setShowFilters] = useState(false);

  const fetchVolunteers = useCallback(async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/volunteers`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        const volunteerOptions: VolunteerOption[] = data.map((vol: any) => ({
          id: vol.id,
          name: `${vol.firstName} ${vol.lastName}`
        }));
        setVolunteers(volunteerOptions);
      }
    } catch (error) {
      console.error('Failed to fetch volunteers:', error);
    }
  }, [user.token]);

  const fetchContactHistory = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const queryParams = new URLSearchParams({
        page: (page + 1).toString(),
        limit: rowsPerPage.toString(),
        ...(searchQuery && { search: searchQuery }),
        ...(selectedVolunteer && { volunteerId: selectedVolunteer }),
        ...(startDate && { startDate }),
        ...(endDate && { endDate })
      });

      const response = await fetch(`${API_BASE_URL}/api/admin/voter-contact-history?${queryParams}`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (!response.ok) {
        throw new Error('Failed to fetch contact history');
      }

      const data: ContactHistoryResponse = await response.json();
      setContacts(data.contacts);
      setTotal(data.total);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch contact history');
    } finally {
      setLoading(false);
    }
  }, [user.token, page, rowsPerPage, searchQuery, selectedVolunteer, startDate, endDate]);

  // Fetch volunteers for filter dropdown
  useEffect(() => {
    fetchVolunteers();
  }, [fetchVolunteers]);

  // Fetch contact history when filters change
  useEffect(() => {
    fetchContactHistory();
  }, [fetchContactHistory]);

  const handlePageChange = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleRowsPerPageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const clearFilters = () => {
    setSearchQuery('');
    setSelectedVolunteer('');
    setStartDate('');
    setEndDate('');
    setPage(0);
  };

  const getStatusColor = (status: ContactStatus) => {
    switch (status) {
      case 'reached': return 'success';
      case 'not-home': return 'warning';
      case 'refused': return 'error';
      case 'needs-follow-up': return 'info';
      default: return 'default';
    }
  };

  const getSupportColor = (support: VoterSupport) => {
    switch (support) {
      case 'StrongYes': return '#2e7d32';
      case 'LeaningYes': return '#66bb6a';
      case 'Undecided': return '#ffa726';
      case 'LeaningNo': return '#ef5350';
      case 'StrongNo': return '#c62828';
      default: return '#757575';
    }
  };

  const formatSupportText = (support: VoterSupport) => {
    switch (support) {
      case 'StrongYes': return 'Strong Yes';
      case 'LeaningYes': return 'Leaning Yes';
      case 'Undecided': return 'Undecided';
      case 'LeaningNo': return 'Leaning No';
      case 'StrongNo': return 'Strong No';
      default: return support;
    }
  };

  const formatStatusText = (status: ContactStatus) => {
    switch (status) {
      case 'reached': return 'Reached';
      case 'not-home': return 'Not Home';
      case 'refused': return 'Refused';
      case 'needs-follow-up': return 'Needs Follow-up';
      default: return status;
    }
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5">Voter Contact History</Typography>
        <Button
          variant="outlined"
          startIcon={<FilterList />}
          onClick={() => setShowFilters(!showFilters)}
        >
          {showFilters ? 'Hide Filters' : 'Show Filters'}
        </Button>
      </Box>

      {/* Filters */}
      {showFilters && (
        <Paper sx={{ p: 3, mb: 3 }}>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '2fr 2fr 1fr 1fr 1fr' }, gap: 2, alignItems: 'center' }}>
            <TextField
              fullWidth
              label="Search by voter name or ID"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              InputProps={{
                startAdornment: <Search sx={{ mr: 1, color: 'text.secondary' }} />
              }}
            />
            <FormControl fullWidth>
              <InputLabel>Volunteer</InputLabel>
              <Select
                value={selectedVolunteer}
                onChange={(e) => setSelectedVolunteer(e.target.value)}
                label="Volunteer"
              >
                <MenuItem value="">All Volunteers</MenuItem>
                {volunteers.map((volunteer) => (
                  <MenuItem key={volunteer.id} value={volunteer.id}>
                    {volunteer.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              fullWidth
              label="Start Date"
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            <TextField
              fullWidth
              label="End Date"
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            <Button
              fullWidth
              variant="outlined"
              startIcon={<Clear />}
              onClick={clearFilters}
            >
              Clear Filters
            </Button>
          </Box>
        </Paper>
      )}

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Table */}
      <TableContainer component={Paper}>
        {loading ? (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        ) : (
          <>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Voter Name</TableCell>
                  <TableCell>Address</TableCell>
                  <TableCell>Volunteer</TableCell>
                  <TableCell>Contact Date</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Voter Support</TableCell>
                  <TableCell>Notes</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {contacts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center">
                      <Typography variant="body2" color="text.secondary" py={3}>
                        No contact history found
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  contacts.map((contact) => (
                    <TableRow key={contact.id}>
                      <TableCell>
                        <Box display="flex" alignItems="center">
                          <Person sx={{ mr: 1, color: 'text.secondary', fontSize: 18 }} />
                          {contact.voterName}
                        </Box>
                      </TableCell>
                      <TableCell>{contact.voterAddress}</TableCell>
                      <TableCell>{contact.volunteerName}</TableCell>
                      <TableCell>
                        <Box display="flex" alignItems="center">
                          <CalendarToday sx={{ mr: 1, color: 'text.secondary', fontSize: 16 }} />
                          {new Date(contact.contactDate).toLocaleString()}
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={formatStatusText(contact.status)}
                          color={getStatusColor(contact.status)}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        {contact.voterSupport && (
                          <Chip
                            label={formatSupportText(contact.voterSupport)}
                            size="small"
                            sx={{
                              backgroundColor: getSupportColor(contact.voterSupport),
                              color: 'white'
                            }}
                          />
                        )}
                      </TableCell>
                      <TableCell>
                        <Box sx={{ maxWidth: 300 }}>
                          {contact.audioFileUrl && (
                            <Box sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Chip
                                icon={<VolumeUp />}
                                label={`Voice Memo (${contact.audioDurationSeconds ? Math.floor(contact.audioDurationSeconds / 60) + ':' + (contact.audioDurationSeconds % 60).toString().padStart(2, '0') : '0:00'})`}
                                size="small"
                                color="primary"
                                variant="outlined"
                              />
                              <audio 
                                controls 
                                src={`${API_BASE_URL}${contact.audioFileUrl}`} 
                                style={{ height: '30px', maxWidth: '200px' }}
                              />
                            </Box>
                          )}
                          <Typography variant="body2">
                            {contact.notes || '-'}
                          </Typography>
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
            <TablePagination
              rowsPerPageOptions={[10, 25, 50, 100]}
              component="div"
              count={total}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={handlePageChange}
              onRowsPerPageChange={handleRowsPerPageChange}
            />
          </>
        )}
      </TableContainer>
    </Box>
  );
};

export default VoterContactHistory;