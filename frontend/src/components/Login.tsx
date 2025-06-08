import React, { useState } from 'react';
import {
  Paper,
  TextField,
  Button,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Container,
  Card,
  CardContent
} from '@mui/material';
import { Login as LoginIcon, HowToVote } from '@mui/icons-material';
import { LoginRequest } from '../types';

interface LoginProps {
  onLogin: (credentials: LoginRequest) => Promise<void>;
  isLoading?: boolean;
  error?: string | null;
}

const Login: React.FC<LoginProps> = ({ onLogin, isLoading = false, error }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (email && password) {
      await onLogin({ email, password });
    }
  };

  return (
    <Container component="main" maxWidth="sm">
      <Box
        sx={{
          marginTop: 8,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Card sx={{ width: '100%', maxWidth: 400 }}>
          <CardContent sx={{ p: 4 }}>
            {/* Header */}
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mb: 3 }}>
              <img 
                src="/campaign-logo.png" 
                alt="Tanveer Patel for Hoover City Council" 
                style={{ 
                  width: 'auto', 
                  maxWidth: '180px', 
                  height: '60px', 
                  marginBottom: '16px' 
                }} 
              />
              <Typography variant="subtitle1" align="center" color="text.secondary">
                Canvassing Portal
              </Typography>
            </Box>

            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}

            <Box component="form" onSubmit={handleSubmit} sx={{ mt: 1 }}>
              <TextField
                margin="normal"
                required
                fullWidth
                id="email"
                label="Email Address"
                name="email"
                autoComplete="email"
                autoFocus
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isLoading}
              />
              <TextField
                margin="normal"
                required
                fullWidth
                name="password"
                label="Password"
                type="password"
                id="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isLoading}
              />
              <Button
                type="submit"
                fullWidth
                variant="contained"
                sx={{ mt: 3, mb: 2, py: 1.5 }}
                disabled={isLoading || !email || !password}
                startIcon={isLoading ? <CircularProgress size={20} /> : <LoginIcon />}
              >
                {isLoading ? 'Signing In...' : 'Sign In'}
              </Button>
            </Box>

            {/* Campaign Info */}
            <Box sx={{ 
              mt: 3, 
              p: 2, 
              background: 'linear-gradient(45deg, #ebe4ff 30%, #d5dfff 90%)',
              borderRadius: 2,
              border: '1px solid rgba(103, 61, 230, 0.1)'
            }}>
              <Typography variant="body2" align="center" sx={{ color: '#2f1c6a', fontWeight: 600 }}>
                "Take a Walk With Me."
              </Typography>
              <Typography variant="body2" align="center" sx={{ color: '#2f1c6a', mt: 0.5 }}>
                Join the movement for a better Hoover - August 26, 2025 Election
              </Typography>
              <Typography variant="body2" align="center" sx={{ mt: 1 }}>
                <strong>Promote voter registration:</strong>{' '}
                <a 
                  href="https://myinfo.alabamavotes.gov/VoterView" 
                  target="_blank" 
                  rel="noopener noreferrer"
                  style={{ color: 'inherit' }}
                >
                  alabamavotes.gov
                </a>
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Box>
    </Container>
  );
};

export default Login;