import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import VersionInfo from './VersionInfo';
import VolunteerResourcesSection from './VolunteerResourcesSection';
import { customerConfig, campaignConfig } from '../config/customerConfig';
import {
  TextField,
  Button,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Container,
  Card,
  CardContent,
  IconButton,
  InputAdornment,
  Link,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions
} from '@mui/material';
import { 
  Login as LoginIcon, 
  Visibility, 
  VisibilityOff
} from '@mui/icons-material';
import { LoginRequest } from '../types';
import { authService } from '../services/authService';

// Get customer-specific campaign info
const campaignSlogan = process.env.REACT_APP_CAMPAIGN_SLOGAN || "Join our campaign!";
const campaignMessage = process.env.REACT_APP_CAMPAIGN_MESSAGE || "Join the movement for positive change";
const campaignDisclaimer = process.env.REACT_APP_CAMPAIGN_DISCLAIMER || `Paid for by ${campaignConfig.campaignName}`;

interface LoginProps {
  onLogin: (credentials: LoginRequest) => Promise<void>;
  isLoading?: boolean;
  error?: string | null;
}

const Login: React.FC<LoginProps> = ({ onLogin, isLoading = false, error }) => {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  
  // Forgot password state
  const [forgotPasswordOpen, setForgotPasswordOpen] = useState(false);
  const [forgotPasswordEmail, setForgotPasswordEmail] = useState('');
  const [forgotPasswordLoading, setForgotPasswordLoading] = useState(false);
  const [forgotPasswordMessage, setForgotPasswordMessage] = useState<string | null>(null);
  const [forgotPasswordError, setForgotPasswordError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (email && password) {
      await onLogin({ email, password });
    }
  };

  const handleForgotPassword = async () => {
    if (!forgotPasswordEmail) {
      setForgotPasswordError('Please enter your email address');
      return;
    }

    setForgotPasswordLoading(true);
    setForgotPasswordError(null);
    setForgotPasswordMessage(null);

    try {
      const result = await authService.forgotPassword(forgotPasswordEmail);
      setForgotPasswordMessage(result.message);
    } catch (error) {
      setForgotPasswordError(error instanceof Error ? error.message : 'An error occurred');
    } finally {
      setForgotPasswordLoading(false);
    }
  };

  const handleOpenForgotPassword = () => {
    setForgotPasswordEmail(email); // Pre-fill with login email if available
    setForgotPasswordOpen(true);
    setForgotPasswordError(null);
    setForgotPasswordMessage(null);
  };

  const handleCloseForgotPassword = () => {
    setForgotPasswordOpen(false);
    setForgotPasswordEmail('');
    setForgotPasswordError(null);
    setForgotPasswordMessage(null);
  };

  return (
    <Container component="main" maxWidth="sm">
      <Box
        sx={{
          marginTop: { xs: 2, sm: 4 },
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Card sx={{ width: '100%', maxWidth: 400 }}>
          <CardContent sx={{ p: 3 }}>
            {/* Header */}
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mb: 2 }}>
              <img 
                src={customerConfig.logoUrl} 
                alt={customerConfig.logoAlt} 
                style={{ 
                  width: 'auto', 
                  maxWidth: '180px', 
                  height: '60px', 
                  marginBottom: '8px' 
                }} 
              />
            </Box>

            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}

            <Box component="form" onSubmit={handleSubmit} sx={{ mt: 2 }}>
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
                type={showPassword ? 'text' : 'password'}
                id="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isLoading}
                InputProps={{
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        aria-label="toggle password visibility"
                        onClick={() => setShowPassword(!showPassword)}
                        edge="end"
                        disabled={isLoading}
                      >
                        {showPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
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
              
              {/* Forgot Password Link */}
              <Box sx={{ textAlign: 'center', mb: 2 }}>
                <Link
                  component="button"
                  variant="body2"
                  onClick={handleOpenForgotPassword}
                  sx={{ 
                    color: '#2f1c6a',
                    textDecoration: 'none',
                    '&:hover': {
                      textDecoration: 'underline'
                    }
                  }}
                >
                  Forgot your password?
                </Link>
              </Box>

              {/* Registration Link */}
              <Box sx={{ textAlign: 'center', mb: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  Want to join our team?{' '}
                  <Link
                    component="button"
                    variant="body2"
                    onClick={() => navigate('/register')}
                    sx={{ 
                      color: '#2f1c6a',
                      textDecoration: 'none',
                      fontWeight: 500,
                      background: 'none',
                      border: 'none',
                      cursor: 'pointer',
                      '&:hover': {
                        textDecoration: 'underline'
                      }
                    }}
                  >
                    Register as a volunteer
                  </Link>
                </Typography>
              </Box>
            </Box>

            {/* Campaign Info - Customer Specific */}
            {(campaignSlogan || campaignMessage) && (
              <Box sx={{ 
                mt: 3, 
                p: 2, 
                background: 'linear-gradient(45deg, #ebe4ff 30%, #d5dfff 90%)',
                borderRadius: 2,
                border: '1px solid rgba(103, 61, 230, 0.1)'
              }}>
                {campaignSlogan && (
                  <Typography variant="body2" align="center" sx={{ color: '#2f1c6a', fontWeight: 600 }}>
                    {campaignSlogan}
                  </Typography>
                )}
                {campaignMessage && (
                  <Typography variant="body2" align="center" sx={{ color: '#2f1c6a', mt: 0.5 }}>
                    {campaignMessage}
                  </Typography>
                )}
                {campaignDisclaimer && (
                  <Typography variant="caption" align="center" sx={{ mt: 2, color: '#2f1c6a', fontStyle: 'italic' }}>
                    {campaignDisclaimer}
                  </Typography>
                )}
              </Box>
            )}

            {/* Volunteer Resources */}
            <Box sx={{ mt: 3 }}>
              <Typography variant="h6" align="center" sx={{ mb: 2, color: '#2f1c6a', fontWeight: 600 }}>
                Volunteer Resources
              </Typography>
              <VolunteerResourcesSection showQuickTips={true} showQRCode={true} />
            </Box>

            {/* Version Information */}
            <VersionInfo />
          </CardContent>
        </Card>
      </Box>

      {/* Forgot Password Dialog */}
      <Dialog open={forgotPasswordOpen} onClose={handleCloseForgotPassword} maxWidth="sm" fullWidth>
        <DialogTitle>Reset Your Password</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            Enter your email address and we'll send you a link to reset your password.
          </Typography>
          
          {forgotPasswordError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {forgotPasswordError}
            </Alert>
          )}
          
          {forgotPasswordMessage && (
            <Alert severity="success" sx={{ mb: 2 }}>
              {forgotPasswordMessage}
            </Alert>
          )}
          
          <TextField
            label="Email Address"
            type="email"
            fullWidth
            margin="normal"
            value={forgotPasswordEmail}
            onChange={(e) => setForgotPasswordEmail(e.target.value)}
            disabled={forgotPasswordLoading}
            required
            autoFocus
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseForgotPassword} disabled={forgotPasswordLoading}>
            Cancel
          </Button>
          <Button 
            onClick={handleForgotPassword} 
            variant="contained" 
            disabled={forgotPasswordLoading || !forgotPasswordEmail}
            startIcon={forgotPasswordLoading ? <CircularProgress size={20} /> : undefined}
          >
            {forgotPasswordLoading ? 'Sending...' : 'Send Reset Link'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default Login;