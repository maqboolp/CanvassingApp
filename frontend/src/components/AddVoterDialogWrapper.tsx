import React, { useState, useEffect } from 'react';
import { Box, CircularProgress } from '@mui/material';
import { API_BASE_URL } from '../config';
import AddVoterDialog from './AddVoterDialog';

interface AddVoterDialogWrapperProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

const AddVoterDialogWrapper: React.FC<AddVoterDialogWrapperProps> = (props) => {
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
    
    if (props.open) {
      fetchApiKey();
    }
  }, [props.open]);

  if (!props.open) {
    return null;
  }

  if (apiKeyLoading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (apiKeyError || !googleMapsApiKey) {
    // Still show the dialog but without autocomplete
    return <AddVoterDialog {...props} />;
  }

  return <AddVoterDialog {...props} googleMapsApiKey={googleMapsApiKey} />;
};

export default AddVoterDialogWrapper;