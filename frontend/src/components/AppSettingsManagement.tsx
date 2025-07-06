import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  Chip,
  CircularProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Switch,
  FormControlLabel,
  Tooltip,
  InputAdornment
} from '@mui/material';
import {
  Edit,
  Save,
  Cancel,
  ExpandMore,
  Visibility,
  VisibilityOff,
  CloudUpload,
  Lock,
  Public,
  Settings as SettingsIcon
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { AuthUser } from '../types';

interface AppSetting {
  id: number;
  key: string;
  value: string;
  description: string;
  category: string;
  isPublic: boolean;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

interface AppSettingsManagementProps {
  user: AuthUser;
}

const AppSettingsManagement: React.FC<AppSettingsManagementProps> = ({ user }) => {
  const [settings, setSettings] = useState<AppSetting[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<{ [key: number]: boolean }>({});
  const [editingValues, setEditingValues] = useState<{ [key: number]: string }>({});
  const [editingIds, setEditingIds] = useState<Set<number>>(new Set());
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [showSecrets, setShowSecrets] = useState<{ [key: number]: boolean }>({});
  const [migrating, setMigrating] = useState(false);
  const [migrationResult, setMigrationResult] = useState<any>(null);

  useEffect(() => {
    fetchSettings();
  }, []);

  const fetchSettings = async () => {
    try {
      setLoading(true);
      const response = await fetch(`${API_BASE_URL}/api/appsettings/all`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        setSettings(data);
      } else {
        setError('Failed to fetch settings');
      }
    } catch (err) {
      setError('Failed to fetch settings');
    } finally {
      setLoading(false);
    }
  };

  const handleMigrateFromEnv = async () => {
    if (!window.confirm('This will import all available settings from environment variables. Continue?')) {
      return;
    }

    setMigrating(true);
    setError(null);
    setMigrationResult(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/appsettings/migrate-from-env`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        const result = await response.json();
        setMigrationResult(result);
        setSuccess(`Successfully migrated ${result.migratedCount} settings from environment variables`);
        fetchSettings();
      } else {
        const errorData = await response.text();
        setError(errorData || 'Failed to migrate settings');
      }
    } catch (err) {
      setError('Failed to migrate settings');
    } finally {
      setMigrating(false);
    }
  };

  const handleEdit = (settingId: number) => {
    const setting = settings.find(s => s.id === settingId);
    if (setting) {
      setEditingValues(prev => ({ ...prev, [settingId]: setting.value }));
      setEditingIds(prev => new Set(prev).add(settingId));
    }
  };

  const handleCancel = (settingId: number) => {
    setEditingIds(prev => {
      const newSet = new Set(prev);
      newSet.delete(settingId);
      return newSet;
    });
    setEditingValues(prev => {
      const newValues = { ...prev };
      delete newValues[settingId];
      return newValues;
    });
  };

  const handleSave = async (setting: AppSetting) => {
    setSaving(prev => ({ ...prev, [setting.id]: true }));
    setError(null);

    try {
      const response = await fetch(`${API_BASE_URL}/api/appsettings/${setting.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          ...setting,
          value: editingValues[setting.id]
        })
      });

      if (response.ok) {
        setSuccess('Setting updated successfully');
        handleCancel(setting.id);
        fetchSettings();
      } else {
        const errorData = await response.text();
        setError(errorData || 'Failed to update setting');
      }
    } catch (err) {
      setError('Failed to update setting');
    } finally {
      setSaving(prev => ({ ...prev, [setting.id]: false }));
    }
  };

  const isSecretKey = (key: string): boolean => {
    const secretPatterns = ['KEY', 'SECRET', 'TOKEN', 'PASSWORD', 'AUTH', 'CREDENTIAL'];
    return secretPatterns.some(pattern => key.toUpperCase().includes(pattern));
  };

  const toggleShowSecret = (settingId: number) => {
    setShowSecrets(prev => ({ ...prev, [settingId]: !prev[settingId] }));
  };

  const groupedSettings = settings.reduce((acc, setting) => {
    if (!acc[setting.category]) {
      acc[setting.category] = [];
    }
    acc[setting.category].push(setting);
    return acc;
  }, {} as { [category: string]: AppSetting[] });

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Application Settings</Typography>
        <Button
          variant="contained"
          startIcon={migrating ? <CircularProgress size={20} /> : <CloudUpload />}
          onClick={handleMigrateFromEnv}
          disabled={migrating}
        >
          {migrating ? 'Migrating...' : 'Migrate from Environment'}
        </Button>
      </Box>

      {success && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {migrationResult && (
        <Alert severity="info" sx={{ mb: 2 }} onClose={() => setMigrationResult(null)}>
          <Typography variant="subtitle2">Migration Complete</Typography>
          <Typography variant="body2">
            Migrated {migrationResult.migratedCount} settings: {migrationResult.migratedKeys.join(', ')}
          </Typography>
        </Alert>
      )}

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="body2" color="text.secondary">
            Manage application settings that were previously stored in environment variables. 
            Settings marked as public will be accessible without authentication.
          </Typography>
        </CardContent>
      </Card>

      {Object.entries(groupedSettings).map(([category, categorySettings]) => (
        <Accordion key={category} defaultExpanded>
          <AccordionSummary expandIcon={<ExpandMore />}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <SettingsIcon color="primary" />
              <Typography variant="h6">{category}</Typography>
              <Chip label={categorySettings.length} size="small" color="primary" />
            </Box>
          </AccordionSummary>
          <AccordionDetails>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Key</TableCell>
                    <TableCell>Value</TableCell>
                    <TableCell>Description</TableCell>
                    <TableCell align="center">Visibility</TableCell>
                    <TableCell align="center">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {categorySettings.map((setting) => {
                    const isEditing = editingIds.has(setting.id);
                    const isSecret = isSecretKey(setting.key);
                    const showValue = !isSecret || showSecrets[setting.id];

                    return (
                      <TableRow key={setting.id}>
                        <TableCell>
                          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                            {setting.key}
                          </Typography>
                        </TableCell>
                        <TableCell sx={{ maxWidth: 300 }}>
                          {isEditing ? (
                            <TextField
                              value={editingValues[setting.id] || ''}
                              onChange={(e) => setEditingValues(prev => ({
                                ...prev,
                                [setting.id]: e.target.value
                              }))}
                              size="small"
                              fullWidth
                              multiline
                              maxRows={3}
                              InputProps={{
                                endAdornment: isSecret ? (
                                  <InputAdornment position="end">
                                    <IconButton
                                      size="small"
                                      onClick={() => toggleShowSecret(setting.id)}
                                    >
                                      {showValue ? <VisibilityOff /> : <Visibility />}
                                    </IconButton>
                                  </InputAdornment>
                                ) : null
                              }}
                              type={isSecret && !showValue ? 'password' : 'text'}
                            />
                          ) : (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Typography
                                variant="body2"
                                sx={{
                                  fontFamily: 'monospace',
                                  wordBreak: 'break-all',
                                  filter: isSecret && !showValue ? 'blur(5px)' : 'none',
                                  cursor: isSecret ? 'pointer' : 'default'
                                }}
                                onClick={() => isSecret && toggleShowSecret(setting.id)}
                              >
                                {isSecret && !showValue ? '••••••••' : setting.value}
                              </Typography>
                              {isSecret && (
                                <IconButton
                                  size="small"
                                  onClick={() => toggleShowSecret(setting.id)}
                                >
                                  {showValue ? <VisibilityOff /> : <Visibility />}
                                </IconButton>
                              )}
                            </Box>
                          )}
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" color="text.secondary">
                            {setting.description || '-'}
                          </Typography>
                        </TableCell>
                        <TableCell align="center">
                          <Tooltip title={setting.isPublic ? 'Public' : 'Private'}>
                            {setting.isPublic ? (
                              <Public color="success" />
                            ) : (
                              <Lock color="action" />
                            )}
                          </Tooltip>
                        </TableCell>
                        <TableCell align="center">
                          {isEditing ? (
                            <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                              <IconButton
                                size="small"
                                color="primary"
                                onClick={() => handleSave(setting)}
                                disabled={saving[setting.id]}
                              >
                                {saving[setting.id] ? <CircularProgress size={20} /> : <Save />}
                              </IconButton>
                              <IconButton
                                size="small"
                                color="default"
                                onClick={() => handleCancel(setting.id)}
                                disabled={saving[setting.id]}
                              >
                                <Cancel />
                              </IconButton>
                            </Box>
                          ) : (
                            <IconButton
                              size="small"
                              color="primary"
                              onClick={() => handleEdit(setting.id)}
                            >
                              <Edit />
                            </IconButton>
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          </AccordionDetails>
        </Accordion>
      ))}
    </Box>
  );
};

export default AppSettingsManagement;