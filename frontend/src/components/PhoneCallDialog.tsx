import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  IconButton,
  Box,
  Typography,
  Divider,
  Paper
} from '@mui/material';
import { Close, Phone, Description } from '@mui/icons-material';
import WebRTCPhone from './WebRTCPhone';
import { Voter } from '../types';
import { campaignConfig } from '../config/customerConfig';

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
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: {
          borderRadius: 2,
          minHeight: '600px',
          maxHeight: '90vh'
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
        {/* Call Script & Tips - Moved to top for visibility */}
        <Paper 
          elevation={0}
          sx={{ 
            p: 2, 
            bgcolor: 'info.lighter',
            borderBottom: 2,
            borderColor: 'info.main',
            borderRadius: 0
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <Description fontSize="small" color="info" />
            <Typography variant="subtitle2" fontWeight="bold" color="info.main">
              Phone Script & Quick Tips
            </Typography>
          </Box>
          
          <Box sx={{ 
            p: 1.5, 
            bgcolor: 'background.paper', 
            borderRadius: 1,
            border: 1,
            borderColor: 'divider',
            mb: 1 
          }}>
            <Typography variant="body2" color="text.primary" sx={{ fontStyle: 'italic' }}>
              "Hello, is this {voter.firstName}? My name is [YOUR NAME] and I'm a volunteer with {campaignConfig.campaignName || 'the campaign'}. 
              We're reaching out to voters in your area about the upcoming election. Do you have just a moment to talk?"
            </Typography>
          </Box>
          
          <Typography variant="caption" fontWeight="medium" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
            Key Points to Cover:
          </Typography>
          <Typography variant="caption" color="text.secondary" component="ul" sx={{ m: 0, pl: 2, lineHeight: 1.8 }}>
            <li>✓ Confirm you're speaking with the right person</li>
            <li>✓ Ask if they plan to vote in the upcoming election</li>
            <li>✓ Gauge their support level (Strong Yes / Leaning Yes / Undecided / Leaning No / Strong No)</li>
            <li>✓ Answer any questions they have about voting</li>
            <li>✓ Thank them for their time</li>
          </Typography>
        </Paper>

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
      </DialogContent>
    </Dialog>
  );
};

export default PhoneCallDialog;