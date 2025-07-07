import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Checkbox,
  Chip,
  Box,
  Typography,
  IconButton,
  Divider,
  FormControlLabel,
  Switch,
  Alert,
  CircularProgress
} from '@mui/material';
import {
  Close,
  Person,
  HowToVote,
  Phone,
  Email,
  CheckCircle,
  Cancel
} from '@mui/icons-material';
import { AvailableHouse, AvailableHouseVoter } from '../../types/walk';
import { API_BASE_URL } from '../../config';

interface VoterContactModalProps {
  open: boolean;
  house: AvailableHouse | null;
  onClose: () => void;
  onContactUpdate: () => void;
  token: string;
}

const VoterContactModal: React.FC<VoterContactModalProps> = ({
  open,
  house,
  onClose,
  onContactUpdate,
  token
}) => {
  const [selectedVoters, setSelectedVoters] = useState<Set<string>>(new Set());
  const [allSelected, setAllSelected] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [votersAtHome, setVotersAtHome] = useState(true);

  const handleToggleVoter = (voterId: string) => {
    const newSelected = new Set(selectedVoters);
    if (newSelected.has(voterId)) {
      newSelected.delete(voterId);
    } else {
      newSelected.add(voterId);
    }
    setSelectedVoters(newSelected);
    setAllSelected(newSelected.size === house?.voters.length);
  };

  const handleToggleAll = () => {
    if (allSelected) {
      setSelectedVoters(new Set());
      setAllSelected(false);
    } else {
      setSelectedVoters(new Set(house?.voters.map(v => v.voterId) || []));
      setAllSelected(true);
    }
  };

  const handleMarkContacted = async () => {
    if (!house || selectedVoters.size === 0) return;

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/walk/contact-voters`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          address: house.address,
          voterIds: Array.from(selectedVoters),
          contactedAt: new Date().toISOString(),
          wasHome: votersAtHome,
          contactMethod: 'InPerson'
        })
      });

      if (!response.ok) {
        throw new Error('Failed to mark voters as contacted');
      }

      // Reset selection
      setSelectedVoters(new Set());
      setAllSelected(false);
      
      // Notify parent to refresh data
      onContactUpdate();
      
      // Close modal
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const getPartyColor = (party?: string) => {
    if (!party) return 'default';
    if (party.toLowerCase().includes('dem')) return 'primary';
    if (party.toLowerCase().includes('rep')) return 'error';
    return 'default';
  };

  const getFrequencyColor = (frequency?: string) => {
    if (!frequency) return 'default';
    if (frequency === 'Frequent') return 'success';
    if (frequency === 'Infrequent') return 'warning';
    if (frequency === 'NonVoter') return 'error';
    return 'default';
  };

  if (!house) return null;

  return (
    <Dialog 
      open={open} 
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: { maxHeight: '80vh' }
      }}
    >
      <DialogTitle>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="h6">
              {house.address}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {house.voterCount} voter{house.voterCount > 1 ? 's' : ''} • {Math.round(house.distanceMeters)}m away
            </Typography>
          </Box>
          <IconButton onClick={onClose} size="small">
            <Close />
          </IconButton>
        </Box>
      </DialogTitle>
      
      <DialogContent dividers>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <FormControlLabel
            control={
              <Checkbox
                checked={allSelected}
                onChange={handleToggleAll}
                indeterminate={selectedVoters.size > 0 && selectedVoters.size < house.voters.length}
              />
            }
            label="Select All"
          />
          <FormControlLabel
            control={
              <Switch
                checked={votersAtHome}
                onChange={(e) => setVotersAtHome(e.target.checked)}
              />
            }
            label="At Home"
          />
        </Box>

        <List>
          {house.voters.map((voter, index) => (
            <React.Fragment key={voter.voterId}>
              {index > 0 && <Divider />}
              <ListItem>
                <ListItemIcon>
                  <Checkbox
                    checked={selectedVoters.has(voter.voterId)}
                    onChange={() => handleToggleVoter(voter.voterId)}
                  />
                </ListItemIcon>
                <ListItemText
                  primary={
                    <Box display="flex" alignItems="center" gap={1}>
                      <Person fontSize="small" />
                      <Typography variant="subtitle1">
                        {voter.name}
                      </Typography>
                      {voter.age > 0 && (
                        <Typography variant="body2" color="text.secondary">
                          (Age {voter.age})
                        </Typography>
                      )}
                    </Box>
                  }
                  secondary={
                    <Box display="flex" gap={1} mt={0.5} flexWrap="wrap">
                      {voter.partyAffiliation && (
                        <Chip
                          size="small"
                          label={voter.partyAffiliation}
                          color={getPartyColor(voter.partyAffiliation) as any}
                          icon={<HowToVote fontSize="small" />}
                        />
                      )}
                      {voter.voteFrequency && (
                        <Chip
                          size="small"
                          label={voter.voteFrequency}
                          color={getFrequencyColor(voter.voteFrequency) as any}
                          variant="outlined"
                        />
                      )}
                    </Box>
                  }
                />
              </ListItem>
            </React.Fragment>
          ))}
        </List>

        <Box mt={2} p={2} bgcolor="grey.100" borderRadius={1}>
          <Typography variant="body2" color="text.secondary">
            {selectedVoters.size === 0 
              ? 'Select voters to mark as contacted'
              : `${selectedVoters.size} voter${selectedVoters.size > 1 ? 's' : ''} selected`
            }
          </Typography>
        </Box>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} color="inherit">
          Cancel
        </Button>
        <Button
          onClick={handleMarkContacted}
          variant="contained"
          color="primary"
          disabled={selectedVoters.size === 0 || loading}
          startIcon={loading ? <CircularProgress size={20} /> : <CheckCircle />}
        >
          Mark as Contacted
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default VoterContactModal;