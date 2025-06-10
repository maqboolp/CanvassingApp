import React, { useState, useEffect } from 'react';
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
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  CircularProgress,
  Alert
} from '@mui/material';
import {
  ContactPhone,
  LocationOn,
  FilterList,
  Clear,
  Phone,
  Email
} from '@mui/icons-material';
import { Voter, VoterFilter, PaginationParams, VoterListResponse, ContactStatus, VoterSupport, AuthUser } from '../types';
import ContactModal from './ContactModal';
import { API_BASE_URL } from '../config';

interface VoterListProps {
  onContactVoter: (voter: Voter) => void;
  user?: AuthUser;
}

const VoterList: React.FC<VoterListProps> = ({ onContactVoter, user }) => {
  const [voters, setVoters] = useState<Voter[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [selectedVoter, setSelectedVoter] = useState<Voter | null>(null);
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [filters, setFilters] = useState<VoterFilter>({
    contactStatus: 'not-contacted'
  });
  const [location, setLocation] = useState<{ latitude: number; longitude: number } | null>(null);
  const [useLocation, setUseLocation] = useState(false);
  const [isMobile, setIsMobile] = useState(window.innerWidth < 600);

  const [filterInputs, setFilterInputs] = useState({
    zipCode: '',
    voteFrequency: '',
    ageGroup: '',
    contactStatus: 'not-contacted',
    searchName: ''
  });

  useEffect(() => {
    fetchVoters();
  }, [page, rowsPerPage, filters, useLocation, location]);

  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth < 600);
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

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

  // Set default sorting to ZIP on mount
  useEffect(() => {
    setFilters(prev => ({ ...prev, sortBy: 'zip' }));
  }, []); // Only run once on mount

  const getCurrentLocation = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setLocation({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude
          });
          setUseLocation(true);
          setError(null); // Clear any previous errors
        },
        (error) => {
          console.warn('Geolocation error:', error);
          // Fallback to ZIP code sorting instead of showing error
          setUseLocation(false);
          setLocation(null);
          // Set sorting to ZIP code as fallback
          setFilters(prev => ({ ...prev, sortBy: 'zip' }));
        }
      );
    } else {
      console.warn('Geolocation is not supported by this browser.');
      // Fallback to ZIP code sorting
      setUseLocation(false);
      setLocation(null);
      setFilters(prev => ({ ...prev, sortBy: 'zip' }));
    }
  };

  const fetchVoters = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const queryParams = new URLSearchParams({
        page: (page + 1).toString(),
        limit: rowsPerPage.toString(),
        ...(filters.zipCode && { zipCode: filters.zipCode }),
        ...(filters.voteFrequency && { voteFrequency: filters.voteFrequency }),
        ...(filters.ageGroup && { ageGroup: filters.ageGroup }),
        ...(filters.contactStatus && { contactStatus: filters.contactStatus }),
        ...(filters.searchName && { searchName: filters.searchName }),
        ...(filters.sortBy && { sortBy: filters.sortBy }),
        sortOrder: 'asc',
        ...(useLocation && location && !filters.zipCode && { 
          latitude: location.latitude.toString(),
          longitude: location.longitude.toString(),
          radiusKm: '5' // 5km radius
        })
      });

      const token = user?.token || localStorage.getItem('auth_token');
      const response = await fetch(`${API_BASE_URL}/api/voters?${queryParams}`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        throw new Error('Failed to fetch voters');
      }

      const data: VoterListResponse = await response.json();
      setVoters(data.voters);
      setTotal(data.total);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch voters');
    } finally {
      setLoading(false);
    }
  };

  const handlePageChange = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleRowsPerPageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleFilterChange = (field: keyof typeof filterInputs, value: string) => {
    setFilterInputs(prev => ({ ...prev, [field]: value }));
  };

  const applyFilters = () => {
    setFilters({
      ...filters,
      zipCode: filterInputs.zipCode || undefined,
      voteFrequency: filterInputs.voteFrequency as any || undefined,
      ageGroup: filterInputs.ageGroup as any || undefined,
      contactStatus: filterInputs.contactStatus as any || undefined,
      searchName: filterInputs.searchName || undefined
    });
    setPage(0);
  };

  const clearFilters = () => {
    setFilterInputs({
      zipCode: '',
      voteFrequency: '',
      ageGroup: '',
      contactStatus: 'not-contacted',
      searchName: ''
    });
    setFilters({
      contactStatus: 'not-contacted'
    });
    setPage(0);
  };

  const handleContactClick = (voter: Voter) => {
    setSelectedVoter(voter);
    setContactModalOpen(true);
  };

  const handleContactSubmit = async (status: ContactStatus, notes: string, voterSupport?: VoterSupport) => {
    if (!selectedVoter) return;

    try {
      const token = user?.token || localStorage.getItem('auth_token');
      const response = await fetch(`${API_BASE_URL}/api/contacts`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          voterId: selectedVoter.lalVoterId,
          status,
          voterSupport,
          notes,
          location: await getCurrentLocation()
        })
      });

      if (!response.ok) {
        throw new Error('Failed to log contact');
      }

      setContactModalOpen(false);
      setSelectedVoter(null);
      onContactVoter(selectedVoter);
      fetchVoters(); // Refresh the list
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to log contact');
    }
  };

  const getStatusChipColor = (status?: ContactStatus) => {
    switch (status) {
      case 'reached': return 'success';
      case 'not-home': return 'warning';
      case 'refused': return 'error';
      case 'needs-follow-up': return 'info';
      default: return 'default';
    }
  };

  const formatVoteFrequency = (frequency: string) => {
    switch (frequency) {
      case 'frequent': return 'Frequent (3+)';
      case 'infrequent': return 'Infrequent (1-2)';
      case 'non-voter': return 'Non-voter';
      default: return frequency;
    }
  };

  return (
    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
      {/* Filter Controls */}
      <Box sx={{ p: { xs: 1, sm: 2 }, borderBottom: 1, borderColor: 'divider' }}>
        <Typography variant="h6" gutterBottom sx={{ fontSize: { xs: '1.1rem', sm: '1.25rem' } }}>
          Voter List ({total} voters)
          {useLocation && (
            <Chip 
              icon={<LocationOn />} 
              label="Within 5km" 
              color="success" 
              size="small" 
              sx={{ ml: { xs: 1, sm: 2 } }} 
            />
          )}
        </Typography>
        
        <Box sx={{ display: 'flex', gap: { xs: 1, sm: 2 }, flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField
            size="small"
            label="Search Name"
            value={filterInputs.searchName}
            onChange={(e) => handleFilterChange('searchName', e.target.value)}
            sx={{ minWidth: { xs: 120, sm: 160 }, flex: { xs: '1 1 auto', sm: 'none' } }}
            placeholder="First or Last Name"
          />
          
          <TextField
            size="small"
            label="ZIP Code"
            value={filterInputs.zipCode}
            onChange={(e) => handleFilterChange('zipCode', e.target.value)}
            sx={{ minWidth: { xs: 80, sm: 120 }, flex: { xs: '0 1 auto', sm: 'none' } }}
          />
          
          <FormControl size="small" sx={{ minWidth: { xs: 100, sm: 140 }, flex: { xs: '1 1 auto', sm: 'none' } }}>
            <InputLabel>Contact Status</InputLabel>
            <Select
              value={filterInputs.contactStatus}
              label="Contact Status"
              onChange={(e) => handleFilterChange('contactStatus', e.target.value)}
            >
              <MenuItem value="">All</MenuItem>
              <MenuItem value="contacted">Contacted</MenuItem>
              <MenuItem value="not-contacted">Not Contacted</MenuItem>
            </Select>
          </FormControl>
          
          <Button
            variant="contained"
            startIcon={<FilterList />}
            onClick={applyFilters}
            size={isMobile ? "small" : "medium"}
            sx={{ minWidth: { xs: 'auto', sm: 'auto' } }}
          >
            Apply
          </Button>
          
          <Button
            variant="outlined"
            startIcon={<Clear />}
            onClick={clearFilters}
            size={isMobile ? "small" : "medium"}
            sx={{ minWidth: { xs: 'auto', sm: 'auto' } }}
          >
            Clear
          </Button>
          
          <Button
            variant={useLocation ? "contained" : "outlined"}
            startIcon={<LocationOn />}
            onClick={useLocation ? () => { setUseLocation(false); setLocation(null); } : getCurrentLocation}
            color={useLocation ? "success" : "primary"}
            size={isMobile ? "small" : "medium"}
            sx={{ minWidth: { xs: 'auto', sm: 'auto' } }}
          >
            {isMobile 
              ? (useLocation ? "Off" : "Near") 
              : (useLocation ? "Turn Off Location" : "Find Nearby")
            }
          </Button>
          
          {filters.sortBy === 'zip' && !useLocation && (
            <Chip 
              icon={<LocationOn />} 
              label="Sorted by ZIP" 
              color="info" 
              size="small" 
            />
          )}
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ m: 2 }}>
          {error}
        </Alert>
      )}

      {/* Voter Table */}
      <TableContainer sx={{ maxHeight: 600 }}>
        <Table stickyHeader size={isMobile ? "small" : "medium"}>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Address</TableCell>
              {!isMobile && <TableCell>Distance</TableCell>}
              {!isMobile && <TableCell>Age</TableCell>}
              {!isMobile && <TableCell>Vote Frequency</TableCell>}
              <TableCell>Status</TableCell>
              {!isMobile && <TableCell>Contact Info</TableCell>}
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={isMobile ? 4 : 8} sx={{ textAlign: 'center', py: 4 }}>
                  <CircularProgress />
                </TableCell>
              </TableRow>
            ) : voters.length === 0 ? (
              <TableRow>
                <TableCell colSpan={isMobile ? 4 : 8} sx={{ textAlign: 'center', py: 4 }}>
                  No voters found
                </TableCell>
              </TableRow>
            ) : (
              voters.map((voter) => (
                <TableRow
                  key={voter.lalVoterId}
                  sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                >
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Box>
                        <Typography variant="body2" fontWeight="medium" sx={{ fontSize: isMobile ? '0.875rem' : 'inherit' }}>
                          {voter.firstName} {voter.lastName}
                        </Typography>
                        {voter.middleName && !isMobile && (
                          <Typography variant="caption" color="text.secondary">
                            {voter.middleName}
                          </Typography>
                        )}
                        {isMobile && (
                          <>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                              Age {voter.age} • {voter.gender}
                            </Typography>
                            {voter.cellPhone && (
                              <Typography variant="caption" color="primary" sx={{ display: 'block' }}>
                                📞 {voter.cellPhone}
                              </Typography>
                            )}
                          </>
                        )}
                      </Box>
                    </Box>
                  </TableCell>
                  
                  <TableCell>
                    <Box 
                      sx={{ 
                        display: 'flex', 
                        alignItems: 'flex-start', 
                        gap: 1,
                        cursor: 'pointer',
                        '&:hover': {
                          backgroundColor: 'action.hover',
                          borderRadius: 1,
                        },
                        p: 1,
                        m: -1
                      }}
                      onClick={() => openInMaps(voter)}
                      title="Click to open in maps for directions"
                    >
                      <LocationOn fontSize="small" color="primary" />
                      <Box>
                        <Typography variant="body2" color="primary" sx={{ fontWeight: 'medium', fontSize: isMobile ? '0.8rem' : 'inherit' }}>
                          {voter.addressLine}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {voter.city}, {voter.state} {voter.zip}
                        </Typography>
                        {isMobile && voter.distanceKm && (
                          <Typography variant="caption" color="primary" sx={{ display: 'block', fontWeight: 'medium' }}>
                            📍 {voter.distanceKm.toFixed(2)} km
                          </Typography>
                        )}
                      </Box>
                    </Box>
                  </TableCell>
                  
                  {!isMobile && (
                    <TableCell>
                      {voter.distanceKm ? (
                        <Typography variant="body2" color="primary" sx={{ fontWeight: 'medium' }}>
                          📍 {voter.distanceKm.toFixed(2)} km
                        </Typography>
                      ) : (
                        <Typography variant="caption" color="text.secondary">
                          -
                        </Typography>
                      )}
                    </TableCell>
                  )}
                  
                  {!isMobile && (
                    <TableCell>
                      <Typography variant="body2">
                        {voter.age}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {voter.gender}
                      </Typography>
                    </TableCell>
                  )}
                  
                  {!isMobile && (
                    <TableCell>
                      <Chip
                        label={formatVoteFrequency(voter.voteFrequency)}
                        size="small"
                        variant="outlined"
                      />
                    </TableCell>
                  )}
                  
                  <TableCell>
                    {voter.isContacted ? (
                      <Chip
                        label={voter.lastContactStatus?.replace('-', ' ') || 'Contacted'}
                        size="small"
                        color={getStatusChipColor(voter.lastContactStatus)}
                      />
                    ) : (
                      <Chip
                        label={isMobile ? "Not Called" : "Not Contacted"}
                        size="small"
                        variant="outlined"
                      />
                    )}
                  </TableCell>
                  
                  {!isMobile && (
                    <TableCell>
                      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                        {voter.cellPhone && (
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <Phone fontSize="small" color="action" />
                            <Typography variant="caption">
                              {voter.cellPhone}
                            </Typography>
                          </Box>
                        )}
                        {voter.email && (
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <Email fontSize="small" color="action" />
                            <Typography variant="caption">
                              {voter.email}
                            </Typography>
                          </Box>
                        )}
                      </Box>
                    </TableCell>
                  )}
                  
                  <TableCell>
                    <Button
                      variant="contained"
                      size="small"
                      startIcon={isMobile ? undefined : <ContactPhone />}
                      onClick={() => handleContactClick(voter)}
                      disabled={loading}
                      sx={{ minWidth: isMobile ? '60px' : 'auto' }}
                    >
                      {isMobile ? "Call" : "Contact"}
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Pagination */}
      <TablePagination
        rowsPerPageOptions={[10, 25, 50, 100]}
        component="div"
        count={total}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handlePageChange}
        onRowsPerPageChange={handleRowsPerPageChange}
      />

      {/* Contact Modal */}
      <ContactModal
        open={contactModalOpen}
        voter={selectedVoter}
        onClose={() => {
          setContactModalOpen(false);
          setSelectedVoter(null);
        }}
        onSubmit={handleContactSubmit}
      />
    </Paper>
  );
};

export default VoterList;