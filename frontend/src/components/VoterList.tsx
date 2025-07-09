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
  Autocomplete,
  Collapse,
  Checkbox,
  Toolbar,
  Fade,
  Button,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  CircularProgress,
  Alert,
  useMediaQuery,
  Tabs,
  Tab,
  Switch,
  FormControlLabel
} from '@mui/material';
import {
  ContactPhone,
  LocationOn,
  FilterList,
  Clear,
  Phone,
  Email,
  LocalOffer,
  ExpandMore,
  Label,
  LabelOff,
  DeleteForever,
  PersonAdd,
  Add,
  List as ListIcon,
  Map as MapIcon
} from '@mui/icons-material';
import { Voter, VoterFilter, VoterListResponse, ContactStatus, VoterSupport, AuthUser, VoterTag } from '../types';
import ContactModal from './ContactModal';
import VoterMapViewWrapper from './VoterMapViewWrapper';
import { API_BASE_URL } from '../config';

interface VoterListProps {
  onContactVoter: (voter: Voter) => void;
  user?: AuthUser;
}

const VoterList: React.FC<VoterListProps> = ({ onContactVoter, user }) => {
  const isMobile = useMediaQuery('(max-width:480px)');
  
  const [voters, setVoters] = useState<Voter[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [selectedVoter, setSelectedVoter] = useState<Voter | null>(null);
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [filters, setFilters] = useState<VoterFilter>({
    contactStatus: 'not-contacted',
    travelMode: 'walking',
    radiusKm: 3.2, // Default to 2 miles (3.2 km)
    useTravelDistance: false // Default to straight-line distance
  });
  const [location, setLocation] = useState<{ latitude: number; longitude: number } | null>(null);
  const [useLocation, setUseLocation] = useState(false);
  const [availableTags, setAvailableTags] = useState<VoterTag[]>([]);
  const [selectedTags, setSelectedTags] = useState<VoterTag[]>([]);
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  const [selectedVoters, setSelectedVoters] = useState<string[]>([]);
  const [bulkTagDialog, setBulkTagDialog] = useState(false);
  const [bulkOperation, setBulkOperation] = useState<'add' | 'remove'>('add');
  const [bulkSelectedTags, setBulkSelectedTags] = useState<VoterTag[]>([]);
  const [bulkLoading, setBulkLoading] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [uncontactDialogOpen, setUncontactDialogOpen] = useState(false);
  const [voterToUncontact, setVoterToUncontact] = useState<Voter | null>(null);
  const [uncontactLoading, setUncontactLoading] = useState(false);
  const [addVoterDialogOpen, setAddVoterDialogOpen] = useState(false);
  const [addVoterLoading, setAddVoterLoading] = useState(false);
  const [currentView, setCurrentView] = useState<'list' | 'map'>('list');
  const [newVoter, setNewVoter] = useState({
    firstName: '',
    lastName: '',
    addressLine: '',
    city: '',
    state: 'AL',
    zip: '',
    age: '',
    gender: 'Unknown',
    cellPhone: '',
    email: '',
    voteFrequency: 'NonVoter',
    partyAffiliation: ''
  });

  const [filterInputs, setFilterInputs] = useState({
    voteFrequency: '',
    ageGroup: '',
    contactStatus: 'not-contacted',
    searchName: '',
    partyAffiliation: '',
    tagIds: [] as number[]
  });

  useEffect(() => {
    fetchVoters();
    fetchAvailableTags();
  }, [page, rowsPerPage, filters, useLocation, location]);

  useEffect(() => {
    fetchAvailableTags();
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

  // Set default sorting to ZIP on mount and try to get location
  useEffect(() => {
    setFilters(prev => ({ ...prev, sortBy: 'zip' }));
    // Automatically try to get user location on mount
    getCurrentLocation();
  }, []); // Only run once on mount

  // When switching to map view, ensure we have loaded voters
  useEffect(() => {
    if (currentView === 'map' && voters.length === 0 && location && !loading) {
      // If switching to map view with no voters but we have location, fetch voters
      fetchVoters();
    }
  }, [currentView, location]);

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
          // When using location, sort by distance
          setFilters(prev => ({ ...prev, sortBy: 'distance' }))
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

  const fetchAvailableTags = async () => {
    try {
      const token = user?.token || localStorage.getItem('auth_token');
      const response = await fetch(`${API_BASE_URL}/api/votertags`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      
      if (response.ok) {
        const tags = await response.json();
        setAvailableTags(tags);
      }
    } catch (err) {
      console.error('Failed to fetch tags:', err);
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
        ...(filters.partyAffiliation && { partyAffiliation: filters.partyAffiliation }),
        ...(filters.sortBy && { sortBy: filters.sortBy }),
        sortOrder: 'asc',
        ...(useLocation && location && !filters.zipCode && { 
          latitude: location.latitude.toString(),
          longitude: location.longitude.toString(),
          radiusKm: (filters.radiusKm || 3.2).toString(), // Default 2 miles = 3.2 km
          ...(filters.useTravelDistance && { 
            useTravelDistance: 'true',
            travelMode: filters.travelMode || 'walking'
          })
        })
      });
      
      // Add tag filters
      if (filters.tagIds && filters.tagIds.length > 0) {
        filters.tagIds.forEach(tagId => {
          queryParams.append('tagIds', tagId.toString());
        });
      }

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
      voteFrequency: filterInputs.voteFrequency as any || undefined,
      ageGroup: filterInputs.ageGroup as any || undefined,
      contactStatus: filterInputs.contactStatus as any || undefined,
      searchName: filterInputs.searchName || undefined,
      partyAffiliation: filterInputs.partyAffiliation || undefined,
      tagIds: filterInputs.tagIds.length > 0 ? filterInputs.tagIds : undefined
    });
    setPage(0);
  };

  const clearFilters = () => {
    setFilterInputs({
      voteFrequency: '',
      ageGroup: '',
      contactStatus: 'not-contacted',
      searchName: '',
      partyAffiliation: '',
      tagIds: []
    });
    setSelectedTags([]);
    setFilters({
      contactStatus: 'not-contacted'
    });
    setPage(0);
  };

  const handleTagFilterChange = (event: any, newValue: VoterTag[]) => {
    setSelectedTags(newValue);
    setFilterInputs(prev => ({
      ...prev,
      tagIds: newValue.map(tag => tag.id)
    }));
  };

  const handleSelectAllVoters = () => {
    if (selectedVoters.length === voters.length) {
      setSelectedVoters([]);
    } else {
      setSelectedVoters(voters.map(voter => voter.lalVoterId));
    }
  };

  const handleSelectVoter = (voterId: string) => {
    setSelectedVoters(prev => {
      if (prev.includes(voterId)) {
        return prev.filter(id => id !== voterId);
      } else {
        return [...prev, voterId];
      }
    });
  };

  const handleBulkTagOperation = async () => {
    if (selectedVoters.length === 0 || bulkSelectedTags.length === 0) {
      setError('Please select voters and tags');
      return;
    }

    setBulkLoading(true);
    try {
      const token = user?.token || localStorage.getItem('auth_token');
      const response = await fetch(`${API_BASE_URL}/api/votertags/bulk-${bulkOperation}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          voterIds: selectedVoters,
          tagIds: bulkSelectedTags.map(tag => tag.id)
        })
      });

      if (!response.ok) {
        throw new Error(`Failed to ${bulkOperation} tags`);
      }

      const result = await response.json();
      setError(null);
      
      // Show success message
      const successMsg = `Successfully ${bulkOperation === 'add' ? 'added' : 'removed'} ${bulkSelectedTags.length} tag(s) ${bulkOperation === 'add' ? 'to' : 'from'} ${selectedVoters.length} voter(s)`;
      setSuccessMessage(successMsg);
      
      // Close dialog and reset state
      setBulkTagDialog(false);
      setBulkSelectedTags([]);
      setSelectedVoters([]);
      
      // Refresh voter list
      fetchVoters();
      
      // Clear success message after 5 seconds
      setTimeout(() => setSuccessMessage(null), 5000);
      
    } catch (err) {
      setError(err instanceof Error ? err.message : `Failed to ${bulkOperation} tags`);
    } finally {
      setBulkLoading(false);
    }
  };

  const openBulkTagDialog = (operation: 'add' | 'remove') => {
    setBulkOperation(operation);
    setBulkSelectedTags([]);
    setBulkTagDialog(true);
  };

  const handleContactClick = (voter: Voter) => {
    setSelectedVoter(voter);
    setContactModalOpen(true);
  };

  const handleUncontactClick = (voter: Voter) => {
    setVoterToUncontact(voter);
    setUncontactDialogOpen(true);
  };

  const handleContactSubmit = async (status: ContactStatus, notes: string, voterSupport?: VoterSupport, audioUrl?: string, audioDuration?: number, photoUrl?: string) => {
    if (!selectedVoter) return;

    try {
      // Get current location for proximity check
      const currentLocation = await new Promise<{ latitude: number; longitude: number } | null>((resolve) => {
        if (navigator.geolocation) {
          navigator.geolocation.getCurrentPosition(
            (position) => {
              resolve({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
              });
            },
            (error) => {
              console.warn('Geolocation error:', error);
              resolve(null);
            },
            { 
              enableHighAccuracy: true,
              timeout: 10000,
              maximumAge: 0 
            }
          );
        } else {
          resolve(null);
        }
      });

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
          audioFileUrl: audioUrl,
          audioDurationSeconds: audioDuration,
          photoUrl: photoUrl,
          location: currentLocation
        })
      });

      if (!response.ok) {
        const errorData = await response.json();
        
        // Check for proximity error
        if (errorData.proximityRequired) {
          throw new Error(`Proximity check failed: ${errorData.error}\n\nYou are ${errorData.currentDistance}m away (max allowed: ${errorData.maxDistance}m)`);
        } else if (errorData.requiresLocation) {
          throw new Error('Location services must be enabled to create contacts. Please enable location access and try again.');
        }
        
        throw new Error(errorData.error || 'Failed to log contact');
      }

      setContactModalOpen(false);
      setSelectedVoter(null);
      onContactVoter(selectedVoter);
      fetchVoters(); // Refresh the list
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to log contact');
    }
  };

  const handleUncontactConfirm = async () => {
    if (!voterToUncontact) return;

    setUncontactLoading(true);
    setError(null);

    try {
      const token = user?.token || localStorage.getItem('auth_token');
      
      // Get contacts for this specific voter
      const contactsResponse = await fetch(`${API_BASE_URL}/api/contacts?voterId=${voterToUncontact.lalVoterId}&page=1&limit=100`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!contactsResponse.ok) {
        throw new Error(`Failed to fetch contacts: ${contactsResponse.status}`);
      }

      const contactsData = await contactsResponse.json();
      
      // The contacts are already filtered by voter ID on the backend
      const voterContacts = contactsData.contacts;
      
      if (!voterContacts || voterContacts.length === 0) {
        throw new Error(`No contact records found for ${voterToUncontact.firstName} ${voterToUncontact.lastName}. This voter shows as contacted but has no contact history in the database. Please contact an administrator to resolve this data inconsistency.`);
      }

      // Sort by timestamp descending and get the most recent
      voterContacts.sort((a: any, b: any) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
      const latestContact = voterContacts[0];

      // Delete the contact
      const deleteResponse = await fetch(`${API_BASE_URL}/api/contacts/${latestContact.id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!deleteResponse.ok) {
        const errorData = await deleteResponse.json();
        throw new Error(errorData.error || 'Failed to uncontact voter');
      }

      // Success
      setSuccessMessage(`Successfully uncontacted ${voterToUncontact.firstName} ${voterToUncontact.lastName}`);
      setUncontactDialogOpen(false);
      setVoterToUncontact(null);
      
      // Refresh the voter list
      fetchVoters();
      
      // Clear success message after 5 seconds
      setTimeout(() => setSuccessMessage(null), 5000);

    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to uncontact voter');
    } finally {
      setUncontactLoading(false);
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

  const handleContactComplete = () => {
    setContactModalOpen(false);
    fetchVoters();
  };

  const handleAddVoter = async () => {
    setAddVoterLoading(true);
    setError(null);

    try {
      const token = user?.token || localStorage.getItem('auth_token');
      if (!token) {
        throw new Error('Not authenticated');
      }

      // Validate required fields
      if (!newVoter.firstName.trim() || !newVoter.lastName.trim() || 
          !newVoter.addressLine.trim() || !newVoter.city.trim() || 
          !newVoter.zip.trim() || !newVoter.age.trim()) {
        throw new Error('Please fill in all required fields');
      }

      const voterData = {
        firstName: newVoter.firstName,
        lastName: newVoter.lastName,
        addressLine: newVoter.addressLine,
        city: newVoter.city,
        state: newVoter.state,
        zip: newVoter.zip,
        age: parseInt(newVoter.age),
        gender: newVoter.gender,
        cellPhone: newVoter.cellPhone,
        email: newVoter.email,
        voteFrequency: newVoter.voteFrequency === 'NonVoter' ? 0 : 
                       newVoter.voteFrequency === 'Infrequent' ? 1 : 2,
        partyAffiliation: newVoter.partyAffiliation
      };

      const response = await fetch(`${API_BASE_URL}/api/voters`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(voterData)
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to add voter');
      }

      const result = await response.json();
      
      // Success
      setSuccessMessage(`Successfully added ${newVoter.firstName} ${newVoter.lastName}`);
      setAddVoterDialogOpen(false);
      
      // Reset form
      setNewVoter({
        firstName: '',
        lastName: '',
        addressLine: '',
        city: '',
        state: 'AL',
        zip: '',
        age: '',
        gender: 'Unknown',
        cellPhone: '',
        email: '',
        voteFrequency: 'NonVoter',
        partyAffiliation: ''
      });
      
      // Refresh the voter list
      fetchVoters();
      
      // Clear success message after 5 seconds
      setTimeout(() => setSuccessMessage(null), 5000);

    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add voter');
    } finally {
      setAddVoterLoading(false);
    }
  };

  return (
    <Paper sx={{ 
      width: '100%', 
      overflow: 'hidden',
      mx: { xs: 0, sm: 'auto' }, // No margin on mobile, auto on larger screens
      borderRadius: { xs: 0, sm: 1 } // No border radius on mobile for full edge-to-edge
    }}>
      {/* View Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs value={currentView} onChange={(e, newValue) => setCurrentView(newValue)} aria-label="voter view tabs">
          <Tab 
            icon={<ListIcon />} 
            iconPosition="start" 
            label="List View" 
            value="list"
            sx={{ minHeight: { xs: 48, sm: 64 } }}
          />
          <Tab 
            icon={<MapIcon />} 
            iconPosition="start" 
            label="Map View" 
            value="map"
            sx={{ minHeight: { xs: 48, sm: 64 } }}
          />
        </Tabs>
      </Box>

      {/* Show Map View */}
      {currentView === 'map' ? (
        <Box sx={{ height: 'calc(100vh - 200px)' }}>
          <VoterMapViewWrapper
            voters={voters}
            loading={loading}
            onRefresh={fetchVoters}
            currentLocation={location}
            onContactComplete={handleContactComplete}
          />
        </Box>
      ) : (
        <>
          {/* Filter Controls */}
          <Box sx={{ p: { xs: 1, sm: 2 }, borderBottom: 1, borderColor: 'divider' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6" sx={{ fontSize: { xs: '1.1rem', sm: '1.25rem' } }}>
                Voter List ({total} voters)
                {useLocation && (
                  <>
                    <Chip 
                      icon={<LocationOn />} 
                      label={`Within ${Math.round((filters.radiusKm || 3.2) / 1.60934)} mi`} 
                      color="success" 
                      size="small" 
                      sx={{ ml: { xs: 1, sm: 2 } }} 
                    />
                    {filters.useTravelDistance && (
                      <Chip 
                        label={`Travel distance (some may use straight-line)`} 
                        color="info" 
                        size="small" 
                        variant="outlined"
                        sx={{ ml: 1 }} 
                        title="When Google Maps can't calculate routes, we fall back to straight-line distance"
                      />
                    )}
                  </>
                )}
              </Typography>
              
              {selectedVoters.length > 0 && (
                <Chip
                  label={`${selectedVoters.length} selected`}
                  color="primary"
                  size="small"
                  variant="filled"
                />
              )}
            </Box>
        
        <Box sx={{ display: 'flex', gap: { xs: 1, sm: 2 }, flexWrap: 'wrap', alignItems: 'center', mb: 2 }}>
          <TextField
            size="small"
            label="Search"
            value={filterInputs.searchName}
            onChange={(e) => handleFilterChange('searchName', e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                applyFilters();
              }
            }}
            sx={{ minWidth: { xs: 120, sm: 200 }, flex: { xs: '1 1 auto', sm: 'none' } }}
            placeholder="Name, address, city or ZIP"
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
          
          <FormControl size="small" sx={{ minWidth: { xs: 100, sm: 140 }, flex: { xs: '1 1 auto', sm: 'none' } }}>
            <InputLabel>Party</InputLabel>
            <Select
              value={filterInputs.partyAffiliation}
              label="Party"
              onChange={(e) => handleFilterChange('partyAffiliation', e.target.value)}
            >
              <MenuItem value="">All</MenuItem>
              <MenuItem value="Democratic">Democratic</MenuItem>
              <MenuItem value="Republican">Republican</MenuItem>
              <MenuItem value="Non-Partisan">Non-Partisan</MenuItem>
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
            onClick={useLocation ? () => { 
              setUseLocation(false); 
              setLocation(null); 
              setFilters(prev => ({ ...prev, sortBy: 'lastName' }));
            } : getCurrentLocation}
            color={useLocation ? "success" : "primary"}
            size={isMobile ? "small" : "medium"}
            sx={{ minWidth: { xs: 'auto', sm: 'auto' } }}
          >
            {isMobile 
              ? (useLocation ? "Off" : "Near") 
              : (useLocation ? "Turn Off Location" : "Find Nearby")
            }
          </Button>
          
          {useLocation && (
            <>
              <FormControlLabel
                control={
                  <Switch
                    checked={filters.useTravelDistance || false}
                    onChange={(e) => setFilters({...filters, useTravelDistance: e.target.checked})}
                    size="small"
                  />
                }
                label={isMobile ? "Travel" : "Travel Distance"}
                sx={{ ml: 1 }}
              />
              
              {filters.useTravelDistance && (
                <>
                  <FormControl size="small" sx={{ minWidth: 100 }}>
                    <Select
                      value={filters.travelMode || 'walking'}
                      onChange={(e) => setFilters({...filters, travelMode: e.target.value as 'driving' | 'walking'})}
                      size="small"
                    >
                      <MenuItem value="walking">Walking</MenuItem>
                      <MenuItem value="driving">Driving</MenuItem>
                    </Select>
                  </FormControl>
                  
                  <FormControl size="small" sx={{ minWidth: 80 }}>
                    <Select
                      value={filters.radiusKm || 3.2}
                      onChange={(e) => setFilters({...filters, radiusKm: Number(e.target.value)})}
                      size="small"
                    >
                      <MenuItem value={1.6}>1 mi</MenuItem>
                      <MenuItem value={3.2}>2 mi</MenuItem>
                      <MenuItem value={4.8}>3 mi</MenuItem>
                      <MenuItem value={8}>5 mi</MenuItem>
                      <MenuItem value={16}>10 mi</MenuItem>
                      <MenuItem value={24}>15 mi</MenuItem>
                    </Select>
                  </FormControl>
                </>
              )}
            </>
          )}
          
          <Button
            variant="outlined"
            startIcon={<LocalOffer />}
            endIcon={<ExpandMore sx={{ transform: showAdvancedFilters ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }} />}
            onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
            size={isMobile ? "small" : "medium"}
            sx={{ minWidth: { xs: 'auto', sm: 'auto' } }}
          >
            {isMobile ? "Tags" : "Filter by Tags"}
          </Button>
          
          <Button
            variant="contained"
            startIcon={<PersonAdd />}
            onClick={() => setAddVoterDialogOpen(true)}
            size={isMobile ? "small" : "medium"}
            color="success"
            sx={{ minWidth: { xs: 'auto', sm: 'auto' } }}
          >
            {isMobile ? "Add" : "Add Voter"}
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
        
        {/* Advanced Filters - Tags */}
        <Collapse in={showAdvancedFilters}>
          <Box sx={{ pt: 2, pb: 1 }}>
            <Typography variant="subtitle2" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <LocalOffer fontSize="small" />
              Filter by Tags
            </Typography>
            
            <Autocomplete
              multiple
              size="small"
              options={availableTags}
              getOptionLabel={(option) => option.tagName}
              value={selectedTags}
              onChange={handleTagFilterChange}
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
                  placeholder={selectedTags.length === 0 ? "Select tags to filter by..." : ""}
                  variant="outlined"
                  size="small"
                />
              )}
              sx={{ minWidth: 250 }}
            />
            
            {selectedTags.length > 0 && (
              <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                Showing voters with {selectedTags.length === 1 ? 'this tag' : 'any of these tags'}
              </Typography>
            )}
          </Box>
        </Collapse>
        
        {/* Bulk Operations Toolbar - Only for Admin/SuperAdmin */}
        {(user?.role === 'admin' || user?.role === 'superadmin') && (
          <Fade in={selectedVoters.length > 0}>
            <Toolbar
            sx={{
              bgcolor: 'primary.light',
              color: 'primary.contrastText',
              borderRadius: 1,
              mt: 2,
              minHeight: { xs: 48, sm: 56 },
              px: { xs: 1, sm: 2 }
            }}
          >
            <Typography variant="subtitle1" sx={{ flex: 1, fontSize: { xs: '0.9rem', sm: '1rem' } }}>
              {selectedVoters.length} voter(s) selected
            </Typography>
            
            <Box sx={{ display: 'flex', gap: 1 }}>
              {(user?.role === 'admin' || user?.role === 'superadmin') && (
                <>
                  <Button
                    variant="contained"
                    size={isMobile ? "small" : "medium"}
                    startIcon={<Label />}
                    onClick={() => openBulkTagDialog('add')}
                    sx={{ 
                      bgcolor: 'success.main', 
                      color: 'white',
                      '&:hover': { bgcolor: 'success.dark' },
                      boxShadow: 2
                    }}
                  >
                    Add Tags
                  </Button>
                  
                  <Button
                    variant="contained"
                    size={isMobile ? "small" : "medium"}
                    startIcon={<LabelOff />}
                    onClick={() => openBulkTagDialog('remove')}
                    sx={{ 
                      bgcolor: 'warning.main', 
                      color: 'white',
                      '&:hover': { bgcolor: 'warning.dark' },
                      boxShadow: 2
                    }}
                  >
                    Remove Tags
                  </Button>
                </>
              )}
              
              <Button
                variant="text"
                size={isMobile ? "small" : "medium"}
                onClick={() => setSelectedVoters([])}
                sx={{ 
                  color: 'white',
                  '&:hover': { bgcolor: 'primary.dark' }
                }}
              >
                Clear
              </Button>
            </Box>
          </Toolbar>
        </Fade>
        )}
      </Box>

      {error && (
        <Alert severity="error" sx={{ m: 2 }}>
          {error}
        </Alert>
      )}

      {successMessage && (
        <Alert severity="success" sx={{ m: 2 }} onClose={() => setSuccessMessage(null)}>
          {successMessage}
        </Alert>
      )}

      {/* Voter Table */}
      <TableContainer sx={{ 
        maxHeight: 600,
        width: '100%',
        mx: 0 // Remove any horizontal margins
      }}>
        <Table stickyHeader size={isMobile ? "small" : "medium"}>
          <TableHead>
            <TableRow>
              {(user?.role === 'admin' || user?.role === 'superadmin') && (
                <TableCell padding="checkbox">
                  <Checkbox
                    indeterminate={selectedVoters.length > 0 && selectedVoters.length < voters.length}
                    checked={voters.length > 0 && selectedVoters.length === voters.length}
                    onChange={handleSelectAllVoters}
                    inputProps={{ 'aria-label': 'select all voters' }}
                  />
                </TableCell>
              )}
              <TableCell>Name</TableCell>
              <TableCell>Address</TableCell>
              {!isMobile && <TableCell>Distance</TableCell>}
              {!isMobile && <TableCell>Age</TableCell>}
              {!isMobile && <TableCell>Vote Frequency</TableCell>}
              <TableCell>Party</TableCell>
              {!isMobile && <TableCell>Tags</TableCell>}
              <TableCell>Status</TableCell>
              {!isMobile && <TableCell>Contact Info</TableCell>}
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={(user?.role === 'admin' || user?.role === 'superadmin') ? (isMobile ? 6 : 10) : (isMobile ? 5 : 9)} sx={{ textAlign: 'center', py: 4 }}>
                  <CircularProgress />
                </TableCell>
              </TableRow>
            ) : voters.length === 0 ? (
              <TableRow>
                <TableCell colSpan={(user?.role === 'admin' || user?.role === 'superadmin') ? (isMobile ? 6 : 10) : (isMobile ? 5 : 9)} sx={{ textAlign: 'center', py: 4 }}>
                  No voters found
                </TableCell>
              </TableRow>
            ) : (
              voters.map((voter) => (
                <TableRow
                  key={voter.lalVoterId}
                  sx={{ 
                    '&:last-child td, &:last-child th': { border: 0 },
                    bgcolor: selectedVoters.includes(voter.lalVoterId) ? 'action.selected' : 'inherit'
                  }}
                >
                  {(user?.role === 'admin' || user?.role === 'superadmin') && (
                    <TableCell padding="checkbox">
                      <Checkbox
                        checked={selectedVoters.includes(voter.lalVoterId)}
                        onChange={() => handleSelectVoter(voter.lalVoterId)}
                        inputProps={{ 'aria-label': `select voter ${voter.firstName} ${voter.lastName}` }}
                      />
                    </TableCell>
                  )}
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
                              Age {voter.age} • {voter.gender}{voter.ethnicity && ` • ${voter.ethnicity}`}{voter.partyAffiliation && ` • ${voter.partyAffiliation}`}
                            </Typography>
                            {voter.cellPhone && (
                              <Typography 
                                variant="caption" 
                                component="a"
                                href={`tel:${voter.cellPhone}`}
                                sx={{ 
                                  display: 'block',
                                  color: 'primary.main',
                                  textDecoration: 'none',
                                  '&:hover': {
                                    textDecoration: 'underline'
                                  }
                                }}
                              >
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
                            📍 {(voter.distanceKm * 0.621371).toFixed(2)} mi{voter.distanceIsStraightLine ? ' (approx)' : ''}
                          </Typography>
                        )}
                      </Box>
                    </Box>
                  </TableCell>
                  
                  {!isMobile && (
                    <TableCell>
                      {voter.distanceKm ? (
                        <Typography variant="body2" color="primary" sx={{ fontWeight: 'medium' }}>
                          📍 {(voter.distanceKm * 0.621371).toFixed(2)} mi{voter.distanceIsStraightLine ? ' (approx)' : ''}
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
                        {voter.ethnicity && ` • ${voter.ethnicity}`}
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
                    {voter.partyAffiliation ? (
                      <Typography variant="body2">
                        {voter.partyAffiliation}
                      </Typography>
                    ) : (
                      <Typography variant="caption" color="text.secondary">
                        -
                      </Typography>
                    )}
                  </TableCell>
                  
                  {!isMobile && (
                    <TableCell>
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, maxWidth: isMobile ? 120 : 200 }}>
                        {voter.tags && voter.tags.length > 0 ? (
                          voter.tags.map((tag: VoterTag) => (
                            <Chip
                              key={tag.id}
                              label={tag.tagName}
                              size="small"
                              sx={{
                                backgroundColor: tag.color || '#2196F3',
                                color: 'white',
                                fontSize: isMobile ? '0.6rem' : '0.7rem',
                                height: isMobile ? 20 : 24,
                                '& .MuiChip-label': {
                                  px: isMobile ? 0.5 : 1
                                }
                              }}
                            />
                          ))
                        ) : (
                          <Typography variant="caption" color="text.secondary">
                            -
                          </Typography>
                        )}
                      </Box>
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
                        label="Not Contacted"
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
                            <Typography 
                              variant="caption"
                              component="a"
                              href={`tel:${voter.cellPhone}`}
                              sx={{ 
                                color: 'primary.main', 
                                textDecoration: 'none',
                                '&:hover': {
                                  textDecoration: 'underline'
                                }
                              }}
                            >
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
                    <Box sx={{ display: 'flex', gap: 0.5, flexDirection: isMobile ? 'column' : 'row' }}>
                      <Button
                        variant="contained"
                        size="small"
                        startIcon={isMobile ? undefined : <ContactPhone />}
                        onClick={() => handleContactClick(voter)}
                        disabled={loading}
                        sx={{ minWidth: isMobile ? '60px' : 'auto' }}
                      >
                        Contact
                      </Button>
                      
                      {user?.role === 'superadmin' && voter.isContacted && (
                        <Button
                          variant="outlined"
                          size="small"
                          color="warning"
                          onClick={() => handleUncontactClick(voter)}
                          disabled={loading}
                          sx={{ 
                            minWidth: isMobile ? '60px' : 'auto',
                            fontSize: isMobile ? '0.7rem' : 'inherit'
                          }}
                        >
                          Uncontact
                        </Button>
                      )}
                    </Box>
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
        user={user}
      />

      {/* Bulk Tag Operations Dialog - Only for Admin/SuperAdmin */}
      {(user?.role === 'admin' || user?.role === 'superadmin') && (
        <Dialog open={bulkTagDialog} onClose={() => {
        if (!bulkLoading) {
          setBulkTagDialog(false);
          setBulkSelectedTags([]);
        }
      }} maxWidth="sm" fullWidth>
        <DialogTitle>
          {bulkOperation === 'add' ? 'Add Tags to Voters' : 'Remove Tags from Voters'}
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            {bulkOperation === 'add' 
              ? `Add tags to ${selectedVoters.length} selected voter(s). Selected voters will have these tags added to their existing tags.`
              : `Remove tags from ${selectedVoters.length} selected voter(s). Only voters who currently have these tags will be affected.`
            }
          </Typography>
          
          <Autocomplete
            multiple
            size="small"
            options={availableTags}
            getOptionLabel={(option) => option.tagName}
            value={bulkSelectedTags}
            onChange={(event, newValue) => setBulkSelectedTags(newValue)}
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
                label={`Select tags to ${bulkOperation}`}
                placeholder={bulkSelectedTags.length === 0 ? `Choose tags to ${bulkOperation}...` : ""}
                variant="outlined"
                size="small"
                fullWidth
                margin="normal"
              />
            )}
            disabled={bulkLoading}
          />
          
          {bulkSelectedTags.length > 0 && (
            <Box sx={{ mt: 2, p: 2, bgcolor: 'action.hover', borderRadius: 1 }}>
              <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                {bulkOperation === 'add' ? 'Tags to add:' : 'Tags to remove:'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {bulkSelectedTags.length} tag(s) will be {bulkOperation === 'add' ? 'added to' : 'removed from'} {selectedVoters.length} voter(s)
              </Typography>
            </Box>
          )}

          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setBulkTagDialog(false);
            setBulkSelectedTags([]);
          }} disabled={bulkLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleBulkTagOperation}
            variant="contained" 
            disabled={bulkLoading || bulkSelectedTags.length === 0}
            startIcon={bulkLoading ? <CircularProgress size={20} /> : (bulkOperation === 'add' ? <Label /> : <LabelOff />)}
            color={bulkOperation === 'add' ? 'primary' : 'warning'}
          >
            {bulkLoading 
              ? `${bulkOperation === 'add' ? 'Adding' : 'Removing'}...` 
              : `${bulkOperation === 'add' ? 'Add Tags' : 'Remove Tags'}`
            }
          </Button>
        </DialogActions>
      </Dialog>
      )}

      {/* Uncontact Confirmation Dialog - Only for SuperAdmin */}
      {user?.role === 'superadmin' && (
        <Dialog open={uncontactDialogOpen} onClose={() => {
          if (!uncontactLoading) {
            setUncontactDialogOpen(false);
            setVoterToUncontact(null);
          }
        }} maxWidth="sm" fullWidth>
          <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <DeleteForever color="warning" />
            Uncontact Voter
          </DialogTitle>
          <DialogContent>
            {voterToUncontact && (
              <>
                <Alert severity="warning" sx={{ mb: 2 }}>
                  <strong>Warning:</strong> This action will permanently delete the most recent contact record for this voter.
                </Alert>
                
                <Typography variant="body1" paragraph>
                  Are you sure you want to uncontact <strong>{voterToUncontact.firstName} {voterToUncontact.lastName}</strong>?
                </Typography>
                
                <Box sx={{ p: 2, bgcolor: 'grey.50', borderRadius: 1, mb: 2 }}>
                  <Typography variant="body2">
                    <strong>Voter:</strong> {voterToUncontact.firstName} {voterToUncontact.lastName}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Address:</strong> {voterToUncontact.addressLine}, {voterToUncontact.city}, {voterToUncontact.state} {voterToUncontact.zip}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Current Status:</strong> {voterToUncontact.lastContactStatus || 'Contacted'}
                  </Typography>
                </Box>
                
                <Typography variant="body2" color="text.secondary">
                  This will delete the most recent contact record and may reset the voter to "uncontacted" status if no other contacts exist. 
                  This action cannot be undone and all SuperAdmins will be notified.
                </Typography>
              </>
            )}

            {error && (
              <Alert severity="error" sx={{ mt: 2 }}>
                {error}
              </Alert>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => {
              setUncontactDialogOpen(false);
              setVoterToUncontact(null);
            }} disabled={uncontactLoading}>
              Cancel
            </Button>
            <Button 
              onClick={handleUncontactConfirm}
              variant="contained" 
              color="warning"
              disabled={uncontactLoading}
              startIcon={uncontactLoading ? <CircularProgress size={20} /> : <DeleteForever />}
            >
              {uncontactLoading ? 'Uncontacting...' : 'Uncontact Voter'}
            </Button>
          </DialogActions>
        </Dialog>
      )}

      {/* Add Voter Dialog - Available to all users */}
      <Dialog open={addVoterDialogOpen} onClose={() => {
        if (!addVoterLoading) {
          setAddVoterDialogOpen(false);
        }
      }} maxWidth="md" fullWidth>
        <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <PersonAdd color="success" />
          Add New Voter
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' }, gap: 2 }}>
              <TextField
                label="First Name"
                value={newVoter.firstName}
                onChange={(e) => setNewVoter({ ...newVoter, firstName: e.target.value })}
                fullWidth
                required
                disabled={addVoterLoading}
              />
              <TextField
                label="Last Name"
                value={newVoter.lastName}
                onChange={(e) => setNewVoter({ ...newVoter, lastName: e.target.value })}
                fullWidth
                required
                disabled={addVoterLoading}
              />
              <TextField
                label="Street Address"
                value={newVoter.addressLine}
                onChange={(e) => setNewVoter({ ...newVoter, addressLine: e.target.value })}
                fullWidth
                required
                disabled={addVoterLoading}
              />
              <TextField
                label="City"
                value={newVoter.city}
                onChange={(e) => setNewVoter({ ...newVoter, city: e.target.value })}
                fullWidth
                required
                disabled={addVoterLoading}
              />
              <FormControl fullWidth required>
                <InputLabel>State</InputLabel>
                <Select
                  value={newVoter.state}
                  onChange={(e) => setNewVoter({ ...newVoter, state: e.target.value })}
                  label="State"
                  disabled={addVoterLoading}
                >
                  <MenuItem value="AL">Alabama</MenuItem>
                  <MenuItem value="AK">Alaska</MenuItem>
                  <MenuItem value="AZ">Arizona</MenuItem>
                  <MenuItem value="AR">Arkansas</MenuItem>
                  <MenuItem value="CA">California</MenuItem>
                  <MenuItem value="CO">Colorado</MenuItem>
                  <MenuItem value="CT">Connecticut</MenuItem>
                  <MenuItem value="DE">Delaware</MenuItem>
                  <MenuItem value="FL">Florida</MenuItem>
                  <MenuItem value="GA">Georgia</MenuItem>
                  <MenuItem value="HI">Hawaii</MenuItem>
                  <MenuItem value="ID">Idaho</MenuItem>
                  <MenuItem value="IL">Illinois</MenuItem>
                  <MenuItem value="IN">Indiana</MenuItem>
                  <MenuItem value="IA">Iowa</MenuItem>
                  <MenuItem value="KS">Kansas</MenuItem>
                  <MenuItem value="KY">Kentucky</MenuItem>
                  <MenuItem value="LA">Louisiana</MenuItem>
                  <MenuItem value="ME">Maine</MenuItem>
                  <MenuItem value="MD">Maryland</MenuItem>
                  <MenuItem value="MA">Massachusetts</MenuItem>
                  <MenuItem value="MI">Michigan</MenuItem>
                  <MenuItem value="MN">Minnesota</MenuItem>
                  <MenuItem value="MS">Mississippi</MenuItem>
                  <MenuItem value="MO">Missouri</MenuItem>
                  <MenuItem value="MT">Montana</MenuItem>
                  <MenuItem value="NE">Nebraska</MenuItem>
                  <MenuItem value="NV">Nevada</MenuItem>
                  <MenuItem value="NH">New Hampshire</MenuItem>
                  <MenuItem value="NJ">New Jersey</MenuItem>
                  <MenuItem value="NM">New Mexico</MenuItem>
                  <MenuItem value="NY">New York</MenuItem>
                  <MenuItem value="NC">North Carolina</MenuItem>
                  <MenuItem value="ND">North Dakota</MenuItem>
                  <MenuItem value="OH">Ohio</MenuItem>
                  <MenuItem value="OK">Oklahoma</MenuItem>
                  <MenuItem value="OR">Oregon</MenuItem>
                  <MenuItem value="PA">Pennsylvania</MenuItem>
                  <MenuItem value="RI">Rhode Island</MenuItem>
                  <MenuItem value="SC">South Carolina</MenuItem>
                  <MenuItem value="SD">South Dakota</MenuItem>
                  <MenuItem value="TN">Tennessee</MenuItem>
                  <MenuItem value="TX">Texas</MenuItem>
                  <MenuItem value="UT">Utah</MenuItem>
                  <MenuItem value="VT">Vermont</MenuItem>
                  <MenuItem value="VA">Virginia</MenuItem>
                  <MenuItem value="WA">Washington</MenuItem>
                  <MenuItem value="WV">West Virginia</MenuItem>
                  <MenuItem value="WI">Wisconsin</MenuItem>
                  <MenuItem value="WY">Wyoming</MenuItem>
                </Select>
              </FormControl>
              <TextField
                label="ZIP Code"
                value={newVoter.zip}
                onChange={(e) => setNewVoter({ ...newVoter, zip: e.target.value })}
                fullWidth
                required
                disabled={addVoterLoading}
              />
              <TextField
                label="Age"
                type="number"
                value={newVoter.age}
                onChange={(e) => setNewVoter({ ...newVoter, age: e.target.value })}
                fullWidth
                required
                disabled={addVoterLoading}
                InputProps={{ inputProps: { min: 18, max: 120 } }}
              />
              <FormControl fullWidth>
                <InputLabel>Gender</InputLabel>
                <Select
                  value={newVoter.gender}
                  onChange={(e) => setNewVoter({ ...newVoter, gender: e.target.value })}
                  label="Gender"
                  disabled={addVoterLoading}
                >
                  <MenuItem value="Male">Male</MenuItem>
                  <MenuItem value="Female">Female</MenuItem>
                  <MenuItem value="Unknown">Unknown</MenuItem>
                </Select>
              </FormControl>
              <TextField
                label="Cell Phone"
                value={newVoter.cellPhone}
                onChange={(e) => setNewVoter({ ...newVoter, cellPhone: e.target.value })}
                fullWidth
                disabled={addVoterLoading}
                placeholder="+1234567890"
              />
              <TextField
                label="Email"
                type="email"
                value={newVoter.email}
                onChange={(e) => setNewVoter({ ...newVoter, email: e.target.value })}
                fullWidth
                disabled={addVoterLoading}
              />
              <FormControl fullWidth>
                <InputLabel>Vote Frequency</InputLabel>
                <Select
                  value={newVoter.voteFrequency}
                  onChange={(e) => setNewVoter({ ...newVoter, voteFrequency: e.target.value })}
                  label="Vote Frequency"
                  disabled={addVoterLoading}
                >
                  <MenuItem value="NonVoter">Non-voter</MenuItem>
                  <MenuItem value="Infrequent">Infrequent (1-2)</MenuItem>
                  <MenuItem value="Frequent">Frequent (3+)</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Party Affiliation</InputLabel>
                <Select
                  value={newVoter.partyAffiliation}
                  onChange={(e) => setNewVoter({ ...newVoter, partyAffiliation: e.target.value })}
                  label="Party Affiliation"
                  disabled={addVoterLoading}
                >
                  <MenuItem value="">None</MenuItem>
                  <MenuItem value="Democrat">Democrat</MenuItem>
                  <MenuItem value="Republican">Republican</MenuItem>
                  <MenuItem value="Non-Partisan">Non-Partisan</MenuItem>
                </Select>
              </FormControl>
            </Box>
          </Box>

          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setAddVoterDialogOpen(false);
            setError(null);
          }} disabled={addVoterLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleAddVoter}
            variant="contained" 
            color="success"
            disabled={addVoterLoading}
            startIcon={addVoterLoading ? <CircularProgress size={20} /> : <Add />}
          >
            {addVoterLoading ? 'Adding...' : 'Add Voter'}
          </Button>
        </DialogActions>
      </Dialog>
      </>
      )}
    </Paper>
  );
};

export default VoterList;