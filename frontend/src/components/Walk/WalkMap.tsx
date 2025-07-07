import React, { useEffect, useRef, useState } from 'react';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import './WalkMap.css';
import { Box } from '@mui/material';
import { AvailableHouse, OptimizedRoute, ActiveCanvasser } from '../../types/walk';
import { geocodingService } from '../../services/geocodingService';

// Ensure Mapbox CSS is loaded
if (typeof window !== 'undefined') {
  require('mapbox-gl/dist/mapbox-gl.css');
}

// Set your Mapbox token here or in environment variable
const MAPBOX_TOKEN = process.env.REACT_APP_MAPBOX_TOKEN || 'pk.eyJ1IjoibWFxYm9vbHAiLCJhIjoiY21jcnlkZHRoMHJwMTJrcTc0OW53YjI4ayJ9.Z-OEVxJnAN8QmLeN57yqJg';

// Always set the token
mapboxgl.accessToken = MAPBOX_TOKEN;

interface WalkMapProps {
  currentLocation: { lat: number; lng: number } | null;
  availableHouses: AvailableHouse[];
  selectedHouses: string[];
  optimizedRoute: OptimizedRoute | null;
  currentHouseIndex: number;
  activeCanvassers: ActiveCanvasser[];
  onHouseSelect: (house: AvailableHouse) => void;
  onToggleHouseSelection: (address: string) => void;
}

interface GeocodedHouse extends AvailableHouse {
  geocodedLat?: number;
  geocodedLng?: number;
  geocodeStatus?: 'pending' | 'success' | 'failed';
}

const WalkMap: React.FC<WalkMapProps> = ({
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
  const markersRef = useRef<{ [key: string]: mapboxgl.Marker }>({});
  const [mapLoaded, setMapLoaded] = useState(false);

  // Initialize map
  useEffect(() => {
    // Clean up any existing map first
    if (map.current) {
      try {
        map.current.remove();
      } catch (e) {
        console.error('Error removing map:', e);
      }
      map.current = null;
    }
    
    const initializeMap = () => {
      
      if (!mapContainer.current) {
        setTimeout(() => {
          initializeMap();
        }, 100);
        return;
      }
      
      if (map.current) {
        return;
      }

      // Check if Mapbox token is available
      if (!MAPBOX_TOKEN || MAPBOX_TOKEN === 'YOUR_MAPBOX_TOKEN' || MAPBOX_TOKEN.includes('example')) {
        console.warn('Mapbox token not configured. Map will not load.');
        console.warn('Token check failed - Token:', MAPBOX_TOKEN);
        if (mapContainer.current) {
          mapContainer.current.innerHTML = `
            <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: #f5f5f5; color: #666;">
              <div style="text-align: center;">
                <div style="font-size: 24px; margin-bottom: 8px;">🗺️</div>
                <div>Map requires Mapbox token</div>
                <div style="font-size: 12px; margin-top: 4px;">Set REACT_APP_MAPBOX_TOKEN in .env</div>
              </div>
            </div>
          `;
        }
        return;
      }
    
      try {
      // Ensure container has dimensions
        const rect = mapContainer.current.getBoundingClientRect();
        
        if (rect.width === 0 || rect.height === 0) {
          setTimeout(() => {
            if (mapContainer.current && !map.current) {
              initializeMap();
            }
          }, 500);
          return;
        }
      
        map.current = new mapboxgl.Map({
          container: mapContainer.current,
          style: 'mapbox://styles/mapbox/streets-v12',
          center: currentLocation ? [currentLocation.lng, currentLocation.lat] : [-86.8104, 33.5186],
          zoom: 17 // Higher initial zoom for dense neighborhoods
        });
      } catch (error) {
        console.error('WalkMap - Error creating map:', error);
        if (error instanceof Error) {
          console.error('WalkMap - Error stack:', error.stack);
        }
        return;
      }

      // Force resize after a short delay to ensure container is ready
      setTimeout(() => {
        if (map.current) {
          map.current.resize();
        }
      }, 100);
      
      map.current.on('load', () => {
        console.log('WalkMap - Map load event fired');
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
      });
      
      // Check if map is already loaded (can happen with HMR)
      if (map.current.isStyleLoaded && map.current.isStyleLoaded()) {
        console.log('WalkMap - Map style already loaded');
        setMapLoaded(true);
      }

      map.current.on('error', (e) => {
        console.error('WalkMap - Map error:', e);
      });
      
      // Check if map loaded after timeout
      setTimeout(() => {
        if (map.current && !mapLoaded) {
          setMapLoaded(true);
        }
      }, 2000);

    };
    
    initializeMap();
    
    return () => {
      if (map.current) {
        try {
          map.current.remove();
          map.current = null;
        } catch (e) {
          console.error('Error in cleanup:', e);
        }
      }
    };
  }, []);

  // Update current location marker
  useEffect(() => {
    if (!map.current || !mapLoaded || !currentLocation) return;

    // Ensure map is ready
    if (!map.current.loaded()) {
      map.current.once('load', () => {
        addCurrentLocationMarker();
      });
      return;
    }

    addCurrentLocationMarker();

    function addCurrentLocationMarker() {
      if (!map.current || !currentLocation) return;
      
      const marker = markersRef.current['currentLocation'];
      if (marker) {
        marker.setLngLat([currentLocation.lng, currentLocation.lat]);
      } else {
        // Create current location marker
        const el = document.createElement('div');
        el.className = 'current-location-marker';
        el.style.width = '20px';
        el.style.height = '20px';
        el.style.borderRadius = '50%';
        el.style.backgroundColor = '#2196F3';
        el.style.border = '3px solid white';
        el.style.boxShadow = '0 2px 6px rgba(0,0,0,0.3)';

        markersRef.current['currentLocation'] = new mapboxgl.Marker(el)
          .setLngLat([currentLocation.lng, currentLocation.lat])
          .addTo(map.current);
      }
    }

    // Center map on current location with higher zoom for dense areas
    map.current.flyTo({
      center: [currentLocation.lng, currentLocation.lat],
      zoom: 17 // Increased zoom to see houses better
    });
  }, [currentLocation, mapLoaded]);

  // Update house markers
  useEffect(() => {
    console.log('WalkMap - House markers effect. Map loaded:', mapLoaded, 'Houses:', availableHouses.length);
    
    if (!map.current || !mapLoaded) {
      console.log('WalkMap - Skipping markers, map not ready');
      return;
    }
    
    // Try to update markers directly - the mapLoaded state should be reliable
    updateHouseMarkers();
    
    function updateHouseMarkers() {
      if (!map.current) return;
      
      console.log('WalkMap - updateHouseMarkers called with', availableHouses.length, 'houses');

      // Remove old markers
      Object.entries(markersRef.current).forEach(([key, marker]) => {
        if (key !== 'currentLocation' && !key.startsWith('canvasser-')) {
          marker.remove();
          delete markersRef.current[key];
        }
      });

    let markerCount = 0;
    // Log coordinate bounds
    if (availableHouses.length > 0) {
      const lats = availableHouses.map(h => h.latitude);
      const lngs = availableHouses.map(h => h.longitude);
      console.log('Latitude range:', Math.min(...lats), 'to', Math.max(...lats));
      console.log('Longitude range:', Math.min(...lngs), 'to', Math.max(...lngs));
    }
    
    // Add house markers (filter out ones in water)
    availableHouses.forEach((house, index) => {
      try {
      // Skip houses that are likely in water bodies - but still try to geocode them
      const isInWater = (
        // Lake Purdy area
        (house.latitude >= 33.430 && house.latitude <= 33.450 && 
         house.longitude >= -86.630 && house.longitude <= -86.600) ||
        // Cahaba River areas
        (house.latitude >= 33.380 && house.latitude <= 33.400 && 
         house.longitude >= -86.820 && house.longitude <= -86.800)
      );
      
      // Don't skip, but flag for geocoding
      if (isInWater) {
        console.warn(`House may be in water, will attempt to geocode: ${house.address} at ${house.latitude}, ${house.longitude}`);
      }
      
      // Log every 10th house for debugging
      if (index % 10 === 0) {
        console.log(`House ${index}:`, house.address, 'at', house.latitude, house.longitude, 'voters:', house.voterCount);
      }
      const isSelected = selectedHouses.includes(house.address);
      const isInRoute = optimizedRoute?.houses.some(h => h.address === house.address);
      const routeIndex = optimizedRoute?.houses.findIndex(h => h.address === house.address);
      const isCurrent = routeIndex === currentHouseIndex;

      // Create custom marker element with proper anchor
      const el = document.createElement('div');
      el.className = 'house-marker';
      el.style.width = '30px';
      el.style.height = '30px';
      el.style.borderRadius = '50%';
      el.style.border = '2px solid white';
      el.style.boxShadow = '0 2px 6px rgba(0,0,0,0.3)';
      el.style.cursor = 'pointer';
      el.style.display = 'flex';
      el.style.alignItems = 'center';
      el.style.justifyContent = 'center';
      el.style.fontSize = '12px';
      el.style.fontWeight = 'bold';
      el.style.color = 'white';
      el.style.position = 'relative';

      const voterCountText = String(house.voterCount || 0);

      if (isCurrent) {
        el.style.backgroundColor = '#FF5722';
        el.style.width = '36px';
        el.style.height = '36px';
        el.textContent = voterCountText;
        // Add pulsing animation for current house
        el.style.animation = 'pulse 2s infinite';
      } else if (isInRoute && routeIndex !== undefined) {
        el.style.backgroundColor = '#4CAF50';
        el.textContent = voterCountText;
      } else if (isSelected) {
        el.style.backgroundColor = '#FFC107';
        el.textContent = voterCountText;
      } else {
        el.style.backgroundColor = '#757575';
        el.textContent = voterCountText;
      }
      
      // Add CSS for pulse animation
      if (isCurrent && !document.getElementById('marker-pulse-style')) {
        const style = document.createElement('style');
        style.id = 'marker-pulse-style';
        style.textContent = `
          @keyframes pulse {
            0% {
              box-shadow: 0 0 0 0 rgba(255, 87, 34, 0.7);
            }
            70% {
              box-shadow: 0 0 0 10px rgba(255, 87, 34, 0);
            }
            100% {
              box-shadow: 0 0 0 0 rgba(255, 87, 34, 0);
            }
          }
        `;
        document.head.appendChild(style);
      }

      el.onclick = () => {
        onHouseSelect(house);
      };

      const popup = new mapboxgl.Popup({ offset: 25 })
        .setHTML(`
          <div style="padding: 8px;">
            <strong>${house.address}</strong><br/>
            ${house.voterCount} voter${house.voterCount > 1 ? 's' : ''}<br/>
            ${Math.round(house.distanceMeters)}m away
          </div>
        `);

      // Create marker with anchor point set
      const marker = new mapboxgl.Marker({
        element: el,
        anchor: 'center' // This ensures the marker stays centered on its coordinates
      })
        .setLngLat([house.longitude, house.latitude])
        .setPopup(popup)
        .addTo(map.current!);

      // If house is in water or we suspect bad coordinates, geocode immediately
      if (isInWater) {
        // Use Mapbox Geocoding API directly for consistency with popup
        const geocodeUrl = `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(house.address + ', Hoover, AL')}.json?access_token=${MAPBOX_TOKEN}&types=address&limit=1&bbox=-87.0,33.3,-86.6,33.5`;
        
        fetch(geocodeUrl)
          .then(response => response.json())
          .then(data => {
            if (data.features && data.features.length > 0) {
              const [lng, lat] = data.features[0].center;
              marker.setLngLat([lng, lat]);
              console.log(`Geocoded ${house.address} from water location [${house.longitude}, ${house.latitude}] to [${lng}, ${lat}]`);
              
              // Update the popup location too
              popup.setLngLat([lng, lat]);
            }
          })
          .catch(err => {
            console.error(`Failed to geocode ${house.address}:`, err);
          });
      }

      markersRef.current[house.address] = marker;
      markerCount++;
      } catch (error) {
        console.error(`WalkMap - Error adding marker for house ${index}:`, error, house);
      }
    });
    
    console.log(`WalkMap - Added ${markerCount} markers out of ${availableHouses.length} houses`);
    }
  }, [availableHouses, selectedHouses, optimizedRoute, currentHouseIndex, mapLoaded]);

  // Draw route line
  useEffect(() => {
    if (!map.current || !mapLoaded || !optimizedRoute || !currentLocation) return;

    // Ensure map is ready
    if (!map.current.loaded()) {
      map.current.once('load', () => {
        drawRoute();
      });
      return;
    }

    drawRoute();

    function drawRoute() {
      if (!map.current || !optimizedRoute || !currentLocation) return;

    const routeCoordinates: [number, number][] = [
      [currentLocation.lng, currentLocation.lat],
      ...optimizedRoute.houses.map(h => [h.longitude, h.latitude] as [number, number])
    ];

    // Remove existing route
    if (map.current.getSource('route')) {
      map.current.removeLayer('route');
      map.current.removeSource('route');
    }

    // Add route line
    map.current.addSource('route', {
      type: 'geojson',
      data: {
        type: 'Feature',
        properties: {},
        geometry: {
          type: 'LineString',
          coordinates: routeCoordinates
        }
      }
    });

    map.current.addLayer({
      id: 'route',
      type: 'line',
      source: 'route',
      layout: {
        'line-join': 'round',
        'line-cap': 'round'
      },
      paint: {
        'line-color': '#2196F3',
        'line-width': 4,
        'line-opacity': 0.75
      }
    });

    // Fit map to show entire route
    const bounds = new mapboxgl.LngLatBounds();
    routeCoordinates.forEach(coord => bounds.extend(coord));
    map.current.fitBounds(bounds, { padding: 50 });
    }
  }, [optimizedRoute, currentLocation, mapLoaded]);

  // Update active canvasser markers
  useEffect(() => {
    if (!map.current || !mapLoaded) return;

    // Ensure map is ready
    if (!map.current.loaded()) {
      map.current.once('load', () => {
        updateCanvasserMarkers();
      });
      return;
    }

    updateCanvasserMarkers();

    function updateCanvasserMarkers() {
      if (!map.current) return;

      // Remove old canvasser markers
      Object.entries(markersRef.current).forEach(([key, marker]) => {
        if (key.startsWith('canvasser-')) {
          marker.remove();
          delete markersRef.current[key];
        }
      });

      // Add canvasser markers
      activeCanvassers.forEach((canvasser) => {
      const el = document.createElement('div');
      el.className = 'canvasser-marker';
      el.style.width = '24px';
      el.style.height = '24px';
      el.style.borderRadius = '50%';
      el.style.backgroundColor = '#9C27B0';
      el.style.border = '2px solid white';
      el.style.boxShadow = '0 2px 4px rgba(0,0,0,0.3)';

      const popup = new mapboxgl.Popup({ offset: 25 })
        .setHTML(`
          <div style="padding: 8px;">
            <strong>${canvasser.name}</strong><br/>
            ${canvasser.housesVisited} houses visited<br/>
            ${Math.round(canvasser.distanceMeters)}m away
          </div>
        `);

      const marker = new mapboxgl.Marker(el)
        .setLngLat([canvasser.longitude, canvasser.latitude])
        .setPopup(popup)
        .addTo(map.current!);

      markersRef.current[`canvasser-${canvasser.volunteerId}`] = marker;
      });
    }
  }, [activeCanvassers, mapLoaded]);

  return (
    <Box
      ref={mapContainer}
      sx={{
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        width: '100%',
        height: '100%',
        backgroundColor: '#f0f0f0',
        '& .mapboxgl-map': {
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%'
        },
        '& .mapboxgl-canvas': {
          position: 'absolute',
          width: '100%',
          height: '100%'
        },
        '& .mapboxgl-popup': {
          maxWidth: '200px'
        },
        '& .mapboxgl-popup-content': {
          padding: '0'
        }
      }}
    />
  );
};

export default WalkMap;