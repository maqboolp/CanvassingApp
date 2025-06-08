import React from 'react';
import { Box } from '@mui/material';

interface SimpleGridProps {
  children: React.ReactNode;
  columns?: number;
  spacing?: number;
}

export const SimpleGrid: React.FC<SimpleGridProps> = ({ 
  children, 
  columns = 4, 
  spacing = 3 
}) => {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: `repeat(auto-fit, minmax(250px, 1fr))`,
        gap: spacing,
        '@media (max-width: 600px)': {
          gridTemplateColumns: '1fr',
        },
        '@media (min-width: 600px) and (max-width: 900px)': {
          gridTemplateColumns: 'repeat(2, 1fr)',
        },
        '@media (min-width: 900px)': {
          gridTemplateColumns: `repeat(${columns}, 1fr)`,
        },
      }}
    >
      {children}
    </Box>
  );
};

interface SimpleGridItemProps {
  children: React.ReactNode;
  xs?: number;
  sm?: number;
  md?: number;
}

export const SimpleGridItem: React.FC<SimpleGridItemProps> = ({ children }) => {
  return <Box>{children}</Box>;
};