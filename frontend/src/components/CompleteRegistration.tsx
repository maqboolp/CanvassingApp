import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
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
  Stepper,
  Step,
  StepLabel
} from '@mui/material';
import {
  Visibility,
  VisibilityOff,
  CheckCircle,
  Email,
  Person,
  Phone,
  Lock
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';

const CompleteRegistration: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [invitationDetails, setInvitationDetails] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    phoneNumber: '',
    password: '',
    confirmPassword: ''
  });

  const [validationErrors, setValidationErrors] = useState<any>({});

  useEffect(() => {
    if (!token) {
      setError('Invalid or missing invitation token');
      setLoading(false);
      return;
    }

    fetchInvitationDetails();
  }, [token]);

  const fetchInvitationDetails = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/registration/invitation/${token}`);
      
      if (response.ok) {
        const data = await response.json();
        setInvitationDetails(data);
      } else {
        const errorData = await response.json();
        setError(errorData.error || 'Invalid invitation token');
      }
    } catch (err) {
      setError('Failed to load invitation details');
    } finally {
      setLoading(false);
    }
  };

  const validateForm = () => {
    const errors: any = {};

    if (!formData.firstName.trim()) {
      errors.firstName = 'First name is required';
    }

    if (!formData.lastName.trim()) {
      errors.lastName = 'Last name is required';
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
      const response = await fetch(`${API_BASE_URL}/api/registration/complete-invitation`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          token,
          firstName: formData.firstName.trim(),
          lastName: formData.lastName.trim(),
          phoneNumber: formData.phoneNumber.trim(),
          password: formData.password
        })
      });

      if (response.ok) {
        setSuccess(true);
        setTimeout(() => {
          navigate('/login', { 
            state: { 
              message: 'Registration completed successfully! Please log in with your credentials.' 
            }
          });
        }, 3000);
      } else {
        const errorData = await response.json();
        setError(errorData.error || 'Failed to complete registration');
      }
    } catch (err) {
      setError('Failed to complete registration. Please try again.');
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

  if (loading) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Container>
    );
  }

  if (error && !invitationDetails) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 4 }}>
            <Typography variant="h5" color="error" gutterBottom>
              Invalid Invitation
            </Typography>
            <Typography variant="body1" color="text.secondary" paragraph>
              {error}
            </Typography>
            <Button variant="outlined" onClick={() => navigate('/login')}>
              Go to Login
            </Button>
          </CardContent>
        </Card>
      </Container>
    );
  }

  if (success) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 4 }}>
            <CheckCircle sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
            <Typography variant="h5" gutterBottom>
              Registration Complete!
            </Typography>
            <Typography variant="body1" color="text.secondary" paragraph>
              Your account has been created successfully. You will be redirected to the login page in a moment.
            </Typography>
            <CircularProgress size={24} sx={{ mt: 2 }} />
          </CardContent>
        </Card>
      </Container>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ mt: 4, mb: 4 }}>
      <Card>
        <CardContent sx={{ p: 4 }}>
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Typography variant="h4" gutterBottom sx={{ color: '#673ab7' }}>
              Complete Your Registration
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Welcome to the Tanveer for Hoover Campaign Team!
            </Typography>
          </Box>

          {/* Progress Stepper */}
          <Stepper activeStep={1} sx={{ mb: 4 }}>
            <Step completed>
              <StepLabel>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Email fontSize="small" />
                  Invitation Sent
                </Box>
              </StepLabel>
            </Step>
            <Step>
              <StepLabel>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Person fontSize="small" />
                  Complete Details
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

          {/* Invitation Details */}
          <Alert severity="info" sx={{ mb: 3 }}>
            <Typography variant="body2">
              <strong>Email:</strong> {invitationDetails?.email}
              <br />
              <strong>Role:</strong> {invitationDetails?.role}
              <br />
              <strong>Expires:</strong> {new Date(invitationDetails?.expiresAt).toLocaleDateString()}
            </Typography>
          </Alert>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <form onSubmit={handleSubmit}>
            <TextField
              label="First Name"
              fullWidth
              margin="normal"
              value={formData.firstName}
              onChange={(e) => handleInputChange('firstName', e.target.value)}
              error={!!validationErrors.firstName}
              helperText={validationErrors.firstName}
              disabled={submitting}
              required
              autoFocus
            />

            <TextField
              label="Last Name"
              fullWidth
              margin="normal"
              value={formData.lastName}
              onChange={(e) => handleInputChange('lastName', e.target.value)}
              error={!!validationErrors.lastName}
              helperText={validationErrors.lastName}
              disabled={submitting}
              required
            />

            <TextField
              label="Phone Number"
              fullWidth
              margin="normal"
              value={formData.phoneNumber}
              onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
              error={!!validationErrors.phoneNumber}
              helperText={validationErrors.phoneNumber || 'Required for team coordination'}
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
              type={showPassword ? 'text' : 'password'}
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
                )
              }}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              disabled={submitting}
              startIcon={submitting ? <CircularProgress size={20} /> : <CheckCircle />}
              sx={{ mt: 3, mb: 2, py: 1.5 }}
            >
              {submitting ? 'Creating Account...' : 'Complete Registration'}
            </Button>
          </form>

          <Box sx={{ mt: 3, p: 2, bgcolor: 'action.hover', borderRadius: 1 }}>
            <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
              🎉 You're joining a dedicated team working to bring positive change to Hoover!
              <br />
              After registration, you'll be able to participate in voter outreach and canvassing.
            </Typography>
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
};

export default CompleteRegistration;