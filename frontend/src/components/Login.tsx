import React, { useState } from 'react';
import QRCode from 'react-qr-code';
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
  HowToVote, 
  Visibility, 
  VisibilityOff,
  Language,
  VideoLibrary,
  Payment,
  HowToReg,
  Phone,
  Help,
  OpenInNew
} from '@mui/icons-material';
import { LoginRequest } from '../types';
import { authService } from '../services/authService';

interface LoginProps {
  onLogin: (credentials: LoginRequest) => Promise<void>;
  isLoading?: boolean;
  error?: string | null;
}

const Login: React.FC<LoginProps> = ({ onLogin, isLoading = false, error }) => {
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
                    href="/register"
                    variant="body2"
                    sx={{ 
                      color: '#2f1c6a',
                      textDecoration: 'none',
                      fontWeight: 500,
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
              <Typography variant="caption" align="center" sx={{ mt: 2, color: '#2f1c6a', fontStyle: 'italic' }}>
                Paid for by Tanveer for Hoover
              </Typography>
            </Box>

            {/* Volunteer Resources */}
            <Box sx={{ mt: 3 }}>
              <Typography variant="h6" align="center" sx={{ mb: 2, color: '#2f1c6a', fontWeight: 600 }}>
                Volunteer Resources
              </Typography>
              
              {/* Campaign Information */}
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
                  Campaign Information
                </Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Language fontSize="small" sx={{ color: '#2f1c6a' }} />
                    <a 
                      href="https://tanveer4hoover.com" 
                      target="_blank" 
                      rel="noopener noreferrer"
                      style={{ color: '#2f1c6a', textDecoration: 'none', fontSize: '14px' }}
                    >
                      Campaign Website <OpenInNew fontSize="small" sx={{ ml: 0.5, verticalAlign: 'middle' }} />
                    </a>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <VideoLibrary fontSize="small" sx={{ color: '#2f1c6a' }} />
                    <a 
                      href="https://youtube.com/@tanveer4hoover" 
                      target="_blank" 
                      rel="noopener noreferrer"
                      style={{ color: '#2f1c6a', textDecoration: 'none', fontSize: '14px' }}
                    >
                      Campaign Videos <OpenInNew fontSize="small" sx={{ ml: 0.5, verticalAlign: 'middle' }} />
                    </a>
                  </Box>
                </Box>
              </Box>

              {/* Support the Campaign */}
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
                  Support the Campaign
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Payment fontSize="small" sx={{ color: '#2f1c6a' }} />
                    <Typography variant="body2" sx={{ color: '#2f1c6a' }}>
                      Venmo: @tanveerforhoover
                    </Typography>
                  </Box>
                  <Box sx={{ p: 1, bgcolor: 'white', borderRadius: 1, border: '1px solid #e0e0e0' }}>
                    <QRCode 
                      value="https://venmo.com/tanveerforhoover" 
                      size={80}
                      style={{ height: "auto", maxWidth: "100%", width: "100%" }}
                    />
                  </Box>
                </Box>
              </Box>

              {/* Voter Resources */}
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
                  Voter Resources
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <HowToReg fontSize="small" sx={{ color: '#2f1c6a' }} />
                  <a 
                    href="https://myinfo.alabamavotes.gov/VoterView" 
                    target="_blank" 
                    rel="noopener noreferrer"
                    style={{ color: '#2f1c6a', textDecoration: 'none', fontSize: '14px' }}
                  >
                    Check Voter Registration <OpenInNew fontSize="small" sx={{ ml: 0.5, verticalAlign: 'middle' }} />
                  </a>
                </Box>
              </Box>

              {/* Support & Help */}
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
                  Support & Help
                </Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Phone fontSize="small" sx={{ color: '#2f1c6a' }} />
                    <Typography variant="body2" sx={{ color: '#2f1c6a' }}>
                      Volunteer Hotline: (205) 555-VOTE
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Help fontSize="small" sx={{ color: '#2f1c6a' }} />
                    <Typography variant="body2" sx={{ color: '#2f1c6a' }}>
                      App Support: Email support@tanveer4hoover.com
                    </Typography>
                  </Box>
                </Box>
              </Box>

              {/* Quick Tips */}
              <Box sx={{ 
                p: 2, 
                background: 'rgba(47, 28, 106, 0.05)',
                borderRadius: 2,
                border: '1px solid rgba(47, 28, 106, 0.1)'
              }}>
                <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, color: '#2f1c6a' }}>
                  Canvassing Quick Tips
                </Typography>
                <Typography variant="body2" sx={{ color: '#2f1c6a', fontSize: '12px', lineHeight: 1.4 }}>
                  • Always wear your volunteer badge<br/>
                  • Be respectful and polite<br/>
                  • Don't argue with voters<br/>
                  • Use the app to log all contacts<br/>
                  • Ask for help if you need it
                </Typography>
              </Box>
            </Box>
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