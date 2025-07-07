import React, { useEffect, useRef, useState } from 'react';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import './ImprovedWalkMap.css';
import { Box, Paper, Typography, Chip, List, ListItem, ListItemText, ListItemButton, IconButton, Tooltip } from '@mui/material';
import { Home as HomeIcon, LocationOn, Group, Navigation } from '@mui/icons-material';
import { AvailableHouse, OptimizedRoute, ActiveCanvasser } from '../../types/walk';

const MAPBOX_TOKEN = process.env.REACT_APP_MAPBOX_TOKEN || 'pk.eyJ1IjoibWFxYm9vbHAiLCJhIjoiY21jcnlkZHRoMHJwMTJrcTc0OW53YjI4ayJ9.Z-OEVxJnAN8QmLeN57yqJg';
mapboxgl.accessToken = MAPBOX_TOKEN;

interface ImprovedWalkMapProps {
  currentLocation: { lat: number; lng: number } | null;
  availableHouses: AvailableHouse[];
  selectedHouses: string[];
  optimizedRoute: OptimizedRoute | null;
  currentHouseIndex: number;
  activeCanvassers: ActiveCanvasser[];
  onHouseSelect: (house: AvailableHouse) => void;
  onToggleHouseSelection: (address: string) => void;
}

interface HouseCluster {
  id: string;
  center: { lat: number; lng: number };
  houses: AvailableHouse[];
  totalVoters: number;
  streetName: string;
}

const ImprovedWalkMap: React.FC<ImprovedWalkMapProps> = ({
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
  const map = useRef<mapboxgl.Map | null>(null);
  const [mapLoaded, setMapLoaded] = useState(false);
  const [houseClusters, setHouseClusters] = useState<HouseCluster[]>([]);
  const [selectedCluster, setSelectedCluster] = useState<HouseCluster | null>(null);
  const markersRef = useRef<{ [key: string]: mapboxgl.Marker }>({});

  // Group houses by street for better visualization
  useEffect(() => {
    const clusters = groupHousesByStreet(availableHouses);
    setHouseClusters(clusters);
  }, [availableHouses]);

  // Initialize map with satellite view option
  useEffect(() => {
    if (map.current) return;
    if (!mapContainer.current) return;

    map.current = new mapboxgl.Map({
      container: mapContainer.current,
      style: 'mapbox://styles/mapbox/streets-v12',
      center: currentLocation ? [currentLocation.lng, currentLocation.lat] : [-86.8104, 33.5186],
      zoom: 16,
      pitch: 0,
      bearing: 0
    });

    map.current.on('load', () => {
      setMapLoaded(true);

      // Add navigation controls
      map.current!.addControl(new mapboxgl.NavigationControl(), 'top-right');

      // Add geolocate control
      const geolocate = new mapboxgl.GeolocateControl({
        positionOptions: {
          enableHighAccuracy: true
        },
        trackUserLocation: true,
        showUserHeading: true
      });
      map.current!.addControl(geolocate, 'top-right');

      // Address search functionality removed - MapboxGeocoder package not available

      // Add style switcher
      const styleControl = document.createElement('div');
      styleControl.className = 'mapboxgl-ctrl mapboxgl-ctrl-group';
      styleControl.innerHTML = `
        <button id="streets-view" class="mapboxgl-ctrl-icon" title="Streets view" style="background: white; width: 30px; height: 30px;">
          <span style="font-size: 16px;">🗺️</span>
        </button>
        <button id="satellite-view" class="mapboxgl-ctrl-icon" title="Satellite view" style="background: white; width: 30px; height: 30px;">
          <span style="font-size: 16px;">🛰️</span>
        </button>
      `;
      
      const controlContainer = document.querySelector('.mapboxgl-ctrl-top-right');
      if (controlContainer) {
        controlContainer.appendChild(styleControl);
      }

      // Style switcher event handlers
      document.getElementById('streets-view')?.addEventListener('click', () => {
        map.current?.setStyle('mapbox://styles/mapbox/streets-v12');
      });
      
      document.getElementById('satellite-view')?.addEventListener('click', () => {
        map.current?.setStyle('mapbox://styles/mapbox/satellite-streets-v12');
      });
    });

    return () => {
      map.current?.remove();
      map.current = null;
    };
  }, [currentLocation]);

  // Update cluster markers
  useEffect(() => {
    if (!map.current || !mapLoaded) return;

    // Remove old markers
    Object.values(markersRef.current).forEach(marker => marker.remove());
    markersRef.current = {};

    // Add cluster markers
    houseClusters.forEach(cluster => {
      const el = document.createElement('div');
      el.className = 'cluster-marker';
      el.style.cssText = `
        width: 50px;
        height: 50px;
        background: #2196F3;
        border: 3px solid white;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        color: white;
        font-weight: bold;
        font-size: 16px;
        cursor: pointer;
        box-shadow: 0 2px 10px rgba(0,0,0,0.3);
        position: relative;
        transform: translate(-50%, -50%);
        pointer-events: auto;
      `;
      
      el.textContent = cluster.totalVoters.toString();
      
      el.addEventListener('mouseenter', () => {
        el.style.background = '#1976D2';
        el.style.boxShadow = '0 4px 15px rgba(0,0,0,0.4)';
      });
      
      el.addEventListener('mouseleave', () => {
        el.style.background = '#2196F3';
        el.style.boxShadow = '0 2px 10px rgba(0,0,0,0.3)';
      });

      el.addEventListener('click', (e) => {
        e.stopPropagation();
        setSelectedCluster(cluster);
        map.current?.flyTo({
          center: [cluster.center.lng, cluster.center.lat],
          zoom: 18
        });
      });

      const popup = new mapboxgl.Popup({ 
        offset: 25,
        closeButton: false 
      }).setHTML(`
        <div style="padding: 10px;">
          <strong>${cluster.streetName}</strong><br/>
          ${cluster.houses.length} houses<br/>
          ${cluster.totalVoters} total voters
        </div>
      `);

      const marker = new mapboxgl.Marker({
        element: el,
        anchor: 'center'
      })
        .setLngLat([cluster.center.lng, cluster.center.lat])
        .setPopup(popup)
        .addTo(map.current!);

      markersRef.current[cluster.id] = marker;
    });

    // Add current location marker
    if (currentLocation) {
      const locationEl = document.createElement('div');
      locationEl.style.cssText = `
        width: 20px;
        height: 20px;
        background: #4CAF50;
        border: 3px solid white;
        border-radius: 50%;
        box-shadow: 0 2px 6px rgba(0,0,0,0.3);
      `;

      const locationMarker = new mapboxgl.Marker(locationEl)
        .setLngLat([currentLocation.lng, currentLocation.lat])
        .addTo(map.current!);

      markersRef.current['currentLocation'] = locationMarker;
    }
  }, [houseClusters, mapLoaded, currentLocation]);

  // Helper function to group houses by street
  function groupHousesByStreet(houses: AvailableHouse[]): HouseCluster[] {
    const streetGroups = new Map<string, AvailableHouse[]>();

    houses.forEach(house => {
      // Extract street name from address
      const streetMatch = house.address.match(/^\d+\s+(.+?)(?:,|$)/);
      const streetName = streetMatch ? streetMatch[1] : 'Unknown Street';
      
      if (!streetGroups.has(streetName)) {
        streetGroups.set(streetName, []);
      }
      streetGroups.get(streetName)!.push(house);
    });

    // Create clusters from street groups
    const clusters: HouseCluster[] = [];
    streetGroups.forEach((housesOnStreet, streetName) => {
      if (housesOnStreet.length > 0) {
        // Calculate center using median coordinates
        const lats = housesOnStreet.map(h => h.latitude).sort((a, b) => a - b);
        const lngs = housesOnStreet.map(h => h.longitude).sort((a, b) => a - b);
        const medianLat = lats[Math.floor(lats.length / 2)];
        const medianLng = lngs[Math.floor(lngs.length / 2)];

        clusters.push({
          id: `cluster-${streetName.replace(/\s+/g, '-')}`,
          center: { lat: medianLat, lng: medianLng },
          houses: housesOnStreet,
          totalVoters: housesOnStreet.reduce((sum, h) => sum + h.voterCount, 0),
          streetName
        });
      }
    });

    return clusters;
  }

  return (
    <Box sx={{ position: 'relative', width: '100%', height: '100%' }}>
      <Box
        ref={mapContainer}
        sx={{
          width: '100%',
          height: '100%',
          position: 'absolute',
          top: 0,
          left: 0
        }}
      />
      
      {/* Cluster details panel */}
      {selectedCluster && (
        <Paper
          sx={{
            position: 'absolute',
            right: 16,
            top: 16,
            width: 320,
            maxHeight: '70vh',
            overflow: 'auto',
            p: 2,
            boxShadow: 3
          }}
        >
          <Typography variant="h6" gutterBottom>
            <HomeIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
            {selectedCluster.streetName}
          </Typography>
          
          <Box sx={{ mb: 2 }}>
            <Chip 
              label={`${selectedCluster.houses.length} houses`} 
              size="small" 
              sx={{ mr: 1 }} 
            />
            <Chip 
              label={`${selectedCluster.totalVoters} voters`} 
              size="small" 
              color="primary" 
            />
          </Box>

          <List>
            {selectedCluster.houses.map((house, index) => {
              const isSelected = selectedHouses.includes(house.address);
              const isInRoute = optimizedRoute?.houses.some(h => h.address === house.address);
              
              return (
                <ListItem
                  key={house.address}
                  disablePadding
                  sx={{
                    border: isInRoute ? '2px solid #4CAF50' : 'none',
                    mb: 1,
                    borderRadius: 1
                  }}
                >
                  <ListItemButton
                    onClick={() => onHouseSelect(house)}
                    sx={{
                      backgroundColor: isSelected ? 'action.selected' : 'transparent',
                    }}
                  >
                    <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <Typography variant="body2">
                          {house.address.split(',')[0]}
                        </Typography>
                        {isSelected && (
                          <Chip 
                            label="Selected" 
                            size="small" 
                            color="warning" 
                            sx={{ ml: 1 }} 
                          />
                        )}
                        {isInRoute && (
                          <Chip 
                            label="In Route" 
                            size="small" 
                            color="success" 
                            sx={{ ml: 1 }} 
                          />
                        )}
                      </Box>
                    }
                    secondary={
                      <Box sx={{ display: 'flex', alignItems: 'center', mt: 0.5 }}>
                        <Group sx={{ fontSize: 16, mr: 0.5 }} />
                        <Typography variant="caption">
                          {house.voterCount} voter{house.voterCount > 1 ? 's' : ''}
                        </Typography>
                        <Typography variant="caption" sx={{ ml: 2 }}>
                          {Math.round(house.distanceMeters)}m away
                        </Typography>
                      </Box>
                    }
                  />
                  </ListItemButton>
                </ListItem>
              );
            })}
          </List>

          <Box sx={{ mt: 2, textAlign: 'center' }}>
            <Typography variant="caption" color="text.secondary">
              Click on a house to view voter details
            </Typography>
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
          minWidth: 200
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
          <Typography variant="caption">House clusters</Typography>
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
          <Typography variant="caption">Your location</Typography>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center' }}>
          <Box sx={{ 
            width: 20, 
            height: 20, 
            borderRadius: '50%', 
            backgroundColor: '#9C27B0',
            border: '2px solid white',
            mr: 1
          }} />
          <Typography variant="caption">Other canvassers</Typography>
        </Box>
      </Paper>
    </Box>
  );
};

export default ImprovedWalkMap;