import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
  FormControlLabel,
  Checkbox,
  Link
} from '@mui/material';
import {
  Phone,
  Person,
  Email,
  LocationOn,
  Sms,
  CheckCircle
} from '@mui/icons-material';
import { OptInRequest } from '../types';
import { optInService } from '../services/optInService';
import VersionInfo from './VersionInfo';

const OptInForm: React.FC = () => {
  const navigate = useNavigate();
  
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  
  const [formData, setFormData] = useState<OptInRequest>({
    phoneNumber: '',
    consentGiven: false,
    firstName: '',
    lastName: '',
    email: '',
    zipCode: ''
  });

  const [validationErrors, setValidationErrors] = useState<any>({});

  const validateForm = () => {
    const errors: any = {};

    if (!formData.phoneNumber.trim()) {
      errors.phoneNumber = 'Phone number is required';
    } else if (!/^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$/.test(formData.phoneNumber)) {
      errors.phoneNumber = 'Please enter a valid phone number';
    }

    if (!formData.consentGiven) {
      errors.consent = 'You must agree to receive messages to continue';
    }

    // Optional fields validation
    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = 'Please enter a valid email address';
    }

    if (formData.zipCode && !/^\d{5}(-\d{4})?$/.test(formData.zipCode)) {
      errors.zipCode = 'Please enter a valid ZIP code';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleInputChange = (field: keyof OptInRequest, value: any) => {
    setFormData({ ...formData, [field]: value });
    // Clear validation error for this field
    if (validationErrors[field]) {
      setValidationErrors({ ...validationErrors, [field]: null });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const response = await optInService.submitOptIn(formData);
      
      if (response.success) {
        setSuccess(true);
      } else {
        setError(response.message || 'Failed to complete opt-in');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setSubmitting(false);
    }
  };

  if (success) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Card sx={{ p: 2, textAlign: 'center' }}>
          <CardContent>
            <CheckCircle sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
            <Typography variant="h4" gutterBottom color="success.main" fontWeight={600}>
              Successfully Opted In!
            </Typography>
            <Typography variant="body1" sx={{ mb: 3 }}>
              Thank you for joining Tanveer for Hoover's campaign updates!
            </Typography>
            <Alert severity="success" sx={{ mb: 3, textAlign: 'left' }}>
              <Typography variant="body2">
                You'll receive a welcome message shortly. You can opt out at any time by replying STOP to any message.
              </Typography>
            </Alert>
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
              <Button
                variant="contained"
                onClick={() => navigate('/')}
                sx={{ 
                  backgroundColor: '#2f1c6a',
                  '&:hover': {
                    backgroundColor: '#241555'
                  }
                }}
              >
                Visit Campaign Website
              </Button>
              <Button
                variant="outlined"
                onClick={() => window.location.reload()}
                sx={{ 
                  color: '#2f1c6a',
                  borderColor: '#2f1c6a',
                  '&:hover': {
                    borderColor: '#241555',
                    backgroundColor: 'rgba(47, 28, 106, 0.04)'
                  }
                }}
              >
                Add Another Number
              </Button>
            </Box>
          </CardContent>
        </Card>
        <VersionInfo />
      </Container>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ mt: 4 }}>
      <Card sx={{ p: 2 }}>
        <CardContent>
          <Box sx={{ textAlign: 'center', mb: 4 }}>
            <Box
              component="img"
              src="/campaign-logo.png"
              alt="Tanveer for Hoover"
              sx={{ 
                height: 100, 
                mb: 2,
                objectFit: 'contain'
              }} 
            />
            <Typography variant="h4" gutterBottom sx={{ color: '#2f1c6a', fontWeight: 600 }}>
              Stay Connected
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Get campaign updates and event notifications via text message
            </Typography>
          </Box>

          <Alert severity="info" sx={{ mb: 3 }}>
            <Typography variant="body2">
              <strong>Privacy Promise:</strong> Your information is secure and will only be used for campaign communications. 
              Standard message and data rates may apply.
            </Typography>
          </Alert>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <form onSubmit={handleSubmit}>
            <TextField
              label="Phone Number"
              type="tel"
              fullWidth
              margin="normal"
              value={formData.phoneNumber}
              onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
              error={!!validationErrors.phoneNumber}
              helperText={validationErrors.phoneNumber || 'We\'ll use this to send you campaign updates'}
              disabled={submitting}
              required
              autoFocus
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Phone fontSize="small" />
                  </InputAdornment>
                )
              }}
            />

            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2, mt: 2 }}>
              <TextField
                label="First Name (Optional)"
                value={formData.firstName}
                onChange={(e) => handleInputChange('firstName', e.target.value)}
                disabled={submitting}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Person fontSize="small" />
                    </InputAdornment>
                  )
                }}
              />

              <TextField
                label="Last Name (Optional)"
                value={formData.lastName}
                onChange={(e) => handleInputChange('lastName', e.target.value)}
                disabled={submitting}
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
              label="Email Address (Optional)"
              type="email"
              fullWidth
              margin="normal"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              error={!!validationErrors.email}
              helperText={validationErrors.email}
              disabled={submitting}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Email fontSize="small" />
                  </InputAdornment>
                )
              }}
            />

            <TextField
              label="ZIP Code (Optional)"
              value={formData.zipCode}
              onChange={(e) => handleInputChange('zipCode', e.target.value)}
              error={!!validationErrors.zipCode}
              helperText={validationErrors.zipCode}
              disabled={submitting}
              fullWidth
              margin="normal"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <LocationOn fontSize="small" />
                  </InputAdornment>
                )
              }}
            />

            <FormControlLabel
              control={
                <Checkbox
                  checked={formData.consentGiven}
                  onChange={(e) => handleInputChange('consentGiven', e.target.checked)}
                  color="primary"
                  disabled={submitting}
                />
              }
              label={
                <Typography variant="body2" color={validationErrors.consent ? 'error' : 'textSecondary'}>
                  I agree to receive texts and robocalls from Tanveer for Hoover. 
                  Message and data rates may apply. Reply STOP to opt out.
                </Typography>
              }
              sx={{ mt: 2, mb: 1 }}
            />
            {validationErrors.consent && (
              <Typography variant="caption" color="error" sx={{ ml: 4 }}>
                {validationErrors.consent}
              </Typography>
            )}

            <Button
              type="submit"
              fullWidth
              variant="contained"
              sx={{ 
                mt: 3,
                mb: 2,
                py: 1.5,
                backgroundColor: '#2f1c6a',
                '&:hover': {
                  backgroundColor: '#241555'
                }
              }}
              disabled={submitting}
              startIcon={submitting ? <CircularProgress size={20} /> : <Sms />}
            >
              {submitting ? 'Opting In...' : 'Opt In to Receive Messages'}
            </Button>

            <Box sx={{ textAlign: 'center', mt: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Want to volunteer instead?{' '}
                <Link
                  component="button"
                  type="button"
                  onClick={() => navigate('/register')}
                  sx={{ color: '#2f1c6a', fontWeight: 500 }}
                >
                  Sign up here
                </Link>
              </Typography>
            </Box>

            <Box sx={{ textAlign: 'center', mt: 3, pt: 2, borderTop: 1, borderColor: 'divider' }}>
              <Typography variant="caption" color="text.secondary">
                You can also text JOIN to (205) 922-7271 to opt in
                <br />
                Reply STOP to opt out at any time
              </Typography>
            </Box>
          </form>
        </CardContent>
      </Card>
      <VersionInfo />
    </Container>
  );
};

export default OptInForm;