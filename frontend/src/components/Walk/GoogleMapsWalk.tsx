import React, { useEffect, useRef, useState, useCallback } from 'react';
import { Box, Paper, Typography, Chip, IconButton, ButtonGroup, Button } from '@mui/material';
import { Home as HomeIcon, Close as CloseIcon, Satellite, Map as MapIcon, MyLocation } from '@mui/icons-material';
import { AvailableHouse, OptimizedRoute, ActiveCanvasser } from '../../types/walk';

// You'll need to add your Google Maps API key
const GOOGLE_MAPS_API_KEY = process.env.REACT_APP_GOOGLE_MAPS_API_KEY || '';

interface GoogleMapsWalkProps {
  currentLocation: { lat: number; lng: number } | null;
  availableHouses: AvailableHouse[];
  selectedHouses: string[];
  optimizedRoute: OptimizedRoute | null;
  currentHouseIndex: number;
  activeCanvassers: ActiveCanvasser[];
  onHouseSelect: (house: AvailableHouse) => void;
  onToggleHouseSelection: (address: string) => void;
}

declare global {
  interface Window {
    google: any;
    initGoogleMaps: () => void;
  }
}

const GoogleMapsWalk: React.FC<GoogleMapsWalkProps> = ({
  currentLocation,
  availableHouses,
  selectedHouses,
  optimizedRoute,
  currentHouseIndex,
  activeCanvassers,
  onHouseSelect,
  onToggleHouseSelection
}) => {
  const mapRef = useRef<HTMLDivElement>(null);
  const map = useRef<any>(null);
  const markers = useRef<any[]>([]);
  const [mapLoaded, setMapLoaded] = useState(false);
  const [selectedHouse, setSelectedHouse] = useState<AvailableHouse | null>(null);
  const [mapType, setMapType] = useState<'roadmap' | 'satellite'>('roadmap');

  // Load Google Maps script
  useEffect(() => {
    if (!GOOGLE_MAPS_API_KEY) {
      console.error('Google Maps API key is required');
      return;
    }

    // Check if already loaded
    if (window.google && window.google.maps) {
      setMapLoaded(true);
      return;
    }

    // Create script tag
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${GOOGLE_MAPS_API_KEY}&libraries=places`;
    script.async = true;
    script.defer = true;
    
    window.initGoogleMaps = () => {
      setMapLoaded(true);
    };
    
    script.onload = () => {
      window.initGoogleMaps();
    };

    document.head.appendChild(script);

    return () => {
      if (script.parentNode) {
        script.parentNode.removeChild(script);
      }
    };
  }, []);

  // Initialize map
  useEffect(() => {
    if (!mapLoaded || !mapRef.current || map.current) return;

    const center = currentLocation || { lat: 33.5186, lng: -86.8104 };
    
    map.current = new window.google.maps.Map(mapRef.current, {
      center,
      zoom: 18,
      mapTypeId: 'roadmap',
      streetViewControl: true,
      fullscreenControl: true,
      mapTypeControl: false, // We'll use our own
      styles: [
        {
          featureType: "poi",
          elementType: "labels",
          stylers: [{ visibility: "off" }]
        }
      ]
    });

    // Add click listener to close popups
    map.current.addListener('click', () => {
      setSelectedHouse(null);
    });
  }, [mapLoaded, currentLocation]);

  // Update markers
  useEffect(() => {
    if (!map.current || !mapLoaded) return;

    // Clear existing markers
    markers.current.forEach(marker => {
      if (marker.setMap) {
        marker.setMap(null);
      }
    });
    markers.current = [];

    // Add house markers
    availableHouses.forEach(house => {
      const isSelected = selectedHouses.includes(house.address);
      const isInRoute = optimizedRoute?.houses.some(h => h.address === house.address);
      const routeIndex = optimizedRoute?.houses.findIndex(h => h.address === house.address);
      const isCurrent = routeIndex === currentHouseIndex;

      // Create custom marker using Google's marker customization
      const markerDiv = document.createElement('div');
      markerDiv.innerHTML = `
        <div style="
          width: 36px;
          height: 36px;
          background-color: ${isCurrent ? '#FF5722' : isInRoute ? '#4CAF50' : isSelected ? '#FFC107' : '#2196F3'};
          border: 3px solid white;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          color: white;
          font-weight: bold;
          font-size: 16px;
          box-shadow: 0 2px 8px rgba(0,0,0,0.4);
          cursor: pointer;
          position: relative;
          ${isCurrent ? 'animation: pulse 2s infinite;' : ''}
          ${isSelected ? 'box-shadow: 0 0 0 4px #FFC107;' : ''}
        ">
          ${house.voterCount}
        </div>
      `;

      // Create a custom overlay for the marker
      const overlay = new window.google.maps.OverlayView();
      overlay.onAdd = function() {
        const panes = this.getPanes();
        panes.overlayMouseTarget.appendChild(markerDiv);
      };
      overlay.draw = function() {
        const projection = this.getProjection();
        const position = projection.fromLatLngToDivPixel(
          new window.google.maps.LatLng(house.latitude, house.longitude)
        );
        if (position) {
          markerDiv.style.position = 'absolute';
          markerDiv.style.left = (position.x - 18) + 'px';
          markerDiv.style.top = (position.y - 18) + 'px';
        }
      };
      overlay.onRemove = function() {
        if (markerDiv.parentNode) {
          markerDiv.parentNode.removeChild(markerDiv);
        }
      };
      overlay.setMap(map.current);
      
      // Add click listener to the div
      markerDiv.addEventListener('click', () => {
        setSelectedHouse(house);
      });

      markers.current.push(overlay);
    });

    // Add current location marker
    if (currentLocation) {
      const locationDiv = document.createElement('div');
      locationDiv.innerHTML = `
        <div style="
          width: 20px;
          height: 20px;
          background: #4CAF50;
          border: 3px solid white;
          border-radius: 50%;
          box-shadow: 0 2px 6px rgba(0,0,0,0.3);
        "></div>
      `;

      const locationOverlay = new window.google.maps.OverlayView();
      locationOverlay.onAdd = function() {
        const panes = this.getPanes();
        panes.overlayMouseTarget.appendChild(locationDiv);
      };
      locationOverlay.draw = function() {
        const projection = this.getProjection();
        const position = projection.fromLatLngToDivPixel(
          new window.google.maps.LatLng(currentLocation.lat, currentLocation.lng)
        );
        if (position) {
          locationDiv.style.position = 'absolute';
          locationDiv.style.left = (position.x - 10) + 'px';
          locationDiv.style.top = (position.y - 10) + 'px';
        }
      };
      locationOverlay.onRemove = function() {
        if (locationDiv.parentNode) {
          locationDiv.parentNode.removeChild(locationDiv);
        }
      };
      locationOverlay.setMap(map.current);

      markers.current.push(locationOverlay);
    }

    // Draw route if available
    if (optimizedRoute && currentLocation) {
      const routePath = [
        currentLocation,
        ...optimizedRoute.houses.map(h => ({ lat: h.latitude, lng: h.longitude }))
      ];

      const routeLine = new window.google.maps.Polyline({
        path: routePath,
        geodesic: true,
        strokeColor: '#2196F3',
        strokeOpacity: 0.8,
        strokeWeight: 4,
        map: map.current
      });
    }
  }, [availableHouses, selectedHouses, optimizedRoute, currentHouseIndex, currentLocation, mapLoaded]);

  // Update map type
  useEffect(() => {
    if (map.current) {
      map.current.setMapTypeId(mapType);
    }
  }, [mapType]);

  // Center on location
  const centerOnLocation = () => {
    if (map.current && currentLocation) {
      map.current.panTo(currentLocation);
      map.current.setZoom(18);
    }
  };

  if (!GOOGLE_MAPS_API_KEY) {
    return (
      <Box sx={{ 
        height: '100%', 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        backgroundColor: '#f5f5f5'
      }}>
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography variant="h6" gutterBottom>
            Google Maps API Key Required
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Please add REACT_APP_GOOGLE_MAPS_API_KEY to your .env file
          </Typography>
        </Paper>
      </Box>
    );
  }

  return (
    <Box sx={{ position: 'relative', width: '100%', height: '100%' }}>
      <Box 
        ref={mapRef} 
        sx={{ 
          width: '100%', 
          height: '100%',
          position: 'absolute',
          top: 0,
          left: 0
        }} 
      />

      {/* Map type controls */}
      <Paper
        sx={{
          position: 'absolute',
          top: 16,
          left: 16,
          zIndex: 1000,
          p: 1
        }}
      >
        <ButtonGroup size="small">
          <Button
            variant={mapType === 'roadmap' ? 'contained' : 'outlined'}
            onClick={() => setMapType('roadmap')}
            startIcon={<MapIcon />}
          >
            Street
          </Button>
          <Button
            variant={mapType === 'satellite' ? 'contained' : 'outlined'}
            onClick={() => setMapType('satellite')}
            startIcon={<Satellite />}
          >
            Satellite
          </Button>
        </ButtonGroup>
      </Paper>

      {/* Center on location button */}
      {currentLocation && (
        <Paper
          sx={{
            position: 'absolute',
            top: 80,
            left: 16,
            zIndex: 1000,
            p: 0
          }}
        >
          <IconButton onClick={centerOnLocation} color="primary">
            <MyLocation />
          </IconButton>
        </Paper>
      )}

      {/* Selected house details */}
      {selectedHouse && (
        <Paper
          sx={{
            position: 'absolute',
            right: 16,
            top: 16,
            width: 320,
            maxHeight: '60vh',
            overflow: 'auto',
            p: 2,
            boxShadow: 3,
            zIndex: 1000
          }}
        >
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
            <Typography variant="h6">
              <HomeIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
              House Details
            </Typography>
            <IconButton 
              size="small" 
              onClick={() => setSelectedHouse(null)}
              sx={{ ml: 1 }}
            >
              <CloseIcon />
            </IconButton>
          </Box>
          
          <Typography variant="body2" gutterBottom>
            {selectedHouse.address}
          </Typography>
          
          <Box sx={{ mb: 2 }}>
            <Chip 
              label={`${selectedHouse.voterCount} voter${selectedHouse.voterCount > 1 ? 's' : ''}`} 
              size="small" 
              color="primary" 
              sx={{ mr: 1 }}
            />
            <Chip 
              label={`${Math.round(selectedHouse.distanceMeters)}m away`} 
              size="small" 
            />
          </Box>

          <Typography variant="body2" color="text.secondary">
            Latitude: {selectedHouse.latitude.toFixed(6)}<br/>
            Longitude: {selectedHouse.longitude.toFixed(6)}
          </Typography>

          <Box sx={{ mt: 2 }}>
            <Button
              variant="contained"
              fullWidth
              onClick={() => {
                onHouseSelect(selectedHouse);
                setSelectedHouse(null);
              }}
            >
              Select for Route
            </Button>
          </Box>
        </Paper>
      )}

      {/* Map legend */}
      <Paper
        sx={{
          position: 'absolute',
          left: 16,
          bottom: 16,
          p: 2,
          minWidth: 200,
          zIndex: 1000
        }}
      >
        <Typography variant="subtitle2" gutterBottom>
          Map Legend
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
          <Box sx={{ 
            width: 20, 
            height: 20, 
            borderRadius: '8px', 
            backgroundColor: '#2196F3',
            border: '2px solid white',
            mr: 1
          }} />
          <Typography variant="caption">Available houses</Typography>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
          <Box sx={{ 
            width: 20, 
            height: 20, 
            borderRadius: '8px', 
            backgroundColor: '#FFC107',
            border: '2px solid white',
            mr: 1
          }} />
          <Typography variant="caption">Selected houses</Typography>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
          <Box sx={{ 
            width: 20, 
            height: 20, 
            borderRadius: '8px', 
            backgroundColor: '#4CAF50',
            border: '2px solid white',
            mr: 1
          }} />
          <Typography variant="caption">In route / Your location</Typography>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center' }}>
          <Box sx={{ 
            width: 20, 
            height: 20, 
            borderRadius: '8px', 
            backgroundColor: '#FF5722',
            border: '2px solid white',
            mr: 1
          }} />
          <Typography variant="caption">Current stop</Typography>
        </Box>
      </Paper>

      {/* Add CSS for animations */}
      <style>{`
        @keyframes pulse {
          0% {
            transform: scale(1);
            opacity: 1;
          }
          50% {
            transform: scale(1.1);
            opacity: 0.8;
          }
          100% {
            transform: scale(1);
            opacity: 1;
          }
        }
      `}</style>
    </Box>
  );
};

export default GoogleMapsWalk;