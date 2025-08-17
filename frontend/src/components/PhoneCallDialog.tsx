import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  IconButton,
  Box,
  Typography,
  Divider
} from '@mui/material';
import { Close } from '@mui/icons-material';
import WebRTCPhone from './WebRTCPhone';
import { Voter } from '../types';

interface PhoneCallDialogProps {
  open: boolean;
  onClose: () => void;
  voter: Voter | null;
  onCallComplete: () => void;
}

const PhoneCallDialog: React.FC<PhoneCallDialogProps> = ({
  open,
  onClose,
  voter,
  onCallComplete
}) => {
  const handleCallComplete = () => {
    onCallComplete();
    // Keep dialog open for a moment to show completion message
    setTimeout(() => {
      onClose();
    }, 2000);
  };

  if (!voter) return null;

  return (
    <Dialog 
      open={open} 
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: {
          borderRadius: 2,
          minHeight: '400px'
        }
      }}
    >
      <DialogTitle sx={{ 
        m: 0, 
        p: 2, 
        display: 'flex', 
        alignItems: 'center', 
        justifyContent: 'space-between',
        bgcolor: 'primary.main',
        color: 'white'
      }}>
        <Box>
          <Typography variant="h6" component="div">
            Phone Banking Call
          </Typography>
          <Typography variant="caption" sx={{ opacity: 0.9 }}>
            Browser-based calling system
          </Typography>
        </Box>
        <IconButton
          aria-label="close"
          onClick={onClose}
          sx={{
            color: 'white',
            '&:hover': {
              bgcolor: 'rgba(255, 255, 255, 0.1)'
            }
          }}
        >
          <Close />
        </IconButton>
      </DialogTitle>
      
      <DialogContent sx={{ p: 0 }}>
        {/* Voter Info Header */}
        <Box sx={{ 
          p: 2, 
          bgcolor: 'grey.50',
          borderBottom: 1,
          borderColor: 'divider'
        }}>
          <Typography variant="subtitle1" fontWeight="bold">
            {voter.firstName} {voter.lastName}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {voter.addressLine}, {voter.city}, {voter.state} {voter.zip}
          </Typography>
          {voter.age && (
            <Typography variant="caption" color="text.secondary">
              Age: {voter.age} • Party: {voter.partyAffiliation || 'Unknown'}
            </Typography>
          )}
        </Box>

        <Divider />

        {/* WebRTC Phone Component */}
        <Box sx={{ p: 2 }}>
          <WebRTCPhone 
            voter={voter} 
            onCallComplete={handleCallComplete}
          />
        </Box>

        {/* Call Tips */}
        <Box sx={{ 
          p: 2, 
          bgcolor: 'info.lighter',
          borderTop: 1,
          borderColor: 'divider',
          mt: 'auto'
        }}>
          <Typography variant="caption" color="text.secondary" component="div">
            <strong>Quick Tips:</strong>
          </Typography>
          <Typography variant="caption" color="text.secondary" component="ul" sx={{ m: 0, pl: 2 }}>
            <li>Introduce yourself and the campaign</li>
            <li>Ask if they plan to vote</li>
            <li>Note their support level</li>
            <li>Thank them for their time</li>
          </Typography>
        </Box>
      </DialogContent>
    </Dialog>
  );
};

export default PhoneCallDialog;