import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import VersionInfo from './VersionInfo';
import {
  Container,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Box,
  Alert,
  CircularProgress,
  InputAdornment,
  IconButton,
  Link,
  Stepper,
  Step,
  StepLabel
} from '@mui/material';
import {
  Visibility,
  VisibilityOff,
  Person,
  Email,
  Phone,
  Lock,
  HowToReg,
  CheckCircle,
  Schedule
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { customerConfig } from '../config/customerConfig';

// Get customer-specific campaign info
const campaignSlogan = process.env.REACT_APP_CAMPAIGN_SLOGAN || "Join our campaign!";
const campaignMessage = process.env.REACT_APP_CAMPAIGN_MESSAGE || "Join the movement for positive change";
const campaignDisclaimer = process.env.REACT_APP_CAMPAIGN_DISCLAIMER || `Paid for by ${customerConfig.appTitle}`;
const campaignWebsite = process.env.REACT_APP_CAMPAIGN_WEBSITE;
const registrationTitle = process.env.REACT_APP_REGISTRATION_TITLE || "Join Our Campaign Team";
const registrationSubtitle = process.env.REACT_APP_REGISTRATION_SUBTITLE || "Help bring positive change to our community!";

const SelfRegistration: React.FC = () => {
  const navigate = useNavigate();
  
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    password: '',
    confirmPassword: ''
  });

  const [validationErrors, setValidationErrors] = useState<any>({});

  const validateForm = () => {
    const errors: any = {};

    if (!formData.firstName.trim()) {
      errors.firstName = 'First name is required';
    }

    if (!formData.lastName.trim()) {
      errors.lastName = 'Last name is required';
    }

    if (!formData.email.trim()) {
      errors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = 'Please enter a valid email address';
    }

    if (!formData.phoneNumber.trim()) {
      errors.phoneNumber = 'Phone number is required';
    } else if (!/^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$/.test(formData.phoneNumber)) {
      errors.phoneNumber = 'Please enter a valid phone number';
    }

    if (!formData.password) {
      errors.password = 'Password is required';
    } else if (formData.password.length < 6) {
      errors.password = 'Password must be at least 6 characters';
    }

    if (!formData.confirmPassword) {
      errors.confirmPassword = 'Please confirm your password';
    } else if (formData.password !== formData.confirmPassword) {
      errors.confirmPassword = 'Passwords do not match';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/registration/self-register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          firstName: formData.firstName.trim(),
          lastName: formData.lastName.trim(),
          email: formData.email.trim(),
          phoneNumber: formData.phoneNumber.trim(),
          password: formData.password
        })
      });

      if (response.ok) {
        setSuccess(true);
      } else {
        const errorData = await response.json();
        setError(errorData.error || 'Failed to submit registration');
      }
    } catch (err) {
      setError('Failed to submit registration. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleInputChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear validation error when user starts typing
    if (validationErrors[field]) {
      setValidationErrors((prev: any) => ({ ...prev, [field]: '' }));
    }
  };

  if (success) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 4 }}>
            <img 
              src={customerConfig.logoUrl} 
              alt={customerConfig.logoAlt} 
              style={{ 
                width: 'auto', 
                maxWidth: '180px', 
                height: '60px', 
                marginBottom: '16px' 
              }} 
            />
            <CheckCircle sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
            <Typography variant="h5" gutterBottom sx={{ color: '#2f1c6a', fontWeight: 600 }}>
              Registration Submitted!
            </Typography>
            <Typography variant="body1" color="text.secondary" paragraph>
              Thank you for your interest in joining our campaign! 
              Your registration has been submitted and is awaiting review.
            </Typography>
            <Alert severity="info" sx={{ mb: 3, textAlign: 'left' }}>
              <Typography variant="body2">
                <strong>What happens next:</strong>
                <br />• Our team will review your application
                <br />• You'll receive an email once approved or if we need more information
                <br />• This process typically takes 1-2 business days
              </Typography>
            </Alert>
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
              <Button variant="outlined" onClick={() => navigate('/login')}>
                Go to Login
              </Button>
              {campaignWebsite && (
                <Button variant="contained" onClick={() => window.location.href = campaignWebsite}>
                  Visit Campaign Site
                </Button>
              )}
            </Box>
            
            {/* Version Information */}
            <VersionInfo />
          </CardContent>
        </Card>
      </Container>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ mt: 4, mb: 4 }}>
      <Card>
        <CardContent sx={{ p: 4 }}>
          {/* Campaign Header */}
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <img 
              src={customerConfig.logoUrl} 
              alt={customerConfig.logoAlt} 
              style={{ 
                width: 'auto', 
                maxWidth: '200px', 
                height: '70px', 
                marginBottom: '16px' 
              }} 
            />
            <Typography variant="h4" gutterBottom sx={{ color: '#2f1c6a', fontWeight: 600 }}>
              {registrationTitle}
            </Typography>
            <Typography variant="body1" color="text.secondary">
              {registrationSubtitle}
            </Typography>
          </Box>

          {/* Progress Stepper */}
          <Stepper activeStep={0} sx={{ mb: 4 }}>
            <Step>
              <StepLabel>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <HowToReg fontSize="small" />
                  Register
                </Box>
              </StepLabel>
            </Step>
            <Step>
              <StepLabel>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Schedule fontSize="small" />
                  Admin Review
                </Box>
              </StepLabel>
            </Step>
            <Step>
              <StepLabel>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <CheckCircle fontSize="small" />
                  Start Contributing
                </Box>
              </StepLabel>
            </Step>
          </Stepper>

          <Alert severity="info" sx={{ mb: 3 }}>
            <Typography variant="body2">
              <strong>Volunteer Registration</strong>
              <br />
              All new volunteers are reviewed by our team before account activation. 
              You'll receive an email notification once your application is processed.
            </Typography>
          </Alert>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <form onSubmit={handleSubmit}>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
              <TextField
                label="First Name"
                value={formData.firstName}
                onChange={(e) => handleInputChange('firstName', e.target.value)}
                error={!!validationErrors.firstName}
                helperText={validationErrors.firstName}
                disabled={submitting}
                required
                autoFocus
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Person fontSize="small" />
                    </InputAdornment>
                  )
                }}
              />

              <TextField
                label="Last Name"
                value={formData.lastName}
                onChange={(e) => handleInputChange('lastName', e.target.value)}
                error={!!validationErrors.lastName}
                helperText={validationErrors.lastName}
                disabled={submitting}
                required
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Person fontSize="small" />
                    </InputAdornment>
                  )
                }}
              />
            </Box>

            <TextField
              label="Email Address"
              type="email"
              fullWidth
              margin="normal"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              error={!!validationErrors.email}
              helperText={validationErrors.email || 'We\'ll use this to send you account updates'}
              disabled={submitting}
              required
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Email fontSize="small" />
                  </InputAdornment>
                )
              }}
            />

            <TextField
              label="Phone Number"
              fullWidth
              margin="normal"
              value={formData.phoneNumber}
              onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
              error={!!validationErrors.phoneNumber}
              helperText={validationErrors.phoneNumber || 'Required for team coordination and event notifications'}
              disabled={submitting}
              required
              placeholder="(555) 123-4567"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Phone fontSize="small" />
                  </InputAdornment>
                )
              }}
            />

            <TextField
              label="Password"
              type={showPassword ? 'text' : 'password'}
              fullWidth
              margin="normal"
              value={formData.password}
              onChange={(e) => handleInputChange('password', e.target.value)}
              error={!!validationErrors.password}
              helperText={validationErrors.password || 'Minimum 6 characters'}
              disabled={submitting}
              required
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Lock fontSize="small" />
                  </InputAdornment>
                ),
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      onClick={() => setShowPassword(!showPassword)}
                      edge="end"
                      disabled={submitting}
                    >
                      {showPassword ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                )
              }}
            />

            <TextField
              label="Confirm Password"
              type={showConfirmPassword ? 'text' : 'password'}
              fullWidth
              margin="normal"
              value={formData.confirmPassword}
              onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
              error={!!validationErrors.confirmPassword}
              helperText={validationErrors.confirmPassword}
              disabled={submitting}
              required
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Lock fontSize="small" />
                  </InputAdornment>
                ),
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      aria-label="toggle password visibility"
                      onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                      edge="end"
                      disabled={submitting}
                    >
                      {showConfirmPassword ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                    </IconButton>
                  </InputAdornment>
                )
              }}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              disabled={submitting}
              startIcon={submitting ? <CircularProgress size={20} /> : <HowToReg />}
              sx={{ mt: 3, mb: 2, py: 1.5 }}
            >
              {submitting ? 'Submitting Registration...' : 'Submit Registration'}
            </Button>
          </form>

          <Box sx={{ mt: 3, textAlign: 'center' }}>
            <Typography variant="body2" color="text.secondary">
              Already have an account?{' '}
              <Link
                component="button"
                variant="body2"
                onClick={() => navigate('/login')}
                sx={{ textDecoration: 'none', fontWeight: 500 }}
              >
                Sign in here
              </Link>
            </Typography>
          </Box>

          {/* Campaign Info */}
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
                <Typography variant="caption" align="center" sx={{ mt: 1, display: 'block', color: '#2f1c6a', fontStyle: 'italic' }}>
                  {campaignDisclaimer}
                </Typography>
              )}
            </Box>
          )}

          {/* Version Information */}
          <VersionInfo />
        </CardContent>
      </Card>
    </Container>
  );
};

export default SelfRegistration;