import React, { useState } from 'react';
import { Button, Box, Typography, Paper } from '@mui/material';
import { API_BASE_URL } from '../config';

export const ApiTest: React.FC = () => {
  const [results, setResults] = useState<any>({});

  const testEndpoint = async (endpoint: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}${endpoint}`);
      const text = await response.text();
      let data: any;
      try {
        data = JSON.parse(text);
      } catch {
        data = text;
      }
      
      setResults((prev: any) => ({
        ...prev,
        [endpoint]: {
          status: response.status,
          ok: response.ok,
          data,
          headers: Object.fromEntries(response.headers.entries())
        }
      }));
    } catch (err) {
      setResults((prev: any) => ({
        ...prev,
        [endpoint]: {
          error: err instanceof Error ? err.message : 'Unknown error'
        }
      }));
    }
  };

  const endpoints = [
    '/api/healthcheck',
    '/api/healthcheck/settings',
    '/api/settings/twilio',
    '/api/phonenumberpool'
  ];

  return (
    <Paper sx={{ p: 3, mt: 2 }}>
      <Typography variant="h6" gutterBottom>API Endpoint Test</Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        API Base URL: {API_BASE_URL}
      </Typography>
      
      <Box sx={{ mt: 2 }}>
        {endpoints.map(endpoint => (
          <Box key={endpoint} sx={{ mb: 2 }}>
            <Button 
              variant="outlined" 
              size="small"
              onClick={() => testEndpoint(endpoint)}
              sx={{ mb: 1 }}
            >
              Test {endpoint}
            </Button>
            {results[endpoint] && (
              <Paper variant="outlined" sx={{ p: 1, mt: 1 }}>
                <pre style={{ margin: 0, fontSize: '12px', overflow: 'auto' }}>
                  {JSON.stringify(results[endpoint], null, 2)}
                </pre>
              </Paper>
            )}
          </Box>
        ))}
      </Box>
    </Paper>
  );
};