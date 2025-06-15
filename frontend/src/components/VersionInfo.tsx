import React from 'react';
import { Box, Typography } from '@mui/material';

const VersionInfo: React.FC = () => {
  const packageVersion = process.env.REACT_APP_VERSION || '0.1.1';
  const buildHash = process.env.REACT_APP_BUILD_HASH || 'dev';
  const buildDate = process.env.REACT_APP_BUILD_DATE || new Date().toISOString().split('T')[0];

  return (
    <Box sx={{ 
      mt: 2, 
      pt: 2, 
      borderTop: '1px solid rgba(0,0,0,0.1)',
      textAlign: 'center' 
    }}>
      <Typography variant="caption" color="text.secondary" sx={{ fontSize: '10px' }}>
        v{packageVersion} • {buildHash} • {buildDate}
      </Typography>
    </Box>
  );
};

export default VersionInfo;