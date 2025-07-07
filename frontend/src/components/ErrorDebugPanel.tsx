import React, { useState } from 'react';
import {
  Drawer,
  Box,
  Typography,
  IconButton,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  Button,
  Divider,
  Paper,
  Collapse,
  TextField,
  InputAdornment,
  Badge,
  Fab
} from '@mui/material';
import {
  BugReport,
  Close,
  ExpandMore,
  ExpandLess,
  Error as ErrorIcon,
  Warning,
  Info,
  Search,
  Delete,
  ContentCopy
} from '@mui/icons-material';
import { errorLoggingService, ErrorLog } from '../services/errorLoggingService';

const ErrorDebugPanel: React.FC = () => {
  const [open, setOpen] = useState(false);
  const [logs, setLogs] = useState<ErrorLog[]>([]);
  const [expandedLog, setExpandedLog] = useState<string | null>(null);
  const [filter, setFilter] = useState('');
  const [filterType, setFilterType] = useState<'all' | 'error' | 'warning' | 'info'>('all');

  const handleOpen = () => {
    setOpen(true);
    setLogs(errorLoggingService.getStoredLogs());
  };

  const handleClose = () => {
    setOpen(false);
  };

  const handleClearLogs = () => {
    if (window.confirm('Are you sure you want to clear all error logs?')) {
      errorLoggingService.clearStoredLogs();
      setLogs([]);
    }
  };

  const toggleExpanded = (logId: string) => {
    setExpandedLog(expandedLog === logId ? null : logId);
  };

  const copyToClipboard = (log: ErrorLog) => {
    const text = JSON.stringify(log, null, 2);
    navigator.clipboard.writeText(text);
  };

  const getIcon = (severity: string) => {
    switch (severity) {
      case 'error':
        return <ErrorIcon color="error" />;
      case 'warning':
        return <Warning color="warning" />;
      case 'info':
        return <Info color="info" />;
      default:
        return <Info />;
    }
  };

  const filteredLogs = logs.filter(log => {
    if (filterType !== 'all' && log.severity !== filterType) {
      return false;
    }
    if (filter) {
      const searchLower = filter.toLowerCase();
      return (
        log.message.toLowerCase().includes(searchLower) ||
        log.type.toLowerCase().includes(searchLower) ||
        (log.correlationId && log.correlationId.toLowerCase().includes(searchLower))
      );
    }
    return true;
  });

  const errorCount = logs.filter(l => l.severity === 'error').length;
  const warningCount = logs.filter(l => l.severity === 'warning').length;

  // Only show in development
  if (process.env.NODE_ENV !== 'development') {
    return null;
  }

  return (
    <>
      <Fab
        color="secondary"
        size="small"
        onClick={handleOpen}
        sx={{
          position: 'fixed',
          bottom: 16,
          right: 16,
          zIndex: 9999
        }}
      >
        <Badge badgeContent={errorCount} color="error">
          <BugReport />
        </Badge>
      </Fab>

      <Drawer
        anchor="right"
        open={open}
        onClose={handleClose}
        sx={{
          '& .MuiDrawer-paper': {
            width: 400,
            maxWidth: '100%'
          }
        }}
      >
        <Box sx={{ p: 2 }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6">Error Debug Panel</Typography>
            <IconButton onClick={handleClose}>
              <Close />
            </IconButton>
          </Box>

          <Box display="flex" gap={1} mb={2}>
            <Chip
              label={`All (${logs.length})`}
              onClick={() => setFilterType('all')}
              color={filterType === 'all' ? 'primary' : 'default'}
              size="small"
            />
            <Chip
              label={`Errors (${errorCount})`}
              onClick={() => setFilterType('error')}
              color={filterType === 'error' ? 'error' : 'default'}
              size="small"
            />
            <Chip
              label={`Warnings (${warningCount})`}
              onClick={() => setFilterType('warning')}
              color={filterType === 'warning' ? 'warning' : 'default'}
              size="small"
            />
          </Box>

          <TextField
            fullWidth
            size="small"
            placeholder="Search logs..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <Search />
                </InputAdornment>
              )
            }}
            sx={{ mb: 2 }}
          />

          <Button
            fullWidth
            variant="outlined"
            color="error"
            startIcon={<Delete />}
            onClick={handleClearLogs}
            sx={{ mb: 2 }}
          >
            Clear All Logs
          </Button>

          <Divider />

          <List sx={{ mt: 2 }}>
            {filteredLogs.length === 0 ? (
              <ListItem>
                <ListItemText
                  primary="No logs found"
                  secondary="Errors will appear here when they occur"
                />
              </ListItem>
            ) : (
              filteredLogs.reverse().map((log, index) => {
                const logId = `${log.timestamp}-${index}`;
                return (
                  <Paper key={logId} sx={{ mb: 1, p: 1 }}>
                    <ListItem
                      button
                      onClick={() => toggleExpanded(logId)}
                      sx={{ p: 1 }}
                    >
                      <ListItemIcon>
                        {getIcon(log.severity)}
                      </ListItemIcon>
                      <ListItemText
                        primary={
                          <Box>
                            <Typography variant="body2" noWrap>
                              {log.message}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {new Date(log.timestamp).toLocaleTimeString()} - {log.type}
                            </Typography>
                          </Box>
                        }
                      />
                      {expandedLog === logId ? <ExpandLess /> : <ExpandMore />}
                    </ListItem>

                    <Collapse in={expandedLog === logId}>
                      <Box sx={{ p: 2, backgroundColor: 'grey.50' }}>
                        <Typography variant="caption" component="div">
                          <strong>URL:</strong> {log.context.url}
                        </Typography>
                        {log.correlationId && (
                          <Typography variant="caption" component="div">
                            <strong>Correlation ID:</strong> {log.correlationId}
                          </Typography>
                        )}
                        {log.context.userId && (
                          <Typography variant="caption" component="div">
                            <strong>User ID:</strong> {log.context.userId}
                          </Typography>
                        )}
                        {log.stack && (
                          <Box mt={1}>
                            <Typography variant="caption" component="div">
                              <strong>Stack Trace:</strong>
                            </Typography>
                            <pre style={{
                              fontSize: '0.7rem',
                              overflow: 'auto',
                              maxHeight: 200,
                              backgroundColor: '#f5f5f5',
                              padding: '4px',
                              borderRadius: '4px'
                            }}>
                              {log.stack}
                            </pre>
                          </Box>
                        )}
                        {log.metadata && (
                          <Box mt={1}>
                            <Typography variant="caption" component="div">
                              <strong>Metadata:</strong>
                            </Typography>
                            <pre style={{
                              fontSize: '0.7rem',
                              overflow: 'auto',
                              backgroundColor: '#f5f5f5',
                              padding: '4px',
                              borderRadius: '4px'
                            }}>
                              {JSON.stringify(log.metadata, null, 2)}
                            </pre>
                          </Box>
                        )}
                        <Button
                          size="small"
                          startIcon={<ContentCopy />}
                          onClick={() => copyToClipboard(log)}
                          sx={{ mt: 1 }}
                        >
                          Copy to Clipboard
                        </Button>
                      </Box>
                    </Collapse>
                  </Paper>
                );
              })
            )}
          </List>
        </Box>
      </Drawer>
    </>
  );
};

export default ErrorDebugPanel;