import React, { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import 'leaflet.markercluster/dist/MarkerCluster.css';
import 'leaflet.markercluster/dist/MarkerCluster.Default.css';
import 'leaflet.markercluster';
import { Box, Paper, Typography, List, ListItem, ListItemText, Chip, IconButton, ButtonGroup, Button } from '@mui/material';
import { Home as HomeIcon, Group, Satellite, Map as MapIcon, MyLocation, Close as CloseIcon } from '@mui/icons-material';
import { AvailableHouse, OptimizedRoute, ActiveCanvasser } from '../../types/walk';

// Fix Leaflet icon issues
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: require('leaflet/dist/images/marker-icon-2x.png'),
  iconUrl: require('leaflet/dist/images/marker-icon.png'),
  shadowUrl: require('leaflet/dist/images/marker-shadow.png'),
});

interface LeafletWalkMapProps {
  currentLocation: { lat: number; lng: number } | null;
  availableHouses: AvailableHouse[];
  selectedHouses: string[];
  optimizedRoute: OptimizedRoute | null;
  currentHouseIndex: number;
  activeCanvassers: ActiveCanvasser[];
  onHouseSelect: (house: AvailableHouse) => void;
  onToggleHouseSelection: (address: string) => void;
}

const LeafletWalkMap: React.FC<LeafletWalkMapProps> = ({
  currentLocation,
  availableHouses,
  selectedHouses,
  optimizedRoute,
  currentHouseIndex,
  activeCanvassers,
  onHouseSelect,
  onToggleHouseSelection
}) => {
  const mapContainer = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const markersRef = useRef<{ [key: string]: L.Marker }>({});
  const clusterGroup = useRef<L.MarkerClusterGroup | null>(null);
  const [selectedHouse, setSelectedHouse] = useState<AvailableHouse | null>(null);
  const [mapLayer, setMapLayer] = useState<'street' | 'satellite'>('street');

  // Initialize map
  useEffect(() => {
    if (!mapContainer.current || map.current) return;

    // Initialize map with OpenStreetMap
    map.current = L.map(mapContainer.current).setView(
      currentLocation ? [currentLocation.lat, currentLocation.lng] : [33.5186, -86.8104],
      17
    );

    // Add tile layers with more detail
    const streetLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 20,
      maxNativeZoom: 19
    });

    const satelliteLayer = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
      attribution: '© Esri',
      maxZoom: 20,
      maxNativeZoom: 19
    });

    streetLayer.addTo(map.current);

    // Store layers for switching
    (map.current as any).streetLayer = streetLayer;
    (map.current as any).satelliteLayer = satelliteLayer;

    // Add zoom control
    L.control.zoom({ position: 'topright' }).addTo(map.current);

    // Initialize marker cluster group
    clusterGroup.current = L.markerClusterGroup({
      spiderfyOnMaxZoom: true,
      showCoverageOnHover: true,
      zoomToBoundsOnClick: true,
      maxClusterRadius: 50,
      iconCreateFunction: (cluster) => {
        const childCount = cluster.getChildCount();
        let c = ' marker-cluster-';
        if (childCount < 10) {
          c += 'small';
        } else if (childCount < 100) {
          c += 'medium';
        } else {
          c += 'large';
        }

        // Calculate total voters in cluster
        let totalVoters = 0;
        cluster.getAllChildMarkers().forEach(marker => {
          const house = (marker as any).house;
          if (house) {
            totalVoters += house.voterCount;
          }
        });

        return new L.DivIcon({
          html: `<div><span>${totalVoters}</span></div>`,
          className: 'marker-cluster' + c,
          iconSize: new L.Point(40, 40)
        });
      }
    });

    map.current.addLayer(clusterGroup.current);

    return () => {
      map.current?.remove();
      map.current = null;
    };
  }, [currentLocation]);

  // Switch map layers
  useEffect(() => {
    if (!map.current) return;

    const streetLayer = (map.current as any).streetLayer;
    const satelliteLayer = (map.current as any).satelliteLayer;

    if (mapLayer === 'satellite') {
      map.current.removeLayer(streetLayer);
      map.current.addLayer(satelliteLayer);
    } else {
      map.current.removeLayer(satelliteLayer);
      map.current.addLayer(streetLayer);
    }
  }, [mapLayer]);

  // Update house markers
  useEffect(() => {
    if (!map.current || !clusterGroup.current) return;

    // Clear existing markers
    clusterGroup.current.clearLayers();

    // Add house markers
    availableHouses.forEach(house => {
      const isSelected = selectedHouses.includes(house.address);
      const isInRoute = optimizedRoute?.houses.some(h => h.address === house.address);
      const routeIndex = optimizedRoute?.houses.findIndex(h => h.address === house.address);
      const isCurrent = routeIndex === currentHouseIndex;

      // Create custom icon with voter count
      const iconHtml = `
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
          ${isCurrent ? 'animation: pulse 2s infinite;' : ''}
          ${isSelected ? 'box-shadow: 0 0 0 4px #FFC107;' : ''}
        ">
          ${house.voterCount}
        </div>
      `;

      const customIcon = L.divIcon({
        html: iconHtml,
        className: 'custom-house-marker',
        iconSize: [36, 36],
        iconAnchor: [18, 18]
      });

      const marker = L.marker([house.latitude, house.longitude], { icon: customIcon });
      
      // Store house data on marker
      (marker as any).house = house;

      // Add popup with selection status
      marker.bindPopup(`
        <div style="padding: 10px; min-width: 200px;">
          <strong>${house.address}</strong><br/>
          <span style="color: #666;">${house.voterCount} voter${house.voterCount > 1 ? 's' : ''}</span><br/>
          <span style="color: #666;">${Math.round(house.distanceMeters)}m away</span>
          ${isSelected ? '<br/><span style="color: #FFC107;">✓ Selected for route</span>' : '<br/><span style="color: #888;">Click marker to select</span>'}
          ${isInRoute ? '<br/><span style="color: #4CAF50;">✓ In Route</span>' : ''}
          ${isCurrent ? '<br/><span style="color: #FF5722;">➤ Current Stop</span>' : ''}
        </div>
      `);

      // Add click handler
      marker.on('click', () => {
        setSelectedHouse(house);
        onHouseSelect(house);
      });

      clusterGroup.current!.addLayer(marker);
    });

    // Add current location marker
    if (currentLocation) {
      const locationIcon = L.divIcon({
        html: `
          <div style="
            width: 20px;
            height: 20px;
            background-color: #4CAF50;
            border: 3px solid white;
            border-radius: 50%;
            box-shadow: 0 2px 6px rgba(0,0,0,0.3);
            position: relative;
          ">
            <div style="
              position: absolute;
              width: 40px;
              height: 40px;
              border: 2px solid #4CAF50;
              border-radius: 50%;
              top: -12px;
              left: -12px;
              opacity: 0.3;
              animation: pulse 2s infinite;
            "></div>
          </div>
        `,
        className: 'current-location-marker',
        iconSize: [20, 20],
        iconAnchor: [10, 10]
      });

      const locationMarker = L.marker([currentLocation.lat, currentLocation.lng], { icon: locationIcon });
      locationMarker.bindPopup('Your current location');
      locationMarker.addTo(map.current);
    }

    // Draw route if available
    if (optimizedRoute && currentLocation) {
      const routeCoordinates: L.LatLngExpression[] = [
        [currentLocation.lat, currentLocation.lng],
        ...optimizedRoute.houses.map(h => [h.latitude, h.longitude] as L.LatLngExpression)
      ];

      const routeLine = L.polyline(routeCoordinates, {
        color: '#2196F3',
        weight: 4,
        opacity: 0.7,
        smoothFactor: 1
      });

      routeLine.addTo(map.current);
    }

    // Add CSS for pulse animation
    if (!document.getElementById('leaflet-pulse-style')) {
      const style = document.createElement('style');
      style.id = 'leaflet-pulse-style';
      style.textContent = `
        @keyframes pulse {
          0% {
            transform: scale(1);
            opacity: 1;
          }
          50% {
            transform: scale(1.2);
            opacity: 0.5;
          }
          100% {
            transform: scale(1);
            opacity: 1;
          }
        }
        
        .marker-cluster-small {
          background-color: rgba(110, 204, 57, 0.6);
        }
        .marker-cluster-small div {
          background-color: rgba(110, 204, 57, 0.8);
        }
        .marker-cluster-medium {
          background-color: rgba(240, 194, 12, 0.6);
        }
        .marker-cluster-medium div {
          background-color: rgba(240, 194, 12, 0.8);
        }
        .marker-cluster-large {
          background-color: rgba(241, 128, 23, 0.6);
        }
        .marker-cluster-large div {
          background-color: rgba(241, 128, 23, 0.8);
        }
        .marker-cluster {
          width: 40px !important;
          height: 40px !important;
          margin-left: -20px !important;
          margin-top: -20px !important;
          border-radius: 50%;
          text-align: center;
          color: white;
          font-weight: bold;
          font-size: 16px;
          line-height: 40px;
        }
        .marker-cluster div {
          width: 36px;
          height: 36px;
          margin: 2px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
        }
      `;
      document.head.appendChild(style);
    }
  }, [availableHouses, selectedHouses, optimizedRoute, currentHouseIndex, currentLocation]);

  // Center on current location
  const centerOnLocation = () => {
    if (map.current && currentLocation) {
      map.current.setView([currentLocation.lat, currentLocation.lng], 18);
    }
  };

  return (
    <Box sx={{ position: 'relative', width: '100%', height: '100%' }}>
      <Box
        ref={mapContainer}
        sx={{
          width: '100%',
          height: '100%',
          position: 'absolute',
          top: 0,
          left: 0,
          zIndex: 1
        }}
      />

      {/* Map controls */}
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
            variant={mapLayer === 'street' ? 'contained' : 'outlined'}
            onClick={() => setMapLayer('street')}
            startIcon={<MapIcon />}
          >
            Street
          </Button>
          <Button
            variant={mapLayer === 'satellite' ? 'contained' : 'outlined'}
            onClick={() => setMapLayer('satellite')}
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
            borderRadius: '50%', 
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
            borderRadius: '50%', 
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
            borderRadius: '50%', 
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
            borderRadius: '50%', 
            backgroundColor: '#FF5722',
            border: '2px solid white',
            mr: 1
          }} />
          <Typography variant="caption">Current stop</Typography>
        </Box>
      </Paper>
    </Box>
  );
};

export default LeafletWalkMap;