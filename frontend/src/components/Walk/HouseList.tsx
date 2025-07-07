import React from 'react';
import {
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  ListItemSecondaryAction,
  Checkbox,
  Typography,
  Chip,
  Box,
  Paper,
  Avatar,
  IconButton,
  Divider
} from '@mui/material';
import {
  Home,
  Person,
  Route,
  CheckCircle,
  NavigateNext,
  LocationOn
} from '@mui/icons-material';
import { AvailableHouse, OptimizedRoute } from '../../types/walk';

interface HouseListProps {
  houses: AvailableHouse[];
  selectedHouses: string[];
  optimizedRoute: OptimizedRoute | null;
  currentHouseIndex: number;
  onHouseSelect: (house: AvailableHouse) => void;
  onToggleSelection: (address: string) => void;
}

const HouseList: React.FC<HouseListProps> = ({
  houses,
  selectedHouses,
  optimizedRoute,
  currentHouseIndex,
  onHouseSelect,
  onToggleSelection
}) => {
  const getRouteOrder = (address: string) => {
    if (!optimizedRoute) return null;
    const index = optimizedRoute.houses.findIndex(h => h.address === address);
    return index >= 0 ? index + 1 : null;
  };

  const isCurrentHouse = (address: string) => {
    if (!optimizedRoute) return false;
    return optimizedRoute.houses[currentHouseIndex]?.address === address;
  };

  const formatDistance = (meters: number) => {
    if (meters < 1000) {
      return `${Math.round(meters)}m`;
    }
    return `${(meters / 1000).toFixed(1)}km`;
  };

  const sortedHouses = optimizedRoute
    ? [...houses].sort((a, b) => {
        const orderA = getRouteOrder(a.address) || 999;
        const orderB = getRouteOrder(b.address) || 999;
        if (orderA !== orderB) return orderA - orderB;
        return a.distanceMeters - b.distanceMeters;
      })
    : [...houses].sort((a, b) => a.distanceMeters - b.distanceMeters);

  return (
    <Paper sx={{ height: '100%', overflow: 'auto' }}>
      <List sx={{ py: 0 }}>
        {sortedHouses.map((house, index) => {
          const routeOrder = getRouteOrder(house.address);
          const isCurrent = isCurrentHouse(house.address);
          const isSelected = selectedHouses.includes(house.address);
          const isInRoute = routeOrder !== null;

          return (
            <React.Fragment key={house.address}>
              {index > 0 && <Divider />}
              <ListItem
                disablePadding
                sx={{
                  backgroundColor: isCurrent ? 'action.selected' : 'inherit',
                  '&:hover': {
                    backgroundColor: 'action.hover'
                  }
                }}
              >
                <ListItemButton onClick={() => onHouseSelect(house)}>
                  <ListItemIcon>
                    {isInRoute ? (
                      <Avatar
                        sx={{
                          width: 32,
                          height: 32,
                          bgcolor: isCurrent ? 'error.main' : 'success.main',
                          fontSize: '14px'
                        }}
                      >
                        {routeOrder}
                      </Avatar>
                    ) : (
                      <Checkbox
                        edge="start"
                        checked={isSelected}
                        onChange={() => onToggleSelection(house.address)}
                        onClick={(e) => e.stopPropagation()}
                      />
                    )}
                  </ListItemIcon>
                  
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="subtitle2" component="span">
                          {house.address}
                        </Typography>
                        {isCurrent && (
                          <Chip
                            label="CURRENT"
                            size="small"
                            color="error"
                            sx={{ height: 20 }}
                          />
                        )}
                      </Box>
                    }
                    secondary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 0.5 }}>
                        <Typography variant="caption" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Person sx={{ fontSize: 16 }} />
                          {house.voterCount} voter{house.voterCount !== 1 ? 's' : ''}
                        </Typography>
                        <Typography variant="caption" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <LocationOn sx={{ fontSize: 16 }} />
                          {formatDistance(house.distanceMeters)}
                        </Typography>
                      </Box>
                    }
                  />
                  
                  <ListItemSecondaryAction>
                    <IconButton edge="end" onClick={() => onHouseSelect(house)}>
                      <NavigateNext />
                    </IconButton>
                  </ListItemSecondaryAction>
                </ListItemButton>
              </ListItem>
            </React.Fragment>
          );
        })}
      </List>
    </Paper>
  );
};

export default HouseList;