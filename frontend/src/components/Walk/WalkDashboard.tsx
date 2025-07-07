import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Typography,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  CircularProgress,
  Alert,
  Fab,
  Badge,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  LinearProgress,
  Avatar,
  Divider,
  Paper,
  useTheme,
  useMediaQuery
} from '@mui/material';
import {
  PlayArrow,
  Pause,
  Stop,
  Home as HomeIcon,
  DirectionsWalk,
  Map as MapIcon,
  List as ListIcon,
  MyLocation,
  Navigation,
  Groups,
  CheckCircle,
  Cancel,
  Schedule,
  Route,
  LocationOn,
  Person,
  Phone,
  Email,
  HowToVote
} from '@mui/icons-material';
import { WalkSession, AvailableHouse, HouseClaim, OptimizedRoute, ActiveCanvasser } from '../../types/walk';
import { walkHubService, WalkHubUpdate } from '../../services/walkHubService';
import { API_BASE_URL } from '../../config';
import MapComparison from './MapComparison';
import HouseList from './HouseList';
import HouseDetailsCard from './HouseDetailsCard';
import ActiveCanvassersList from './ActiveCanvassersList';
import VoterContactModal from './VoterContactModal';

interface WalkDashboardProps {
  user: any; // TODO: Use proper user type
}

const WalkDashboard: React.FC<WalkDashboardProps> = ({ user }) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  
  // State
  const [session, setSession] = useState<WalkSession | null>(null);
  const [availableHouses, setAvailableHouses] = useState<AvailableHouse[]>([]);
  const [selectedHouses, setSelectedHouses] = useState<string[]>([]);
  const [optimizedRoute, setOptimizedRoute] = useState<OptimizedRoute | null>(null);
  const [currentHouseIndex, setCurrentHouseIndex] = useState(0);
  const [activeCanvassers, setActiveCanvassers] = useState<ActiveCanvasser[]>([]);
  const [currentLocation, setCurrentLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [viewMode, setViewMode] = useState<'map' | 'list'>('map');
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selectedHouse, setSelectedHouse] = useState<AvailableHouse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [gpsTracking, setGpsTracking] = useState(false);
  const [signalRConnected, setSignalRConnected] = useState(false);
  const [voterModalOpen, setVoterModalOpen] = useState(false);
  const [selectedHouseForContact, setSelectedHouseForContact] = useState<AvailableHouse | null>(null);

  // SignalR setup
  useEffect(() => {
    walkHubService.setCallbacks({
      onHouseStatusUpdate: (update: WalkHubUpdate) => {
        console.log('House status update received:', update);
        if (update.type === 'HouseClaimed' && update.houseId && update.address) {
          // Remove house from available houses if claimed by someone else
          if (update.volunteerId !== parseInt(user.volunteerId)) {
            setAvailableHouses(prev => prev.filter(h => h.address !== update.address));
          }
        } else if (update.type === 'HouseReleased' && update.address) {
          // Could refresh available houses when a house is released
          loadAvailableHouses();
        }
      },
      onCanvasserLocationUpdate: (update: WalkHubUpdate) => {
        if (update.volunteerId && update.latitude && update.longitude) {
          setActiveCanvassers(prev => 
            prev.map(c => 
              c.volunteerId === update.volunteerId 
                ? { ...c, latitude: update.latitude!, longitude: update.longitude! }
                : c
            )
          );
        }
      },
      onCanvasserUpdate: (update: WalkHubUpdate) => {
        if (update.type === 'CanvasserJoined' && update.volunteerId && update.volunteerName) {
          // Refresh nearby canvassers list
          if (currentLocation) {
            walkHubService.getNearbyCanvassers(currentLocation.lat, currentLocation.lng);
          }
        } else if (update.type === 'CanvasserLeft' && update.volunteerId) {
          setActiveCanvassers(prev => prev.filter(c => c.volunteerId !== update.volunteerId));
        }
      },
      onNearbyCanvassers: (canvassers) => {
        setActiveCanvassers(canvassers);
      },
      onConnectionStateChange: (connected) => {
        setSignalRConnected(connected);
      }
    });

    // Connect to SignalR when component mounts
    walkHubService.connect().catch(console.error);

    return () => {
      walkHubService.disconnect();
    };
  }, [user.volunteerId]);

  // Get current location
  useEffect(() => {
    // For testing: Use Birmingham, AL coordinates if not in Birmingham area
    const birminghamCenter = { lat: 33.5186, lng: -86.8104 };
    
    // Immediately set Birmingham location for testing
    console.log('WalkDashboard: Setting initial Birmingham location');
    setCurrentLocation(birminghamCenter);
    
    if (navigator.geolocation) {
      const watchId = navigator.geolocation.watchPosition(
        (position) => {
          const newLocation = {
            lat: position.coords.latitude,
            lng: position.coords.longitude
          };
          
          // Check if location is in Birmingham area (rough approximation)
          const isInBirmingham = 
            newLocation.lat >= 33.2 && newLocation.lat <= 33.7 &&
            newLocation.lng >= -87.0 && newLocation.lng <= -86.5;
          
          if (!isInBirmingham) {
            console.log('Not in Birmingham area, keeping Birmingham center for testing');
            // Already set above, no need to set again
          } else {
            console.log('In Birmingham area, using actual location:', newLocation);
            setCurrentLocation(newLocation);
          }
          
          // Update location via SignalR if connected and session is active
          if (signalRConnected && session?.status === 'active') {
            walkHubService.updateLocation(newLocation.lat, newLocation.lng).catch(console.error);
          }
        },
        (error) => {
          console.error('Error getting location:', error);
          console.log('Using Birmingham center for testing due to location error');
          setCurrentLocation(birminghamCenter);
          setError('Using Birmingham, AL location for testing');
        },
        {
          enableHighAccuracy: true,
          timeout: 5000,
          maximumAge: 0
        }
      );

      return () => navigator.geolocation.clearWatch(watchId);
    }
  }, []);

  // Load active session on mount
  useEffect(() => {
    console.log('Loading current session on mount...');
    loadCurrentSession();
  }, []);

  // Load available houses when location changes
  useEffect(() => {
    if (currentLocation && session?.status === 'active') {
      loadAvailableHouses();
    }
  }, [currentLocation, session]);

  const loadCurrentSession = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/walk/sessions/current`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        console.log('Session loaded:', data);
        setSession(data);
        // Load houses immediately after session is restored
        if (data && currentLocation) {
          loadAvailableHouses();
        }
      }
    } catch (error) {
      console.error('Error loading session:', error);
    }
  };

  const loadAvailableHouses = async () => {
    if (!currentLocation) {
      console.log('loadAvailableHouses: No current location yet');
      return;
    }

    console.log('Loading available houses for location:', currentLocation);
    
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/walk/houses/available?latitude=${currentLocation.lat}&longitude=${currentLocation.lng}&radiusKm=0.5&limit=50`,
        {
          headers: {
            'Authorization': `Bearer ${user.token}`
          }
        }
      );

      if (response.ok) {
        const data: AvailableHouse[] = await response.json();
        console.log('Available houses loaded:', data.length, 'houses');
        // Log first few houses to check data
        console.log('First 5 houses:', data.slice(0, 5));
        // Check for duplicate addresses
        const uniqueAddresses = new Set(data.map((h: AvailableHouse) => h.address));
        console.log(`Unique addresses: ${uniqueAddresses.size} out of ${data.length} houses`);
        setAvailableHouses(data);
      } else {
        console.error('Failed to load houses:', response.status, response.statusText);
      }
    } catch (error) {
      console.error('Error loading available houses:', error);
    }
  };

  const startSession = useCallback(async () => {
    try {
      if (!currentLocation) {
        setError('Please enable location services to start walking');
        return;
      }

      if (!user?.token) {
        setError('Authentication required');
        return;
      }

      setLoading(true);
      setError(null);
      const response = await fetch(`${API_BASE_URL}/api/walk/sessions/start`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          latitude: currentLocation.lat,
          longitude: currentLocation.lng
        })
      });

      if (response.ok) {
        const data = await response.json();
        setSession(data);
        setGpsTracking(true);
        await loadAvailableHouses();
        
        // Join SignalR walk session
        if (signalRConnected && currentLocation) {
          await walkHubService.joinWalkSession(currentLocation.lat, currentLocation.lng);
          await walkHubService.getNearbyCanvassers(currentLocation.lat, currentLocation.lng);
        }
      } else {
        const error = await response.json();
        setError(error.error || 'Failed to start session');
      }
    } catch (error) {
      console.error('Error starting session:', error);
      setError('Network error occurred');
    } finally {
      setLoading(false);
    }
  }, [currentLocation, user?.token, signalRConnected]);

  const endSession = useCallback(async () => {
    try {
      if (!currentLocation || !session) return;
      if (!user?.token) {
        setError('Authentication required');
        return;
      }

      setLoading(true);
      setError(null);
      const response = await fetch(`${API_BASE_URL}/api/walk/sessions/end`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          latitude: currentLocation.lat,
          longitude: currentLocation.lng
        })
      });

      if (response.ok) {
        setSession(null);
        setOptimizedRoute(null);
        setSelectedHouses([]);
        setGpsTracking(false);
        
        // Leave SignalR walk session
        if (signalRConnected) {
          await walkHubService.leaveWalkSession();
        }
      }
    } catch (error) {
      console.error('Error ending session:', error);
      setError('Failed to end session');
    } finally {
      setLoading(false);
    }
  }, [currentLocation, session, user?.token, signalRConnected]);

  const generateRoute = useCallback(async () => {
    try {
      if (!currentLocation || selectedHouses.length === 0) return;
      if (!user?.token) {
        setError('Authentication required');
        return;
      }

      setLoading(true);
      setError(null);
      const response = await fetch(`${API_BASE_URL}/api/walk/routes/optimize`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          startLatitude: currentLocation.lat,
          startLongitude: currentLocation.lng,
          addresses: selectedHouses
        })
      });

      if (response.ok) {
        const route = await response.json();
        setOptimizedRoute(route);
        
        // Claim the houses in the route
        await claimHouses(selectedHouses);
      }
    } catch (error) {
      console.error('Error generating route:', error);
      setError('Failed to generate route');
    } finally {
      setLoading(false);
    }
  }, [currentLocation, selectedHouses, user?.token]);

  const claimHouses = async (addresses: string[]) => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/walk/houses/claim`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          addresses,
          claimDurationMinutes: 45
        })
      });

      if (response.ok) {
        const claims = await response.json();
        // Update session with new claims
        if (session) {
          setSession({
            ...session,
            activeClaims: claims
          });
        }
      }
    } catch (error) {
      console.error('Failed to claim houses:', error);
    }
  };

  const arriveAtHouse = async (claimId: number) => {
    if (!currentLocation) return;

    try {
      await fetch(`${API_BASE_URL}/api/walk/houses/${claimId}/arrive`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          latitude: currentLocation.lat,
          longitude: currentLocation.lng
        })
      });
    } catch (error) {
      console.error('Failed to mark arrival:', error);
    }
  };

  const completeHouseVisit = async (claimId: number, votersContacted: number, votersHome: number, contactIds: string[]) => {
    if (!currentLocation) return;

    try {
      await fetch(`${API_BASE_URL}/api/walk/houses/${claimId}/complete`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          latitude: currentLocation.lat,
          longitude: currentLocation.lng,
          votersContacted,
          votersHome,
          contactIds
        })
      });

      // Move to next house
      if (currentHouseIndex < (optimizedRoute?.houses.length || 0) - 1) {
        setCurrentHouseIndex(currentHouseIndex + 1);
      }
    } catch (error) {
      console.error('Failed to complete visit:', error);
    }
  };

  const formatDuration = (minutes: number) => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
  };

  const formatDistance = (meters: number) => {
    if (meters < 1000) {
      return `${Math.round(meters)}m`;
    }
    return `${(meters / 1000).toFixed(1)}km`;
  };

  // Validate user prop after all hooks - temporarily disabled for testing
  console.log('WalkDashboard user object:', user);
  console.log('WalkDashboard rendering - validation bypassed for testing');
  // Temporarily bypass all validation
  // if (!user) {
  //   return (
  //     <Box sx={{ p: 3, textAlign: 'center' }}>
  //       <CircularProgress />
  //       <Typography variant="h6" sx={{ mt: 2 }}>
  //         Loading user information...
  //       </Typography>
  //     </Box>
  //   );
  // }

  return (
    <Box sx={{ height: '100vh', display: 'flex', flexDirection: 'column' }}>
      {/* Header */}
      <Box sx={{ 
        p: 2, 
        bgcolor: 'background.paper', 
        borderBottom: 1, 
        borderColor: 'divider',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center'
      }}>
        <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <DirectionsWalk />
          Canvassing Walk
        </Typography>

        {session && (
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <Chip
              icon={<HomeIcon />}
              label={`${session.housesVisited || 0} visited`}
              color="primary"
              variant="outlined"
              onClick={() => {}} // Prevent undefined onClick
            />
            <Chip
              icon={<Person />}
              label={`${session.votersContacted || 0} contacted`}
              color="success"
              variant="outlined"
              onClick={() => {}} // Prevent undefined onClick
            />
            <Chip
              icon={<Route />}
              label={formatDistance(session.totalDistanceMeters || 0)}
              variant="outlined"
              onClick={() => {}} // Prevent undefined onClick
            />
            <Chip
              icon={<Schedule />}
              label={formatDuration(session.durationMinutes || 0)}
              variant="outlined"
              onClick={() => {}} // Prevent undefined onClick
            />
          </Box>
        )}

        <Box sx={{ display: 'flex', gap: 1 }}>
          <IconButton 
            onClick={() => {
              try {
                setViewMode(viewMode === 'map' ? 'list' : 'map');
              } catch (error) {
                console.error('Error toggling view mode:', error);
              }
            }}
          >
            {viewMode === 'map' ? <ListIcon /> : <MapIcon />}
          </IconButton>
          <IconButton 
            onClick={() => {
              try {
                setDrawerOpen(true);
              } catch (error) {
                console.error('Error opening drawer:', error);
              }
            }}
          >
            <Groups />
          </IconButton>
        </Box>
      </Box>

      {/* Main Content */}
      <Box sx={{ flex: 1, position: 'relative', overflow: 'hidden' }}>
        {!session ? (
          <Box sx={{ 
            height: '100%', 
            display: 'flex', 
            alignItems: 'center', 
            justifyContent: 'center',
            flexDirection: 'column',
            gap: 3
          }}>
            <Typography variant="h5" color="text.secondary">
              Ready to start canvassing?
            </Typography>
            <Button
              variant="contained"
              size="large"
              startIcon={<PlayArrow />}
              onClick={startSession}
              disabled={loading || !currentLocation}
            >
              Start Walking Session
            </Button>
            {!currentLocation && (
              <Alert severity="warning">
                Please enable location services to start
              </Alert>
            )}
          </Box>
        ) : viewMode === 'map' ? (
          <MapComparison
            currentLocation={currentLocation}
            availableHouses={availableHouses}
            selectedHouses={selectedHouses}
            optimizedRoute={optimizedRoute}
            currentHouseIndex={currentHouseIndex}
            activeCanvassers={activeCanvassers}
            onHouseSelect={(house) => {
              // Toggle house selection for route planning
              setSelectedHouses(prev =>
                prev.includes(house.address)
                  ? prev.filter(a => a !== house.address)
                  : [...prev, house.address]
              );
            }}
            onToggleHouseSelection={(address) => {
              setSelectedHouses(prev =>
                prev.includes(address)
                  ? prev.filter(a => a !== address)
                  : [...prev, address]
              );
            }}
          />
        ) : (
          <HouseList
            houses={availableHouses}
            selectedHouses={selectedHouses}
            optimizedRoute={optimizedRoute}
            currentHouseIndex={currentHouseIndex}
            onHouseSelect={(house) => setSelectedHouse(house)}
            onToggleSelection={(address) => {
              setSelectedHouses(prev =>
                prev.includes(address)
                  ? prev.filter(a => a !== address)
                  : [...prev, address]
              );
            }}
          />
        )}

        {/* Instructions Panel */}
        {session && selectedHouses.length === 0 && !optimizedRoute && (
          <Paper sx={{
            position: 'absolute',
            bottom: 100,
            left: 16,
            p: 1.5,
            backgroundColor: 'rgba(255, 255, 255, 0.95)',
            maxWidth: 320,
            zIndex: 1000
          }}>
            <Typography variant="subtitle2" gutterBottom sx={{ display: 'flex', alignItems: 'center' }}>
              <Route sx={{ fontSize: 18, mr: 0.5 }} />
              How to Create a Walking Route
            </Typography>
            <Typography variant="caption" color="text.secondary" component="div">
              1. Click on house markers to select them<br/>
              2. Selected houses will show with a yellow ring<br/>
              3. Click "Generate Route" button when ready<br/>
              4. The app will optimize your walking path
            </Typography>
          </Paper>
        )}

        {/* Action Buttons */}
        {session && (
          <Box sx={{ 
            position: 'absolute', 
            bottom: 16, 
            right: 16, 
            display: 'flex', 
            flexDirection: 'column', 
            gap: 2 
          }}>
            {selectedHouses.length > 0 && !optimizedRoute && (
              <Fab
                color="primary"
                variant="extended"
                onClick={generateRoute}
                disabled={loading}
              >
                <Route sx={{ mr: 1 }} />
                Generate Route ({selectedHouses.length})
              </Fab>
            )}

            {optimizedRoute && (
              <>
                <Fab
                  color="secondary"
                  variant="extended"
                  onClick={() => {
                    const currentHouse = optimizedRoute.houses[currentHouseIndex];
                    if (currentHouse) {
                      // Navigate to current house
                      window.open(
                        `https://www.google.com/maps/dir/?api=1&destination=${currentHouse.latitude},${currentHouse.longitude}`,
                        '_blank'
                      );
                    }
                  }}
                >
                  <Navigation sx={{ mr: 1 }} />
                  Navigate
                </Fab>
                
                <Fab
                  color="success"
                  variant="extended"
                  onClick={() => {
                    const currentHouse = optimizedRoute.houses[currentHouseIndex];
                    if (currentHouse) {
                      // Find the full house data from availableHouses
                      const fullHouse = availableHouses.find(h => h.address === currentHouse.address);
                      if (fullHouse) {
                        setSelectedHouseForContact(fullHouse);
                        setVoterModalOpen(true);
                      }
                    }
                  }}
                >
                  <Groups sx={{ mr: 1 }} />
                  View Voters
                </Fab>
              </>
            )}

            <Fab
              color="error"
              onClick={endSession}
              disabled={loading}
            >
              <Stop />
            </Fab>
          </Box>
        )}
      </Box>

      {/* House Details Modal */}
      {selectedHouse && (
        <HouseDetailsCard
          house={selectedHouse}
          session={session}
          onClose={() => setSelectedHouse(null)}
          onArrival={arriveAtHouse}
          onComplete={completeHouseVisit}
        />
      )}

      {/* Active Canvassers Drawer */}
      <Drawer
        anchor="right"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
      >
        <ActiveCanvassersList
          canvassers={activeCanvassers}
          currentLocation={currentLocation}
        />
      </Drawer>

      {/* Connection Status */}
      <Box sx={{ position: 'fixed', top: 8, right: 8, zIndex: 1200 }}>
        <Chip
          icon={signalRConnected ? <CheckCircle /> : <Cancel />}
          label={signalRConnected ? "Live Updates Connected" : "Offline Mode"}
          color={signalRConnected ? "success" : "default"}
          size="small"
          variant="filled"
        />
      </Box>

      {/* Error Snackbar */}
      {error && (
        <Alert 
          severity="error" 
          onClose={() => setError(null)}
          sx={{ position: 'fixed', top: 80, left: '50%', transform: 'translateX(-50%)' }}
        >
          {error}
        </Alert>
      )}

      {/* Voter Contact Modal */}
      <VoterContactModal
        open={voterModalOpen}
        house={selectedHouseForContact}
        onClose={() => {
          setVoterModalOpen(false);
          setSelectedHouseForContact(null);
        }}
        onContactUpdate={() => {
          loadAvailableHouses();
          setVoterModalOpen(false);
          setSelectedHouseForContact(null);
        }}
        token={user?.token || ''}
      />
    </Box>
  );
};

export default WalkDashboard;