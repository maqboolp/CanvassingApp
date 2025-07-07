import React from 'react';
import {
  Box,
  Typography,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Avatar,
  Chip,
  IconButton,
  Divider
} from '@mui/material';
import {
  Person,
  LocationOn,
  Home,
  AccessTime,
  Call
} from '@mui/icons-material';
import { ActiveCanvasser } from '../../types/walk';

interface ActiveCanvassersListProps {
  canvassers: ActiveCanvasser[];
  currentLocation: { lat: number; lng: number } | null;
}

const ActiveCanvassersList: React.FC<ActiveCanvassersListProps> = ({
  canvassers,
  currentLocation
}) => {
  const formatDistance = (meters: number) => {
    if (meters < 1000) {
      return `${Math.round(meters)}m away`;
    }
    return `${(meters / 1000).toFixed(1)}km away`;
  };

  const formatTime = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    return date.toLocaleDateString();
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase();
  };

  return (
    <Box sx={{ width: 320, p: 2 }}>
      <Typography variant="h6" sx={{ mb: 2 }}>
        Active Canvassers Nearby ({canvassers.length})
      </Typography>

      {canvassers.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
          No other canvassers in your area
        </Typography>
      ) : (
        <List>
          {canvassers.map((canvasser, index) => (
            <React.Fragment key={canvasser.volunteerId}>
              {index > 0 && <Divider />}
              <ListItem alignItems="flex-start" sx={{ px: 0 }}>
                <ListItemAvatar>
                  <Avatar sx={{ bgcolor: 'primary.main' }}>
                    {getInitials(canvasser.name)}
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  primary={
                    <Typography variant="subtitle1" fontWeight="bold">
                      {canvasser.name}
                    </Typography>
                  }
                  secondary={
                    <Box sx={{ mt: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.5 }}>
                        <LocationOn sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="caption" color="text.secondary">
                          {formatDistance(canvasser.distanceMeters)}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 0.5 }}>
                        <Home sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="caption" color="text.secondary">
                          {canvasser.housesVisited} houses visited
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <AccessTime sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="caption" color="text.secondary">
                          Last update {formatTime(canvasser.lastUpdateTime)}
                        </Typography>
                      </Box>
                    </Box>
                  }
                />
                <IconButton edge="end" color="primary">
                  <Call />
                </IconButton>
              </ListItem>
            </React.Fragment>
          ))}
        </List>
      )}
    </Box>
  );
};

export default ActiveCanvassersList;