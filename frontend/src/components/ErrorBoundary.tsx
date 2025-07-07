import React, { Component, ErrorInfo, ReactNode } from 'react';
import {
  Box,
  Container,
  Typography,
  Button,
  Card,
  CardContent,
  CardActions,
  Alert,
  Collapse,
  IconButton
} from '@mui/material';
import {
  Error as ErrorIcon,
  Refresh,
  Home,
  ExpandMore,
  ExpandLess,
  BugReport
} from '@mui/icons-material';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  showDetails: boolean;
  errorId: string;
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      showDetails: false,
      errorId: ''
    };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    // Generate error ID for tracking
    const errorId = `ERR-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    
    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
      console.error('Error caught by boundary:', error);
    }

    return {
      hasError: true,
      errorId
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error details
    console.error('Error Boundary caught an error:', {
      error: error.toString(),
      errorInfo: errorInfo.componentStack,
      errorId: this.state.errorId,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      url: window.location.href
    });

    // Update state with error details
    this.setState({
      error,
      errorInfo
    });

    // In production, you might want to send this to an error tracking service
    if (process.env.NODE_ENV === 'production') {
      // Example: Send to error tracking service
      // errorTrackingService.logError(error, errorInfo, this.state.errorId);
    }
  }

  handleReset = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      showDetails: false,
      errorId: ''
    });
  };

  handleRefresh = () => {
    window.location.reload();
  };

  handleGoHome = () => {
    window.location.href = '/';
  };

  toggleDetails = () => {
    this.setState(prevState => ({
      showDetails: !prevState.showDetails
    }));
  };

  render() {
    if (this.state.hasError) {
      // Use custom fallback if provided
      if (this.props.fallback) {
        return <>{this.props.fallback}</>;
      }

      // Default error UI
      return (
        <Container maxWidth="sm" sx={{ mt: 8 }}>
          <Card elevation={3}>
            <CardContent>
              <Box display="flex" alignItems="center" mb={2}>
                <ErrorIcon color="error" sx={{ fontSize: 40, mr: 2 }} />
                <Typography variant="h5" component="h1">
                  Oops! Something went wrong
                </Typography>
              </Box>

              <Alert severity="error" sx={{ mb: 2 }}>
                We're sorry, but something unexpected happened. The error has been logged 
                and we'll look into it.
              </Alert>

              <Typography variant="body2" color="text.secondary" paragraph>
                You can try refreshing the page or going back to the home page. 
                If the problem persists, please contact support.
              </Typography>

              {process.env.NODE_ENV === 'development' && (
                <Box>
                  <Button
                    onClick={this.toggleDetails}
                    startIcon={this.state.showDetails ? <ExpandLess /> : <ExpandMore />}
                    size="small"
                    sx={{ mb: 1 }}
                  >
                    {this.state.showDetails ? 'Hide' : 'Show'} Error Details
                  </Button>

                  <Collapse in={this.state.showDetails}>
                    <Alert severity="warning" sx={{ mb: 2 }}>
                      <Typography variant="caption" component="div">
                        <strong>Error ID:</strong> {this.state.errorId}
                      </Typography>
                      <Typography variant="caption" component="div">
                        <strong>Error:</strong> {this.state.error?.toString()}
                      </Typography>
                      {this.state.error?.stack && (
                        <Box mt={1}>
                          <Typography variant="caption" component="div">
                            <strong>Stack Trace:</strong>
                          </Typography>
                          <pre style={{ 
                            fontSize: '0.75rem', 
                            overflow: 'auto',
                            backgroundColor: '#f5f5f5',
                            padding: '8px',
                            borderRadius: '4px'
                          }}>
                            {this.state.error.stack}
                          </pre>
                        </Box>
                      )}
                      {this.state.errorInfo?.componentStack && (
                        <Box mt={1}>
                          <Typography variant="caption" component="div">
                            <strong>Component Stack:</strong>
                          </Typography>
                          <pre style={{ 
                            fontSize: '0.75rem', 
                            overflow: 'auto',
                            backgroundColor: '#f5f5f5',
                            padding: '8px',
                            borderRadius: '4px'
                          }}>
                            {this.state.errorInfo.componentStack}
                          </pre>
                        </Box>
                      )}
                    </Alert>
                  </Collapse>
                </Box>
              )}
            </CardContent>

            <CardActions sx={{ justifyContent: 'center', pb: 2 }}>
              <Button
                variant="contained"
                color="primary"
                startIcon={<Refresh />}
                onClick={this.handleRefresh}
              >
                Refresh Page
              </Button>
              <Button
                variant="outlined"
                startIcon={<Home />}
                onClick={this.handleGoHome}
              >
                Go to Home
              </Button>
              {process.env.NODE_ENV === 'development' && (
                <IconButton
                  color="default"
                  onClick={this.handleReset}
                  title="Reset error boundary (dev only)"
                >
                  <BugReport />
                </IconButton>
              )}
            </CardActions>
          </Card>

          <Typography 
            variant="caption" 
            color="text.secondary" 
            align="center" 
            display="block"
            sx={{ mt: 2 }}
          >
            Error ID: {this.state.errorId}
          </Typography>
        </Container>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;