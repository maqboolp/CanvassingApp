import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  TextField,
  Typography,
  Box,
  Divider
} from '@mui/material';
import { ContactPhone, Person, LocationOn } from '@mui/icons-material';
import { Voter, ContactStatus, VoterSupport, AuthUser } from '../types';

interface ContactModalProps {
  open: boolean;
  voter: Voter | null;
  onClose: () => void;
  onSubmit: (status: ContactStatus, notes: string, voterSupport?: VoterSupport) => void;
  user?: AuthUser;
}

const ContactModal: React.FC<ContactModalProps> = ({
  open,
  voter,
  onClose,
  onSubmit,
  user
}) => {
  const [status, setStatus] = useState<ContactStatus>('reached');
  const [notes, setNotes] = useState('');
  const [voterSupport, setVoterSupport] = useState<VoterSupport | undefined>(undefined);
  const [submitting, setSubmitting] = useState(false);
  const [currentLocation, setCurrentLocation] = useState<{ latitude: number; longitude: number } | null>(null);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [checkingLocation, setCheckingLocation] = useState(false);
  const [distance, setDistance] = useState<number | null>(null);

  // Calculate distance between two coordinates using Haversine formula
  const calculateDistance = (lat1: number, lon1: number, lat2: number, lon2: number): number => {
    const R = 6371; // Earth's radius in km
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = 
      Math.sin(dLat/2) * Math.sin(dLat/2) +
      Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
      Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    return R * c * 1000; // Convert to meters
  };

  // Check location when modal opens
  React.useEffect(() => {
    if (open && voter?.latitude && voter?.longitude && user?.role !== 'superadmin') {
      setCheckingLocation(true);
      setLocationError(null);
      
      if (!navigator.geolocation) {
        setLocationError('Geolocation is not supported by your browser');
        setCheckingLocation(false);
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position) => {
          const location = {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude
          };
          setCurrentLocation(location);
          
          // Calculate distance to voter
          const dist = calculateDistance(
            location.latitude,
            location.longitude,
            voter.latitude!,
            voter.longitude!
          );
          setDistance(Math.round(dist));
          setCheckingLocation(false);
        },
        (error) => {
          setLocationError('Unable to get your location. Please enable location services.');
          setCheckingLocation(false);
        },
        { 
          enableHighAccuracy: true,
          timeout: 10000,
          maximumAge: 0 
        }
      );
    }
  }, [open, voter, user?.role]);

  const handleSubmit = async () => {
    if (!voter) return;
    
    setSubmitting(true);
    try {
      await onSubmit(status, notes, voterSupport);
      // Reset form
      setStatus('reached');
      setNotes('');
      setVoterSupport(undefined);
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = () => {
    setStatus('reached');
    setNotes('');
    setVoterSupport(undefined);
    setCurrentLocation(null);
    setLocationError(null);
    setDistance(null);
    setCheckingLocation(false);
    onClose();
  };

  const isProximityRequired = user?.role !== 'superadmin';
  const isWithinProximity = distance !== null && distance <= 100;
  const canSubmit = !isProximityRequired || isWithinProximity;

  if (!voter) return null;

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <ContactPhone />
        Contact Voter
      </DialogTitle>
      
      <DialogContent>
        {/* Proximity Warning/Status */}
        {isProximityRequired && (
          <Box sx={{ 
            mb: 2, 
            p: 2, 
            bgcolor: checkingLocation ? 'info.light' : 
                     locationError ? 'error.light' :
                     isWithinProximity ? 'success.light' : 'warning.light', 
            borderRadius: 1 
          }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <LocationOn color={
                checkingLocation ? "info" :
                locationError ? "error" :
                isWithinProximity ? "success" : "warning"
              } />
              <Typography variant="body2" color={
                checkingLocation ? "info.dark" :
                locationError ? "error.dark" :
                isWithinProximity ? "success.dark" : "warning.dark"
              }>
                {checkingLocation ? (
                  <>Checking your location...</>
                ) : locationError ? (
                  <>{locationError}</>
                ) : distance !== null ? (
                  isWithinProximity ? (
                    <>You are {distance} meters from the voter - within the required 100 meter range</>
                  ) : (
                    <>You are {distance} meters from the voter - you must be within 100 meters to log this contact</>
                  )
                ) : (
                  <>Location check required</>
                )}
              </Typography>
            </Box>
          </Box>
        )}

        {/* Voter Information */}
        <Box sx={{ mb: 3, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <Person fontSize="small" />
            <Typography variant="h6">
              {voter.firstName} {voter.middleName} {voter.lastName}
            </Typography>
          </Box>
          
          <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
            <LocationOn fontSize="small" color="action" />
            <Box>
              <Typography variant="body2">
                {voter.addressLine}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {voter.city}, {voter.state} {voter.zip}
              </Typography>
            </Box>
          </Box>
          
          {voter.cellPhone && (
            <Typography variant="body2" sx={{ mt: 1 }}>
              <strong>Phone:</strong> {voter.cellPhone}
            </Typography>
          )}
          
          {voter.email && (
            <Typography variant="body2">
              <strong>Email:</strong> {voter.email}
            </Typography>
          )}
          
          <Typography variant="body2">
            <strong>Age:</strong> {voter.age} • <strong>Gender:</strong> {voter.gender}
          </Typography>
          
          <Typography variant="body2">
            <strong>Vote Frequency:</strong> {voter.voteFrequency}
          </Typography>
        </Box>

        <Divider sx={{ my: 2 }} />

        {/* Contact Status */}
        <FormControl component="fieldset" sx={{ mb: 3 }}>
          <FormLabel component="legend">Contact Result *</FormLabel>
          <RadioGroup
            value={status}
            onChange={(e) => setStatus(e.target.value as ContactStatus)}
          >
            <FormControlLabel
              value="reached"
              control={<Radio />}
              label="Reached - Successfully spoke with voter"
            />
            <FormControlLabel
              value="not-home"
              control={<Radio />}
              label="Not Home - No one answered"
            />
            <FormControlLabel
              value="refused"
              control={<Radio />}
              label="Refused - Voter declined to speak"
            />
            <FormControlLabel
              value="needs-follow-up"
              control={<Radio />}
              label="Needs Follow-up - Require additional contact"
            />
          </RadioGroup>
        </FormControl>

        {/* Voter Support - Only show when status is "reached" */}
        {status === 'reached' && (
          <FormControl component="fieldset" sx={{ mb: 3 }}>
            <FormLabel component="legend">Voter Support Level (Optional)</FormLabel>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              How does this voter feel about Tanveer's candidacy?
            </Typography>
            <RadioGroup
              value={voterSupport || ''}
              onChange={(e) => setVoterSupport(e.target.value as VoterSupport || undefined)}
            >
              <FormControlLabel
                value="strongyes"
                control={<Radio />}
                label="Strong Yes - Will vote for Tanveer"
              />
              <FormControlLabel
                value="leaningyes"
                control={<Radio />}
                label="Leaning Yes - May vote for Tanveer"
              />
              <FormControlLabel
                value="undecided"
                control={<Radio />}
                label="Undecided - Need to do research"
              />
              <FormControlLabel
                value="leaningno"
                control={<Radio />}
                label="Leaning No - Not into Tanveer"
              />
              <FormControlLabel
                value="strongno"
                control={<Radio />}
                label="Strong No - Definitely not voting for Tanveer"
              />
              <FormControlLabel
                value=""
                control={<Radio />}
                label="Prefer not to share"
              />
            </RadioGroup>
          </FormControl>
        )}

        {/* Notes */}
        <TextField
          fullWidth
          multiline
          rows={4}
          label="Notes (Optional)"
          placeholder="Add any relevant notes about the interaction, voter feedback, concerns, etc."
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          variant="outlined"
        />

        {/* Instructions */}
        <Box sx={{ mt: 2, p: 2, bgcolor: 'info.light', borderRadius: 1 }}>
          <Typography variant="body2" color="info.contrastText">
            <strong>Reminder:</strong> Keep interactions brief and professional. 
            Focus on voter registration and encourage participation in the democratic process.
          </Typography>
        </Box>
      </DialogContent>
      
      <DialogActions sx={{ p: 2 }}>
        <Button
          onClick={handleClose}
          disabled={submitting}
        >
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={submitting || !canSubmit || checkingLocation}
          startIcon={<ContactPhone />}
        >
          {submitting ? 'Logging Contact...' : 
           checkingLocation ? 'Checking Location...' :
           !canSubmit ? 'Too Far Away' : 'Log Contact'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ContactModal;