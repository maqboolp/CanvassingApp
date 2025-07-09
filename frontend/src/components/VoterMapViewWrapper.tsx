import React, { useState, useEffect } from 'react';
import { Box, Alert, CircularProgress } from '@mui/material';
import { API_BASE_URL } from '../config';
import VoterMapView from './VoterMapView';
import { Voter } from '../types';

interface VoterMapViewWrapperProps {
  voters: Voter[];
  loading: boolean;
  onRefresh: () => void;
  currentLocation: { latitude: number; longitude: number } | null;
  onContactComplete?: () => void;
}

const VoterMapViewWrapper: React.FC<VoterMapViewWrapperProps> = (props) => {
  const [googleMapsApiKey, setGoogleMapsApiKey] = useState<string>('');
  const [apiKeyLoading, setApiKeyLoading] = useState(true);
  const [apiKeyError, setApiKeyError] = useState<string | null>(null);

  useEffect(() => {
    const fetchApiKey = async () => {
      try {
        const token = localStorage.getItem('auth_token');
        const response = await fetch(`${API_BASE_URL}/api/configuration/google-maps-key`, {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        });
        
        if (response.ok) {
          const data = await response.json();
          setGoogleMapsApiKey(data.apiKey);
        } else {
          setApiKeyError('Failed to load Google Maps configuration');
        }
      } catch (error) {
        console.error('Error fetching Google Maps API key:', error);
        setApiKeyError('Failed to load Google Maps configuration');
      } finally {
        setApiKeyLoading(false);
      }
    };
    
    fetchApiKey();
  }, []);

  if (apiKeyLoading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (apiKeyError || !googleMapsApiKey) {
    return (
      <Alert severity="error">
        {apiKeyError || 'Google Maps API key is not configured on the server'}
      </Alert>
    );
  }

  return <VoterMapView {...props} googleMapsApiKey={googleMapsApiKey} />;
};

export default VoterMapViewWrapper;