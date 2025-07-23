import React, { useState, useEffect, useMemo } from 'react';
import { GoogleMap, useJsApiLoader, Marker, InfoWindow, MarkerClusterer } from '@react-google-maps/api';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Chip,
  CircularProgress,
  Alert,
  Avatar,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Divider,
  Collapse,
  Fab
} from '@mui/material';
import {
  LocationOn,
  MyLocation,
  Phone,
  Home,
  CheckCircle,
  RadioButtonUnchecked,
  ContactPhone,
  ExpandMore,
  ExpandLess,
  PersonAdd
} from '@mui/icons-material';
import { Voter, ContactStatus, VoterSupport } from '../types';
import ContactModal from './ContactModal';
import AddVoterDialog from './AddVoterDialog';
import { API_BASE_URL } from '../config';

interface VoterMapViewProps {
  voters: Voter[];
  loading: boolean;
  onRefresh: () => void;
  currentLocation: { latitude: number; longitude: number } | null;
  onContactComplete?: () => void;
  googleMapsApiKey: string;
}

interface HouseData {
  address: string;
  latitude: number;
  longitude: number;
  voters: Voter[];
  allContacted: boolean;
  contactedCount: number;
  totalCount: number;
}

const mapContainerStyle = {
  width: '100%',
  height: 'calc(100vh - 200px)' // Adjust based on your layout
};

const defaultCenter = {
  lat: 33.4152, // Birmingham, AL
  lng: -86.8025
};

const libraries = ['places'] as any[];

const VoterMapView: React.FC<VoterMapViewProps> = ({
  voters,
  loading,
  onRefresh,
  currentLocation,
  onContactComplete,
  googleMapsApiKey
}) => {
  const [selectedHouse, setSelectedHouse] = useState<HouseData | null>(null);
  const [map, setMap] = useState<google.maps.Map | null>(null);
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [selectedVoter, setSelectedVoter] = useState<Voter | null>(null);
  const [legendOpen, setLegendOpen] = useState(false); // Default to collapsed
  const [addVoterDialogOpen, setAddVoterDialogOpen] = useState(false);

  // Use the Google Maps loader hook
  const { isLoaded, loadError } = useJsApiLoader({
    googleMapsApiKey: googleMapsApiKey,
    id: 'google-map-script',
    libraries
  });

  // Group voters by address
  const houseData = useMemo((): HouseData[] => {
    const grouped = voters.reduce((acc, voter) => {
      if (!voter.latitude || !voter.longitude) return acc;
      
      const key = `${voter.latitude},${voter.longitude}`;
      
      if (!acc[key]) {
        acc[key] = {
          address: `${voter.addressLine}, ${voter.city}, ${voter.state} ${voter.zip}`,
          latitude: voter.latitude,
          longitude: voter.longitude,
          voters: [],
          allContacted: true,
          contactedCount: 0,
          totalCount: 0
        };
      }
      
      acc[key].voters.push(voter);
      acc[key].totalCount++;
      
      if (voter.isContacted && voter.lastContactStatus !== 'not-home') {
        acc[key].contactedCount++;
      } else {
        acc[key].allContacted = false;
      }
      
      return acc;
    }, {} as Record<string, HouseData>);

    return Object.values(grouped);
  }, [voters]);

  // Center map on user location when available
  useEffect(() => {
    if (map && currentLocation) {
      map.panTo({
        lat: currentLocation.latitude,
        lng: currentLocation.longitude
      });
    }
  }, [map, currentLocation]);

  const getMarkerIcon = (house: HouseData) => {
    let color: string;
    if (house.allContacted) {
      color = '#0F9D58'; // Green
    } else if (house.contactedCount > 0) {
      color = '#F4B400'; // Yellow/Orange
    } else {
      color = '#DB4437'; // Red
    }

    // Return a custom marker with the voter count
    return {
      path: google.maps.SymbolPath.CIRCLE,
      scale: 20,
      fillColor: color,
      fillOpacity: 0.9,
      strokeColor: '#ffffff',
      strokeWeight: 2,
      labelOrigin: new google.maps.Point(0, 0)
    };
  };

  const getMarkerLabel = (house: HouseData) => {
    return {
      text: house.totalCount.toString(),
      color: '#ffffff',
      fontSize: '12px',
      fontWeight: 'bold'
    };
  };

  const handleMarkerClick = (house: HouseData) => {
    setSelectedHouse(house);
  };

  const handleContactVoter = (voter: Voter) => {
    setSelectedVoter(voter);
    setContactModalOpen(true);
  };

  const handleContactSubmit = async (status: ContactStatus, notes: string, voterSupport?: VoterSupport, audioUrl?: string, audioDuration?: number, photoUrl?: string) => {
    if (!selectedVoter) return;

    try {
      const response = await fetch(`${API_BASE_URL}/api/contacts`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('auth_token')}`
        },
        body: JSON.stringify({
          voterId: selectedVoter.lalVoterId,
          status,
          notes,
          voterSupport,
          audioUrl,
          audioDuration,
          photoUrl,
          location: currentLocation
        })
      });

      if (response.ok) {
        setContactModalOpen(false);
        if (onContactComplete) {
          onContactComplete();
        }
        onRefresh();
      }
    } catch (error) {
      console.error('Failed to submit contact:', error);
    }
  };

  const getVoterStatusIcon = (voter: Voter) => {
    if (voter.isContacted && voter.lastContactStatus !== 'not-home') {
      return <CheckCircle color="success" fontSize="small" />;
    }
    return <RadioButtonUnchecked color="action" fontSize="small" />;
  };

  const getVoterSupportChip = (voter: Voter) => {
    if (!voter.voterSupport) return null;
    
    const supportColors = {
      'StrongYes': 'success',
      'LeaningYes': 'primary',
      'Undecided': 'warning',
      'LeaningNo': 'error',
      'StrongNo': 'error'
    } as const;

    const supportLabels = {
      'StrongYes': 'Strong Yes',
      'LeaningYes': 'Leaning Yes',
      'Undecided': 'Undecided',
      'LeaningNo': 'Leaning No',
      'StrongNo': 'Strong No'
    };

    return (
      <Chip
        label={supportLabels[voter.voterSupport] || voter.voterSupport}
        size="small"
        color={supportColors[voter.voterSupport] || 'default'}
      />
    );
  };

  if (loadError) {
    return (
      <Alert severity="error">
        Error loading Google Maps
      </Alert>
    );
  }

  if (!isLoaded) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (voters.length === 0) {
    return (
      <Box 
        display="flex" 
        flexDirection="column" 
        alignItems="center" 
        justifyContent="center" 
        height="100%" 
        p={4}
      >
        <LocationOn sx={{ fontSize: 60, color: 'text.secondary', mb: 2 }} />
        <Typography variant="h6" color="text.secondary" gutterBottom>
          No Voters to Display
        </Typography>
        <Typography variant="body2" color="text.secondary" align="center">
          {currentLocation 
            ? "No voters found in your area. Try adjusting the search radius in List View."
            : "Enable location access to see voters near you, or search by ZIP code in List View."}
        </Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ height: '100%', position: 'relative' }}>
      {/* Map Legend */}
      <Card sx={{ position: 'absolute', top: 10, left: 10, zIndex: 1, maxWidth: 250 }}>
        <CardContent sx={{ py: 1 }}>
          <Box 
            sx={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'space-between',
              cursor: 'pointer'
            }}
            onClick={() => setLegendOpen(!legendOpen)}
          >
            <Typography variant="subtitle2">Map Legend</Typography>
            <IconButton size="small">
              {legendOpen ? <ExpandLess /> : <ExpandMore />}
            </IconButton>
          </Box>
          <Collapse in={legendOpen}>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5, mt: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ 
                  width: 24, 
                  height: 24, 
                  borderRadius: '50%', 
                  backgroundColor: '#0F9D58',
                  border: '2px solid white',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'white',
                  fontSize: '10px',
                  fontWeight: 'bold'
                }}>
                  #
                </Box>
                <Typography variant="caption">All Contacted</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ 
                  width: 24, 
                  height: 24, 
                  borderRadius: '50%', 
                  backgroundColor: '#F4B400',
                  border: '2px solid white',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'white',
                  fontSize: '10px',
                  fontWeight: 'bold'
                }}>
                  #
                </Box>
                <Typography variant="caption">Partially Contacted</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ 
                  width: 24, 
                  height: 24, 
                  borderRadius: '50%', 
                  backgroundColor: '#DB4437',
                  border: '2px solid white',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'white',
                  fontSize: '10px',
                  fontWeight: 'bold'
                }}>
                  #
                </Box>
                <Typography variant="caption">Not Contacted</Typography>
              </Box>
              {currentLocation && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Box sx={{ 
                    width: 24, 
                    height: 24, 
                    borderRadius: '50%', 
                    backgroundColor: '#4285F4',
                    border: '2px solid white',
                    boxShadow: '0 2px 4px rgba(0,0,0,0.3)'
                  }}>
                  </Box>
                  <Typography variant="caption">Your Location</Typography>
                </Box>
              )}
            </Box>
          </Collapse>
        </CardContent>
      </Card>

      {/* Map Stats */}
      <Card sx={{ position: 'absolute', top: 10, right: 10, zIndex: 1 }}>
        <CardContent sx={{ py: 1 }}>
          <Typography variant="subtitle2">
            {houseData.length} Houses • {voters.length} Voters
          </Typography>
        </CardContent>
      </Card>

      <GoogleMap
          mapContainerStyle={mapContainerStyle}
          center={currentLocation ? {
            lat: currentLocation.latitude,
            lng: currentLocation.longitude
          } : defaultCenter}
          zoom={18}
          onLoad={setMap}
          options={{
            streetViewControl: false,
            mapTypeControl: false,
            fullscreenControl: false
          }}
        >
          {/* User location marker */}
          {currentLocation && (
            <Marker
              position={{
                lat: currentLocation.latitude,
                lng: currentLocation.longitude
              }}
              icon={{
                path: google.maps.SymbolPath.CIRCLE,
                scale: 10,
                fillColor: '#4285F4',
                fillOpacity: 1,
                strokeColor: '#ffffff',
                strokeWeight: 2
              }}
              title="Your Location"
            />
          )}

          {/* House markers */}
          <MarkerClusterer>
            {(clusterer) => (
              <>
                {houseData.map((house, index) => (
                  <Marker
                    key={index}
                    position={{
                      lat: house.latitude,
                      lng: house.longitude
                    }}
                    icon={getMarkerIcon(house)}
                    label={getMarkerLabel(house)}
                    title={`${house.address} (${house.contactedCount}/${house.totalCount} contacted)`}
                    onClick={() => handleMarkerClick(house)}
                    clusterer={clusterer}
                  />
                ))}
              </>
            )}
          </MarkerClusterer>

          {/* Info Window for selected house */}
          {selectedHouse && (
            <InfoWindow
              position={{
                lat: selectedHouse.latitude,
                lng: selectedHouse.longitude
              }}
              onCloseClick={() => setSelectedHouse(null)}
            >
              <Box sx={{ maxWidth: 350, maxHeight: 400, overflow: 'auto' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                  <Home />
                  <Typography variant="subtitle1" fontWeight="bold">
                    {selectedHouse.address}
                  </Typography>
                </Box>
                
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  {selectedHouse.contactedCount} of {selectedHouse.totalCount} contacted
                </Typography>

                <Divider sx={{ my: 1 }} />

                <List dense>
                  {selectedHouse.voters.map((voter) => (
                    <ListItem key={voter.lalVoterId} sx={{ px: 0 }}>
                      <ListItemAvatar>
                        <Avatar sx={{ width: 32, height: 32 }}>
                          {getVoterStatusIcon(voter)}
                        </Avatar>
                      </ListItemAvatar>
                      <ListItemText
                        primary={`${voter.firstName} ${voter.lastName}`}
                        secondary={
                          <Box>
                            <Typography variant="caption" display="block">
                              Age: {voter.age || 'Unknown'}
                            </Typography>
                            {voter.cellPhone && (
                              <Typography variant="caption" display="block">
                                <Phone fontSize="inherit" /> {voter.cellPhone}
                              </Typography>
                            )}
                          </Box>
                        }
                      />
                      <ListItemSecondaryAction>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5, alignItems: 'flex-end' }}>
                          {getVoterSupportChip(voter)}
                          <IconButton
                            edge="end"
                            size="small"
                            onClick={() => handleContactVoter(voter)}
                            color="primary"
                          >
                            <ContactPhone />
                          </IconButton>
                        </Box>
                      </ListItemSecondaryAction>
                    </ListItem>
                  ))}
                </List>
              </Box>
            </InfoWindow>
          )}
        </GoogleMap>

      {/* Floating Action Button for Add Voter */}
      <Fab
        color="success"
        aria-label="add voter"
        sx={{
          position: 'absolute',
          bottom: 16,
          right: 16,
          zIndex: 1
        }}
        onClick={() => setAddVoterDialogOpen(true)}
      >
        <PersonAdd />
      </Fab>

      {/* Contact Modal */}
      {selectedVoter && (
        <ContactModal
          open={contactModalOpen}
          onClose={() => setContactModalOpen(false)}
          voter={selectedVoter}
          onSubmit={handleContactSubmit}
        />
      )}

      {/* Add Voter Dialog */}
      <AddVoterDialog
        open={addVoterDialogOpen}
        onClose={() => setAddVoterDialogOpen(false)}
        onSuccess={() => {
          onRefresh();
          setAddVoterDialogOpen(false);
        }}
        googleMapsApiKey={googleMapsApiKey}
      />

    </Box>
  );
};

export default VoterMapView;