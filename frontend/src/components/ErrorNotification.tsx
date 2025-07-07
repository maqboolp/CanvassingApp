import React, { useState, useEffect } from 'react';
import {
  Snackbar,
  Alert,
  AlertTitle,
  IconButton,
  Collapse,
  Box,
  Typography,
  Button
} from '@mui/material';
import {
  Close as CloseIcon,
  ExpandMore,
  ExpandLess,
  ErrorOutline,
  Warning,
  Info
} from '@mui/icons-material';
import { ApiError, ApiErrorHandler } from '../utils/apiErrorHandler';
import { errorLoggingService } from '../services/errorLoggingService';

interface ErrorNotificationProps {
  // Optional props for customization
  autoHideDuration?: number;
  maxErrors?: number;
}

interface ErrorDetails {
  error: ApiError;
  id: string;
  timestamp: Date;
}

const ErrorNotification: React.FC<ErrorNotificationProps> = ({
  autoHideDuration = 6000,
  maxErrors = 3
}) => {
  const [errors, setErrors] = useState<ErrorDetails[]>([]);
  const [expandedError, setExpandedError] = useState<string | null>(null);

  useEffect(() => {
    // Register error callback
    const handleError = (error: ApiError) => {
      const errorDetail: ErrorDetails = {
        error,
        id: `${Date.now()}-${Math.random()}`,
        timestamp: new Date()
      };

      setErrors(prev => {
        // Keep only the latest maxErrors
        const newErrors = [errorDetail, ...prev].slice(0, maxErrors);
        return newErrors;
      });

      // Log the error
      errorLoggingService.logError(
        error.message,
        error,
        error.type || 'ApiError',
        {
          status: error.status,
          correlationId: error.correlationId
        }
      );
    };

    ApiErrorHandler.registerErrorCallback(handleError);

    return () => {
      ApiErrorHandler.unregisterErrorCallback(handleError);
    };
  }, [maxErrors]);

  const handleClose = (id: string) => {
    setErrors(prev => prev.filter(e => e.id !== id));
    if (expandedError === id) {
      setExpandedError(null);
    }
  };

  const getErrorIcon = (error: ApiError) => {
    if (error.status >= 500) {
      return <ErrorOutline />;
    } else if (error.status >= 400) {
      return <Warning />;
    }
    return <Info />;
  };

  const getSeverity = (error: ApiError): 'error' | 'warning' | 'info' => {
    if (error.status >= 500 || error.status === 0) {
      return 'error';
    } else if (error.status >= 400) {
      return 'warning';
    }
    return 'info';
  };

  const toggleExpanded = (id: string) => {
    setExpandedError(expandedError === id ? null : id);
  };

  const handleRetry = () => {
    // This could be extended to actually retry the failed request
    window.location.reload();
  };

  if (errors.length === 0) {
    return null;
  }

  return (
    <>
      {errors.map((errorDetail, index) => (
        <Snackbar
          key={errorDetail.id}
          open={true}
          autoHideDuration={errorDetail.error.status >= 500 ? null : autoHideDuration}
          onClose={() => handleClose(errorDetail.id)}
          anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
          style={{ top: `${24 + index * 80}px` }}
        >
          <Alert
            severity={getSeverity(errorDetail.error)}
            icon={getErrorIcon(errorDetail.error)}
            action={
              <Box display="flex" alignItems="center">
                {errorDetail.error.shouldShowDetails() && errorDetail.error.errors && (
                  <IconButton
                    size="small"
                    color="inherit"
                    onClick={() => toggleExpanded(errorDetail.id)}
                  >
                    {expandedError === errorDetail.id ? <ExpandLess /> : <ExpandMore />}
                  </IconButton>
                )}
                <IconButton
                  size="small"
                  color="inherit"
                  onClick={() => handleClose(errorDetail.id)}
                >
                  <CloseIcon fontSize="small" />
                </IconButton>
              </Box>
            }
            sx={{ minWidth: 300, maxWidth: 500 }}
          >
            <AlertTitle>
              {errorDetail.error.type === 'ValidationError' && 'Validation Error'}
              {errorDetail.error.type === 'NotFoundError' && 'Not Found'}
              {errorDetail.error.type === 'UnauthorizedError' && 'Unauthorized'}
              {errorDetail.error.type === 'ForbiddenError' && 'Access Denied'}
              {errorDetail.error.type === 'ExternalServiceError' && 'Service Error'}
              {errorDetail.error.type === 'TimeoutError' && 'Request Timeout'}
              {errorDetail.error.type === 'InternalServerError' && 'Server Error'}
              {!errorDetail.error.type && 'Error'}
            </AlertTitle>
            
            <Typography variant="body2">
              {errorDetail.error.message}
            </Typography>

            <Collapse in={expandedError === errorDetail.id}>
              <Box mt={1}>
                {errorDetail.error.errors && (
                  <Box mb={1}>
                    <Typography variant="caption" color="text.secondary">
                      Validation Errors:
                    </Typography>
                    {Object.entries(errorDetail.error.errors).map(([field, messages]) => (
                      <Box key={field} ml={1}>
                        <Typography variant="caption">
                          <strong>{field}:</strong> {messages.join(', ')}
                        </Typography>
                      </Box>
                    ))}
                  </Box>
                )}
                
                {errorDetail.error.correlationId && (
                  <Typography variant="caption" color="text.secondary">
                    Error ID: {errorDetail.error.correlationId}
                  </Typography>
                )}
                
                {errorDetail.error.status === 0 && (
                  <Box mt={1}>
                    <Button size="small" onClick={handleRetry}>
                      Retry
                    </Button>
                  </Box>
                )}
              </Box>
            </Collapse>
          </Alert>
        </Snackbar>
      ))}
    </>
  );
};

export default ErrorNotification;