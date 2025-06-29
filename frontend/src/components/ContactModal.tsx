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
import { Voter, ContactStatus, VoterSupport } from '../types';

interface ContactModalProps {
  open: boolean;
  voter: Voter | null;
  onClose: () => void;
  onSubmit: (status: ContactStatus, notes: string, voterSupport?: VoterSupport) => void;
}

const ContactModal: React.FC<ContactModalProps> = ({
  open,
  voter,
  onClose,
  onSubmit
}) => {
  const [status, setStatus] = useState<ContactStatus>('reached');
  const [notes, setNotes] = useState('');
  const [voterSupport, setVoterSupport] = useState<VoterSupport | undefined>(undefined);
  const [submitting, setSubmitting] = useState(false);

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
    onClose();
  };

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
        {/* Proximity Warning */}
        <Box sx={{ mb: 2, p: 2, bgcolor: 'warning.light', borderRadius: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <LocationOn color="warning" />
            <Typography variant="body2" color="warning.dark">
              You must be within 100 meters of the voter's location to log this contact
            </Typography>
          </Box>
        </Box>

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
          disabled={submitting}
          startIcon={<ContactPhone />}
        >
          {submitting ? 'Logging Contact...' : 'Log Contact'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ContactModal;