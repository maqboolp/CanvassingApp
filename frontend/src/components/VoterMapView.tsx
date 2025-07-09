import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { GoogleMap, LoadScript, Marker, InfoWindow, MarkerClusterer } from '@react-google-maps/api';
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
  Divider
} from '@mui/material';
import {
  LocationOn,
  MyLocation,
  Phone,
  Home,
  CheckCircle,
  RadioButtonUnchecked,
  ContactPhone
} from '@mui/icons-material';
import { Voter, ContactStatus, VoterSupport } from '../types';
import ContactModal from './ContactModal';
import { API_BASE_URL } from '../config';

interface VoterMapViewProps {
  voters: Voter[];
  loading: boolean;
  onRefresh: () => void;
  currentLocation: { latitude: number; longitude: number } | null;
  onContactComplete?: () => void;
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

const VoterMapView: React.FC<VoterMapViewProps> = ({
  voters,
  loading,
  onRefresh,
  currentLocation,
  onContactComplete
}) => {
  const [selectedHouse, setSelectedHouse] = useState<HouseData | null>(null);
  const [map, setMap] = useState<google.maps.Map | null>(null);
  const [contactModalOpen, setContactModalOpen] = useState(false);
  const [selectedVoter, setSelectedVoter] = useState<Voter | null>(null);
  const [googleMapsError, setGoogleMapsError] = useState<string | null>(null);

  // Get Google Maps API key from environment
  const googleMapsApiKey = process.env.REACT_APP_GOOGLE_MAPS_API_KEY || '';

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

  const getMarkerIcon = (house: HouseData): string => {
    if (house.allContacted) {
      // Green house - all contacted
      return 'https://maps.google.com/mapfiles/ms/icons/green-dot.png';
    } else if (house.contactedCount > 0) {
      // Yellow house - partially contacted
      return 'https://maps.google.com/mapfiles/ms/icons/yellow-dot.png';
    } else {
      // Red house - not contacted
      return 'https://maps.google.com/mapfiles/ms/icons/red-dot.png';
    }
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
      'strongyes': 'success',
      'leaningyes': 'primary',
      'undecided': 'warning',
      'leaningno': 'error',
      'strongno': 'error'
    } as const;

    const supportLabels = {
      'strongyes': 'Strong Yes',
      'leaningyes': 'Leaning Yes',
      'undecided': 'Undecided',
      'leaningno': 'Leaning No',
      'strongno': 'Strong No'
    };

    return (
      <Chip
        label={supportLabels[voter.voterSupport] || voter.voterSupport}
        size="small"
        color={supportColors[voter.voterSupport] || 'default'}
      />
    );
  };

  if (!googleMapsApiKey) {
    return (
      <Alert severity="error">
        Google Maps API key is not configured. Please add REACT_APP_GOOGLE_MAPS_API_KEY to your environment variables.
      </Alert>
    );
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ height: '100%', position: 'relative' }}>
      {/* Map Legend */}
      <Card sx={{ position: 'absolute', top: 10, left: 10, zIndex: 1, maxWidth: 250 }}>
        <CardContent sx={{ py: 1 }}>
          <Typography variant="subtitle2" gutterBottom>Map Legend</Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <img src="https://maps.google.com/mapfiles/ms/icons/green-dot.png" alt="Green" width={20} />
              <Typography variant="caption">All Contacted</Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <img src="https://maps.google.com/mapfiles/ms/icons/yellow-dot.png" alt="Yellow" width={20} />
              <Typography variant="caption">Partially Contacted</Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <img src="https://maps.google.com/mapfiles/ms/icons/red-dot.png" alt="Red" width={20} />
              <Typography variant="caption">Not Contacted</Typography>
            </Box>
            {currentLocation && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <MyLocation color="primary" fontSize="small" />
                <Typography variant="caption">Your Location</Typography>
              </Box>
            )}
          </Box>
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

      <LoadScript 
        googleMapsApiKey={googleMapsApiKey}
        onError={() => setGoogleMapsError('Failed to load Google Maps')}
      >
        <GoogleMap
          mapContainerStyle={mapContainerStyle}
          center={currentLocation ? {
            lat: currentLocation.latitude,
            lng: currentLocation.longitude
          } : defaultCenter}
          zoom={15}
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
      </LoadScript>

      {/* Contact Modal */}
      {selectedVoter && (
        <ContactModal
          open={contactModalOpen}
          onClose={() => setContactModalOpen(false)}
          voter={selectedVoter}
          onSubmit={handleContactSubmit}
        />
      )}

      {/* Error Alert */}
      {googleMapsError && (
        <Alert severity="error" sx={{ position: 'absolute', bottom: 20, left: '50%', transform: 'translateX(-50%)' }}>
          {googleMapsError}
        </Alert>
      )}
    </Box>
  );
};

export default VoterMapView;