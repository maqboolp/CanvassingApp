import React, { useState } from 'react';
import { Box, Paper, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material';
import { Public, Map as MapIcon } from '@mui/icons-material';
import LeafletWalkMap from './LeafletWalkMap'; // Leaflet/OpenStreetMap implementation
import GoogleMapsWalk from './GoogleMapsWalk'; // Google Maps implementation
import { AvailableHouse, OptimizedRoute, ActiveCanvasser } from '../../types/walk';

interface MapComparisonProps {
  currentLocation: { lat: number; lng: number } | null;
  availableHouses: AvailableHouse[];
  selectedHouses: string[];
  optimizedRoute: OptimizedRoute | null;
  currentHouseIndex: number;
  activeCanvassers: ActiveCanvasser[];
  onHouseSelect: (house: AvailableHouse) => void;
  onToggleHouseSelection: (address: string) => void;
}

type MapProvider = 'osm' | 'google';

const MapComparison: React.FC<MapComparisonProps> = (props) => {
  const [selectedMap, setSelectedMap] = useState<MapProvider>('osm');
  const hasGoogleMapsKey = !!process.env.REACT_APP_GOOGLE_MAPS_API_KEY;

  const handleMapChange = (event: React.MouseEvent<HTMLElement>, newMap: MapProvider | null) => {
    if (newMap !== null) {
      setSelectedMap(newMap);
    }
  };

  return (
    <Box sx={{ position: 'relative', width: '100%', height: '100%' }}>
      {/* Map provider selector - only show if Google Maps is available */}
      {hasGoogleMapsKey && (
        <Paper
          sx={{
            position: 'absolute',
            top: 16,
            left: '50%',
            transform: 'translateX(-50%)',
            zIndex: 1000,
            p: 1,
            backgroundColor: 'rgba(255, 255, 255, 0.95)'
          }}
        >
          <Typography variant="caption" sx={{ display: 'block', mb: 0.5, textAlign: 'center' }}>
            Map Provider
          </Typography>
          <ToggleButtonGroup
            value={selectedMap}
            exclusive
            onChange={handleMapChange}
            size="small"
          >
            <ToggleButton value="osm" aria-label="openstreetmap">
              <Public sx={{ mr: 1 }} />
              OpenStreetMap
            </ToggleButton>
            <ToggleButton value="google" aria-label="google maps">
              <MapIcon sx={{ mr: 1 }} />
              Google Maps
            </ToggleButton>
          </ToggleButtonGroup>
        </Paper>
      )}

      {/* Render selected map */}
      {selectedMap === 'google' && hasGoogleMapsKey ? (
        <GoogleMapsWalk {...props} />
      ) : (
        <LeafletWalkMap {...props} />
      )}
    </Box>
  );
};

export default MapComparison;