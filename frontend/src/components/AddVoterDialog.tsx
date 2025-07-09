import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Button,
  Box,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  CircularProgress,
  Alert
} from '@mui/material';
import { PersonAdd, Add } from '@mui/icons-material';
import { API_BASE_URL } from '../config';

interface AddVoterDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  googleMapsApiKey?: string;
}

const AddVoterDialog: React.FC<AddVoterDialogProps> = ({ open, onClose, onSuccess, googleMapsApiKey }) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [newVoter, setNewVoter] = useState({
    firstName: '',
    lastName: '',
    addressLine: '',
    city: '',
    state: 'AL',
    zip: '',
    age: '',
    gender: 'Unknown',
    cellPhone: '',
    email: '',
    voteFrequency: 'NonVoter',
    partyAffiliation: ''
  });

  const geocodeAddress = async (address: string): Promise<{ lat: number; lng: number } | null> => {
    if (!googleMapsApiKey) return null;
    
    try {
      const response = await fetch(
        `https://maps.googleapis.com/maps/api/geocode/json?address=${encodeURIComponent(address)}&key=${googleMapsApiKey}`
      );
      const data = await response.json();
      
      if (data.status === 'OK' && data.results.length > 0) {
        const location = data.results[0].geometry.location;
        return { lat: location.lat, lng: location.lng };
      }
    } catch (err) {
      console.error('Geocoding error:', err);
    }
    return null;
  };

  const handleAddVoter = async () => {
    setLoading(true);
    setError(null);

    try {
      const token = localStorage.getItem('auth_token');
      if (!token) {
        throw new Error('Not authenticated');
      }

      // Validate required fields
      if (!newVoter.firstName.trim() || !newVoter.lastName.trim() || 
          !newVoter.addressLine.trim() || !newVoter.city.trim() || 
          !newVoter.zip.trim() || !newVoter.age.trim()) {
        throw new Error('Please fill in all required fields');
      }

      // Geocode the address
      const fullAddress = `${newVoter.addressLine}, ${newVoter.city}, ${newVoter.state} ${newVoter.zip}`;
      const coordinates = await geocodeAddress(fullAddress);

      const voterData = {
        firstName: newVoter.firstName,
        lastName: newVoter.lastName,
        addressLine: newVoter.addressLine,
        city: newVoter.city,
        state: newVoter.state,
        zip: newVoter.zip,
        age: parseInt(newVoter.age),
        gender: newVoter.gender,
        cellPhone: newVoter.cellPhone,
        email: newVoter.email,
        voteFrequency: newVoter.voteFrequency === 'NonVoter' ? 0 : 
                       newVoter.voteFrequency === 'Infrequent' ? 1 : 2,
        partyAffiliation: newVoter.partyAffiliation,
        ...(coordinates && { latitude: coordinates.lat, longitude: coordinates.lng })
      };

      const response = await fetch(`${API_BASE_URL}/api/voters`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(voterData)
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to add voter');
      }

      // Reset form
      setNewVoter({
        firstName: '',
        lastName: '',
        addressLine: '',
        city: '',
        state: 'AL',
        zip: '',
        age: '',
        gender: 'Unknown',
        cellPhone: '',
        email: '',
        voteFrequency: 'NonVoter',
        partyAffiliation: ''
      });

      onSuccess();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add voter');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={() => {
      if (!loading) {
        onClose();
      }
    }} maxWidth="md" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <PersonAdd color="success" />
        Add New Voter
      </DialogTitle>
      <DialogContent>
        <Box sx={{ pt: 2 }}>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
              {error}
            </Alert>
          )}
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' }, gap: 2 }}>
            <TextField
              label="First Name"
              value={newVoter.firstName}
              onChange={(e) => setNewVoter({ ...newVoter, firstName: e.target.value })}
              fullWidth
              required
              disabled={loading}
            />
            <TextField
              label="Last Name"
              value={newVoter.lastName}
              onChange={(e) => setNewVoter({ ...newVoter, lastName: e.target.value })}
              fullWidth
              required
              disabled={loading}
            />
            <TextField
              label="Street Address"
              value={newVoter.addressLine}
              onChange={(e) => setNewVoter({ ...newVoter, addressLine: e.target.value })}
              fullWidth
              required
              disabled={loading}
            />
            <TextField
              label="City"
              value={newVoter.city}
              onChange={(e) => setNewVoter({ ...newVoter, city: e.target.value })}
              fullWidth
              required
              disabled={loading}
            />
            <FormControl fullWidth required>
              <InputLabel>State</InputLabel>
              <Select
                value={newVoter.state}
                onChange={(e) => setNewVoter({ ...newVoter, state: e.target.value })}
                label="State"
                disabled={loading}
              >
                <MenuItem value="AL">Alabama</MenuItem>
                <MenuItem value="AK">Alaska</MenuItem>
                <MenuItem value="AZ">Arizona</MenuItem>
                <MenuItem value="AR">Arkansas</MenuItem>
                <MenuItem value="CA">California</MenuItem>
                <MenuItem value="CO">Colorado</MenuItem>
                <MenuItem value="CT">Connecticut</MenuItem>
                <MenuItem value="DE">Delaware</MenuItem>
                <MenuItem value="FL">Florida</MenuItem>
                <MenuItem value="GA">Georgia</MenuItem>
                <MenuItem value="HI">Hawaii</MenuItem>
                <MenuItem value="ID">Idaho</MenuItem>
                <MenuItem value="IL">Illinois</MenuItem>
                <MenuItem value="IN">Indiana</MenuItem>
                <MenuItem value="IA">Iowa</MenuItem>
                <MenuItem value="KS">Kansas</MenuItem>
                <MenuItem value="KY">Kentucky</MenuItem>
                <MenuItem value="LA">Louisiana</MenuItem>
                <MenuItem value="ME">Maine</MenuItem>
                <MenuItem value="MD">Maryland</MenuItem>
                <MenuItem value="MA">Massachusetts</MenuItem>
                <MenuItem value="MI">Michigan</MenuItem>
                <MenuItem value="MN">Minnesota</MenuItem>
                <MenuItem value="MS">Mississippi</MenuItem>
                <MenuItem value="MO">Missouri</MenuItem>
                <MenuItem value="MT">Montana</MenuItem>
                <MenuItem value="NE">Nebraska</MenuItem>
                <MenuItem value="NV">Nevada</MenuItem>
                <MenuItem value="NH">New Hampshire</MenuItem>
                <MenuItem value="NJ">New Jersey</MenuItem>
                <MenuItem value="NM">New Mexico</MenuItem>
                <MenuItem value="NY">New York</MenuItem>
                <MenuItem value="NC">North Carolina</MenuItem>
                <MenuItem value="ND">North Dakota</MenuItem>
                <MenuItem value="OH">Ohio</MenuItem>
                <MenuItem value="OK">Oklahoma</MenuItem>
                <MenuItem value="OR">Oregon</MenuItem>
                <MenuItem value="PA">Pennsylvania</MenuItem>
                <MenuItem value="RI">Rhode Island</MenuItem>
                <MenuItem value="SC">South Carolina</MenuItem>
                <MenuItem value="SD">South Dakota</MenuItem>
                <MenuItem value="TN">Tennessee</MenuItem>
                <MenuItem value="TX">Texas</MenuItem>
                <MenuItem value="UT">Utah</MenuItem>
                <MenuItem value="VT">Vermont</MenuItem>
                <MenuItem value="VA">Virginia</MenuItem>
                <MenuItem value="WA">Washington</MenuItem>
                <MenuItem value="WV">West Virginia</MenuItem>
                <MenuItem value="WI">Wisconsin</MenuItem>
                <MenuItem value="WY">Wyoming</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="ZIP Code"
              value={newVoter.zip}
              onChange={(e) => setNewVoter({ ...newVoter, zip: e.target.value })}
              fullWidth
              required
              disabled={loading}
            />
            <TextField
              label="Age"
              type="number"
              value={newVoter.age}
              onChange={(e) => setNewVoter({ ...newVoter, age: e.target.value })}
              fullWidth
              required
              disabled={loading}
            />
            <FormControl fullWidth>
              <InputLabel>Gender</InputLabel>
              <Select
                value={newVoter.gender}
                onChange={(e) => setNewVoter({ ...newVoter, gender: e.target.value })}
                label="Gender"
                disabled={loading}
              >
                <MenuItem value="Unknown">Unknown</MenuItem>
                <MenuItem value="Male">Male</MenuItem>
                <MenuItem value="Female">Female</MenuItem>
                <MenuItem value="Other">Other</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Cell Phone"
              value={newVoter.cellPhone}
              onChange={(e) => setNewVoter({ ...newVoter, cellPhone: e.target.value })}
              fullWidth
              disabled={loading}
              placeholder="123-456-7890"
            />
            <TextField
              label="Email"
              type="email"
              value={newVoter.email}
              onChange={(e) => setNewVoter({ ...newVoter, email: e.target.value })}
              fullWidth
              disabled={loading}
            />
            <FormControl fullWidth>
              <InputLabel>Vote Frequency</InputLabel>
              <Select
                value={newVoter.voteFrequency}
                onChange={(e) => setNewVoter({ ...newVoter, voteFrequency: e.target.value })}
                label="Vote Frequency"
                disabled={loading}
              >
                <MenuItem value="NonVoter">Non-Voter</MenuItem>
                <MenuItem value="Infrequent">Infrequent</MenuItem>
                <MenuItem value="Frequent">Frequent</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Party Affiliation"
              value={newVoter.partyAffiliation}
              onChange={(e) => setNewVoter({ ...newVoter, partyAffiliation: e.target.value })}
              fullWidth
              disabled={loading}
              placeholder="e.g., Democrat, Republican, Independent"
            />
          </Box>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button 
          onClick={onClose} 
          disabled={loading}
        >
          Cancel
        </Button>
        <Button 
          onClick={handleAddVoter}
          variant="contained" 
          color="success"
          disabled={loading}
          startIcon={loading ? <CircularProgress size={20} /> : <Add />}
        >
          {loading ? 'Adding...' : 'Add Voter'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default AddVoterDialog;