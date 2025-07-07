import React, { useEffect, useRef } from 'react';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import { Box } from '@mui/material';

// Set Mapbox token
mapboxgl.accessToken = 'pk.eyJ1IjoibWFxYm9vbHAiLCJhIjoiY21jcnlkZHRoMHJwMTJrcTc0OW53YjI4ayJ9.Z-OEVxJnAN8QmLeN57yqJg';

const TestMap: React.FC = () => {
  const mapContainer = useRef<HTMLDivElement>(null);
  const map = useRef<mapboxgl.Map | null>(null);

  useEffect(() => {
    console.log('TestMap - useEffect running');
    if (!mapContainer.current || map.current) return;

    console.log('TestMap - Creating map...');
    try {
      map.current = new mapboxgl.Map({
        container: mapContainer.current,
        style: 'mapbox://styles/mapbox/streets-v12',
        center: [-86.8104, 33.5186],
        zoom: 15
      });

      map.current.on('load', () => {
        console.log('TestMap - Map loaded!');
      });
    } catch (error) {
      console.error('TestMap - Error:', error);
    }

    return () => {
      map.current?.remove();
    };
  }, []);

  return (
    <Box 
      ref={mapContainer} 
      sx={{ 
        width: '100%', 
        height: '400px',
        backgroundColor: '#ccc'
      }} 
    />
  );
};

export default TestMap;