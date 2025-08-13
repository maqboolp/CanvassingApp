import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Button,
  Box,
  Alert,
  CircularProgress,
  TextField,
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  Divider,
} from '@mui/material';
import {
  CheckCircleOutline as CheckIcon,
  EmailOutlined as EmailIcon,
  UnsubscribeOutlined as UnsubscribeIcon,
  NotificationsActiveOutlined as ResubscribeIcon,
} from '@mui/icons-material';
import axios from 'axios';

interface UnsubscribeInfo {
  email: string;
  alreadyUnsubscribed: boolean;
  unsubscribedAt?: string;
  voterName?: string;
  campaignName?: string;
}

const Unsubscribe: React.FC = () => {
  const { token } = useParams<{ token: string }>();
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(false);
  const [info, setInfo] = useState<UnsubscribeInfo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [resubscribed, setResubscribed] = useState(false);
  const [reason, setReason] = useState('');
  const [customReason, setCustomReason] = useState('');

  const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

  useEffect(() => {
    fetchUnsubscribeInfo();
  }, [token]);

  const fetchUnsubscribeInfo = async () => {
    try {
      const response = await axios.get(`${API_BASE_URL}/api/unsubscribe/${token}`);
      setInfo(response.data);
      setError(null);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Invalid unsubscribe link');
      setInfo(null);
    } finally {
      setLoading(false);
    }
  };

  const handleUnsubscribe = async () => {
    setProcessing(true);
    setError(null);

    const unsubscribeReason = reason === 'other' ? customReason : reason;

    try {
      await axios.post(`${API_BASE_URL}/api/unsubscribe/${token}`, {
        reason: unsubscribeReason,
      });
      setSuccess(true);
      if (info) {
        setInfo({ ...info, alreadyUnsubscribed: true });
      }
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to process unsubscribe request');
    } finally {
      setProcessing(false);
    }
  };

  const handleResubscribe = async () => {
    setProcessing(true);
    setError(null);

    try {
      await axios.post(`${API_BASE_URL}/api/unsubscribe/resubscribe/${token}`);
      setResubscribed(true);
      if (info) {
        setInfo({ ...info, alreadyUnsubscribed: false });
      }
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to process resubscribe request');
    } finally {
      setProcessing(false);
    }
  };

  if (loading) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
          <CircularProgress />
          <Typography variant="body1" sx={{ mt: 2 }}>
            Loading unsubscribe information...
          </Typography>
        </Paper>
      </Container>
    );
  }

  if (error && !info) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Paper elevation={3} sx={{ p: 4 }}>
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
          <Typography variant="body1" paragraph>
            The unsubscribe link you clicked appears to be invalid or has expired.
          </Typography>
          <Typography variant="body2" color="text.secondary">
            If you're trying to unsubscribe from our emails, please contact us directly
            or reply to one of our emails with "UNSUBSCRIBE" in the subject line.
          </Typography>
          <Box sx={{ mt: 3 }}>
            <Button component={Link} to="/" variant="contained" fullWidth>
              Return to Homepage
            </Button>
          </Box>
        </Paper>
      </Container>
    );
  }

  if (success) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
          <CheckIcon sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
          <Typography variant="h5" gutterBottom>
            Successfully Unsubscribed
          </Typography>
          <Typography variant="body1" paragraph color="text.secondary">
            You have been removed from our email list.
          </Typography>
          <Typography variant="body2" paragraph>
            Email: <strong>{info?.email}</strong>
          </Typography>
          <Divider sx={{ my: 3 }} />
          <Typography variant="body2" color="text.secondary" paragraph>
            We're sorry to see you go. If you change your mind, you can always
            resubscribe by clicking the button below.
          </Typography>
          <Button
            variant="outlined"
            startIcon={<ResubscribeIcon />}
            onClick={() => {
              setSuccess(false);
              setResubscribed(false);
            }}
            fullWidth
          >
            Resubscribe to Emails
          </Button>
          <Box sx={{ mt: 2 }}>
            <Button component={Link} to="/" variant="text" fullWidth>
              Return to Homepage
            </Button>
          </Box>
        </Paper>
      </Container>
    );
  }

  if (resubscribed) {
    return (
      <Container maxWidth="sm" sx={{ mt: 8 }}>
        <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
          <CheckIcon sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
          <Typography variant="h5" gutterBottom>
            Successfully Resubscribed
          </Typography>
          <Typography variant="body1" paragraph color="text.secondary">
            Welcome back! You've been added back to our email list.
          </Typography>
          <Typography variant="body2" paragraph>
            Email: <strong>{info?.email}</strong>
          </Typography>
          <Box sx={{ mt: 3 }}>
            <Button component={Link} to="/" variant="contained" fullWidth>
              Return to Homepage
            </Button>
          </Box>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="sm" sx={{ mt: 8 }}>
      <Paper elevation={3} sx={{ p: 4 }}>
        <Box sx={{ textAlign: 'center', mb: 3 }}>
          {info?.alreadyUnsubscribed ? (
            <EmailIcon sx={{ fontSize: 64, color: 'text.secondary' }} />
          ) : (
            <UnsubscribeIcon sx={{ fontSize: 64, color: 'warning.main' }} />
          )}
        </Box>

        <Typography variant="h4" gutterBottom align="center">
          {info?.alreadyUnsubscribed ? 'Already Unsubscribed' : 'Unsubscribe from Emails'}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {info && (
          <>
            <Box sx={{ mb: 3, p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Email Address:
              </Typography>
              <Typography variant="body1" fontWeight="bold">
                {info.email}
              </Typography>
              {info.voterName && (
                <>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    Name:
                  </Typography>
                  <Typography variant="body1">{info.voterName}</Typography>
                </>
              )}
              {info.campaignName && (
                <>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    Campaign:
                  </Typography>
                  <Typography variant="body1">{info.campaignName}</Typography>
                </>
              )}
            </Box>

            {info.alreadyUnsubscribed ? (
              <>
                <Alert severity="info" sx={{ mb: 3 }}>
                  This email address is already unsubscribed from our mailing list.
                  {info.unsubscribedAt && (
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      Unsubscribed on: {new Date(info.unsubscribedAt).toLocaleDateString()}
                    </Typography>
                  )}
                </Alert>
                <Typography variant="body2" paragraph color="text.secondary">
                  If you'd like to receive our emails again, you can resubscribe below.
                </Typography>
                <Button
                  variant="contained"
                  color="primary"
                  startIcon={<ResubscribeIcon />}
                  onClick={handleResubscribe}
                  disabled={processing}
                  fullWidth
                >
                  {processing ? 'Processing...' : 'Resubscribe to Emails'}
                </Button>
              </>
            ) : (
              <>
                <Typography variant="body1" paragraph>
                  You are about to unsubscribe from all campaign emails. You will no longer
                  receive updates, newsletters, or other communications from us.
                </Typography>

                <FormControl component="fieldset" sx={{ mb: 3, width: '100%' }}>
                  <FormLabel component="legend">
                    Please let us know why you're unsubscribing (optional):
                  </FormLabel>
                  <RadioGroup
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    sx={{ mt: 1 }}
                  >
                    <FormControlLabel
                      value="too_many"
                      control={<Radio />}
                      label="Too many emails"
                    />
                    <FormControlLabel
                      value="not_relevant"
                      control={<Radio />}
                      label="Content not relevant"
                    />
                    <FormControlLabel
                      value="never_signed_up"
                      control={<Radio />}
                      label="I never signed up"
                    />
                    <FormControlLabel
                      value="other"
                      control={<Radio />}
                      label="Other reason"
                    />
                  </RadioGroup>
                  {reason === 'other' && (
                    <TextField
                      multiline
                      rows={3}
                      placeholder="Please tell us more..."
                      value={customReason}
                      onChange={(e) => setCustomReason(e.target.value)}
                      fullWidth
                      sx={{ mt: 2 }}
                    />
                  )}
                </FormControl>

                <Button
                  variant="contained"
                  color="warning"
                  startIcon={<UnsubscribeIcon />}
                  onClick={handleUnsubscribe}
                  disabled={processing}
                  fullWidth
                  sx={{ mb: 2 }}
                >
                  {processing ? 'Processing...' : 'Confirm Unsubscribe'}
                </Button>

                <Button
                  component={Link}
                  to="/"
                  variant="outlined"
                  fullWidth
                >
                  Cancel - Keep Me Subscribed
                </Button>
              </>
            )}
          </>
        )}
      </Paper>

      <Typography
        variant="body2"
        color="text.secondary"
        align="center"
        sx={{ mt: 3 }}
      >
        If you have any questions or concerns, please contact us at{' '}
        <Link to="/contact">our contact page</Link>.
      </Typography>
    </Container>
  );
};

export default Unsubscribe;