import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  IconButton,
  Divider,
  RadioGroup,
  FormControlLabel,
  Radio,
  TextField,
  Alert,
  Avatar,
  Card,
  CardContent
} from '@mui/material';
import {
  Close,
  Person,
  Phone,
  Email,
  HowToVote,
  CheckCircle,
  Cancel,
  Home,
  LocationOn,
  DirectionsWalk,
  NavigateNext
} from '@mui/icons-material';
import { AvailableHouse, WalkSession } from '../../types/walk';
import { ContactStatus, VoterSupport } from '../../types';

interface HouseDetailsCardProps {
  house: AvailableHouse;
  session: WalkSession | null;
  onClose: () => void;
  onArrival: (claimId: number) => Promise<void>;
  onComplete: (claimId: number, votersContacted: number, votersHome: number, contactIds: string[]) => Promise<void>;
}

const HouseDetailsCard: React.FC<HouseDetailsCardProps> = ({
  house,
  session,
  onClose,
  onArrival,
  onComplete
}) => {
  const [contactedVoters, setContactedVoters] = useState<Set<string>>(new Set());
  const [voterStatuses, setVoterStatuses] = useState<{ [voterId: string]: ContactStatus }>({});
  const [voterSupport, setVoterSupport] = useState<{ [voterId: string]: VoterSupport }>({});
  const [notes, setNotes] = useState<{ [voterId: string]: string }>({});
  const [arrived, setArrived] = useState(false);
  const [loading, setLoading] = useState(false);

  const currentClaim = session?.activeClaims.find(c => c.address === house.address);

  const handleArrival = async () => {
    if (!currentClaim) return;
    setLoading(true);
    try {
      await onArrival(currentClaim.id);
      setArrived(true);
    } finally {
      setLoading(false);
    }
  };

  const handleComplete = async () => {
    if (!currentClaim) return;
    
    const votersContactedCount = contactedVoters.size;
    const votersHomeCount = house.voters.filter(v => 
      voterStatuses[v.voterId] === 'reached'
    ).length;
    
    setLoading(true);
    try {
      await onComplete(
        currentClaim.id,
        votersContactedCount,
        votersHomeCount,
        Array.from(contactedVoters)
      );
      onClose();
    } finally {
      setLoading(false);
    }
  };

  const toggleVoterContacted = (voterId: string) => {
    const newContacted = new Set(contactedVoters);
    if (newContacted.has(voterId)) {
      newContacted.delete(voterId);
      delete voterStatuses[voterId];
      delete voterSupport[voterId];
    } else {
      newContacted.add(voterId);
      setVoterStatuses({ ...voterStatuses, [voterId]: 'reached' });
    }
    setContactedVoters(newContacted);
  };

  const getVoteFrequencyColor = (frequency: string) => {
    switch (frequency) {
      case 'frequent': return 'success';
      case 'infrequent': return 'warning';
      case 'non-voter': return 'error';
      default: return 'default';
    }
  };

  const getPartyColor = (party?: string) => {
    if (!party) return 'default';
    if (party.toLowerCase().includes('dem')) return 'primary';
    if (party.toLowerCase().includes('rep')) return 'error';
    return 'default';
  };

  return (
    <Dialog
      open={true}
      onClose={onClose}
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: { height: '90vh', display: 'flex', flexDirection: 'column' }
      }}
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Home />
          <Typography variant="h6">{house.address}</Typography>
        </Box>
        <IconButton onClick={onClose}>
          <Close />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers sx={{ flex: 1, overflow: 'auto' }}>
        {/* House Info */}
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Box>
                <Typography variant="subtitle2" color="text.secondary">
                  Distance from you
                </Typography>
                <Typography variant="h6">
                  {house.distanceMeters < 1000 
                    ? `${Math.round(house.distanceMeters)}m` 
                    : `${(house.distanceMeters / 1000).toFixed(1)}km`}
                </Typography>
              </Box>
              <Box>
                <Typography variant="subtitle2" color="text.secondary">
                  Voters at address
                </Typography>
                <Typography variant="h6">
                  {house.voterCount}
                </Typography>
              </Box>
              <Button
                variant="outlined"
                startIcon={<NavigateNext />}
                onClick={() => {
                  window.open(
                    `https://www.google.com/maps/dir/?api=1&destination=${house.latitude},${house.longitude}`,
                    '_blank'
                  );
                }}
              >
                Get Directions
              </Button>
            </Box>

            {!arrived && currentClaim && (
              <Button
                fullWidth
                variant="contained"
                color="primary"
                startIcon={<DirectionsWalk />}
                onClick={handleArrival}
                disabled={loading}
              >
                Mark Arrival at House
              </Button>
            )}

            {arrived && (
              <Alert severity="success" icon={<CheckCircle />}>
                You have arrived at this house
              </Alert>
            )}
          </CardContent>
        </Card>

        {/* Voters List */}
        <Typography variant="h6" sx={{ mb: 2 }}>
          Voters at This Address
        </Typography>
        
        <List>
          {house.voters.map((voter, index) => {
            const isContacted = contactedVoters.has(voter.voterId);
            const status = voterStatuses[voter.voterId];
            const support = voterSupport[voter.voterId];

            return (
              <React.Fragment key={voter.voterId}>
                {index > 0 && <Divider />}
                <ListItem
                  sx={{
                    flexDirection: 'column',
                    alignItems: 'stretch',
                    py: 2,
                    backgroundColor: isContacted ? 'action.selected' : 'inherit'
                  }}
                >
                  {/* Voter Info Header */}
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: isContacted ? 'success.main' : 'grey.400' }}>
                        <Person />
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="subtitle1" fontWeight="bold">
                            {voter.name}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Age {voter.age}
                          </Typography>
                        </Box>
                      }
                      secondary={
                        <Box sx={{ display: 'flex', gap: 1, mt: 0.5 }}>
                          {voter.voteFrequency && (
                            <Chip
                              size="small"
                              label={voter.voteFrequency.replace('-', ' ')}
                              color={getVoteFrequencyColor(voter.voteFrequency)}
                              variant="outlined"
                            />
                          )}
                          {voter.partyAffiliation && (
                            <Chip
                              size="small"
                              label={voter.partyAffiliation}
                              color={getPartyColor(voter.partyAffiliation)}
                              variant="outlined"
                            />
                          )}
                        </Box>
                      }
                    />
                    <Button
                      variant={isContacted ? "contained" : "outlined"}
                      color={isContacted ? "success" : "primary"}
                      onClick={() => toggleVoterContacted(voter.voterId)}
                      startIcon={isContacted ? <CheckCircle /> : <Person />}
                    >
                      {isContacted ? 'Contacted' : 'Mark as Contacted'}
                    </Button>
                  </Box>

                  {/* Contact Details (shown when contacted) */}
                  {isContacted && (
                    <Box sx={{ pl: 7, pr: 2 }}>
                      {/* Contact Status */}
                      <Box sx={{ mb: 2 }}>
                        <Typography variant="subtitle2" gutterBottom>
                          Contact Status
                        </Typography>
                        <RadioGroup
                          row
                          value={status || ''}
                          onChange={(e) => setVoterStatuses({
                            ...voterStatuses,
                            [voter.voterId]: e.target.value as ContactStatus
                          })}
                        >
                          <FormControlLabel 
                            value="reached" 
                            control={<Radio size="small" />} 
                            label="Reached" 
                          />
                          <FormControlLabel 
                            value="not-home" 
                            control={<Radio size="small" />} 
                            label="Not Home" 
                          />
                          <FormControlLabel 
                            value="refused" 
                            control={<Radio size="small" />} 
                            label="Refused" 
                          />
                          <FormControlLabel 
                            value="needs-follow-up" 
                            control={<Radio size="small" />} 
                            label="Follow-up" 
                          />
                        </RadioGroup>
                      </Box>

                      {/* Voter Support (shown if reached) */}
                      {status === 'reached' && (
                        <Box sx={{ mb: 2 }}>
                          <Typography variant="subtitle2" gutterBottom>
                            Voter Support
                          </Typography>
                          <RadioGroup
                            row
                            value={support || ''}
                            onChange={(e) => setVoterSupport({
                              ...voterSupport,
                              [voter.voterId]: e.target.value as VoterSupport
                            })}
                          >
                            <FormControlLabel 
                              value="strongyes" 
                              control={<Radio size="small" color="success" />} 
                              label="Strong Yes" 
                            />
                            <FormControlLabel 
                              value="leaningyes" 
                              control={<Radio size="small" color="success" />} 
                              label="Leaning Yes" 
                            />
                            <FormControlLabel 
                              value="undecided" 
                              control={<Radio size="small" />} 
                              label="Undecided" 
                            />
                            <FormControlLabel 
                              value="leaningno" 
                              control={<Radio size="small" color="error" />} 
                              label="Leaning No" 
                            />
                            <FormControlLabel 
                              value="strongno" 
                              control={<Radio size="small" color="error" />} 
                              label="Strong No" 
                            />
                          </RadioGroup>
                        </Box>
                      )}

                      {/* Notes */}
                      <TextField
                        fullWidth
                        size="small"
                        label="Notes"
                        multiline
                        rows={2}
                        value={notes[voter.voterId] || ''}
                        onChange={(e) => setNotes({
                          ...notes,
                          [voter.voterId]: e.target.value
                        })}
                      />
                    </Box>
                  )}
                </ListItem>
              </React.Fragment>
            );
          })}
        </List>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>
          Cancel
        </Button>
        <Button
          variant="contained"
          color="primary"
          onClick={handleComplete}
          disabled={loading || !arrived || contactedVoters.size === 0}
          startIcon={<CheckCircle />}
        >
          Complete Visit ({contactedVoters.size} contacted)
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default HouseDetailsCard;