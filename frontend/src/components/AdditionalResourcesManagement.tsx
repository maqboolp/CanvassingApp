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
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Switch,
  FormControlLabel,
  Alert,
  Chip,
  CircularProgress
} from '@mui/material';
import {
  Add,
  Edit,
  Delete,
  Link as LinkIcon,
  DragIndicator,
  Visibility,
  VisibilityOff
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { AuthUser } from '../types';

interface AdditionalResource {
  id: number;
  title: string;
  url: string;
  description: string;
  category: string;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

interface AdditionalResourcesManagementProps {
  user: AuthUser;
}

const RESOURCE_CATEGORIES = [
  'Training',
  'Voter Information',
  'Campaign Materials',
  'Social Media',
  'Tools & Resources',
  'Legal & Compliance',
  'Other'
];

const AdditionalResourcesManagement: React.FC<AdditionalResourcesManagementProps> = ({ user }) => {
  const [resources, setResources] = useState<AdditionalResource[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingResource, setEditingResource] = useState<AdditionalResource | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    title: '',
    url: '',
    description: '',
    category: 'Other',
    isActive: true,
    displayOrder: 0
  });

  useEffect(() => {
    fetchResources();
  }, []);

  const fetchResources = async () => {
    try {
      setLoading(true);
      const response = await fetch(`${API_BASE_URL}/api/additionalresources/all`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        setResources(data);
      } else {
        setError('Failed to fetch resources');
      }
    } catch (err) {
      setError('Failed to fetch resources');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenDialog = (resource?: AdditionalResource) => {
    if (resource) {
      setEditingResource(resource);
      setFormData({
        title: resource.title,
        url: resource.url,
        description: resource.description,
        category: resource.category,
        isActive: resource.isActive,
        displayOrder: resource.displayOrder
      });
    } else {
      setEditingResource(null);
      setFormData({
        title: '',
        url: '',
        description: '',
        category: 'Other',
        isActive: true,
        displayOrder: resources.length > 0 ? Math.max(...resources.map(r => r.displayOrder)) + 1 : 0
      });
    }
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingResource(null);
    setError(null);
  };

  const handleSave = async () => {
    if (!formData.title || !formData.url) {
      setError('Title and URL are required');
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const method = editingResource ? 'PUT' : 'POST';
      const url = editingResource 
        ? `${API_BASE_URL}/api/additionalresources/${editingResource.id}`
        : `${API_BASE_URL}/api/additionalresources`;

      const response = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          id: editingResource?.id,
          ...formData
        })
      });

      if (response.ok) {
        setSuccess(editingResource ? 'Resource updated successfully' : 'Resource created successfully');
        handleCloseDialog();
        fetchResources();
      } else {
        const errorData = await response.text();
        setError(errorData || 'Failed to save resource');
      }
    } catch (err) {
      setError('Failed to save resource');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (resource: AdditionalResource) => {
    if (!window.confirm(`Are you sure you want to delete "${resource.title}"?`)) {
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/additionalresources/${resource.id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });

      if (response.ok) {
        setSuccess('Resource deleted successfully');
        fetchResources();
      } else {
        setError('Failed to delete resource');
      }
    } catch (err) {
      setError('Failed to delete resource');
    }
  };

  const handleToggleActive = async (resource: AdditionalResource) => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/additionalresources/${resource.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          ...resource,
          isActive: !resource.isActive
        })
      });

      if (response.ok) {
        fetchResources();
      } else {
        setError('Failed to update resource status');
      }
    } catch (err) {
      setError('Failed to update resource status');
    }
  };

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
        <Typography variant="h5">Additional Resources</Typography>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => handleOpenDialog()}
        >
          Add Resource
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

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell width="40">Order</TableCell>
              <TableCell>Title</TableCell>
              <TableCell>Category</TableCell>
              <TableCell>Description</TableCell>
              <TableCell align="center">Status</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {resources.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  <Typography variant="body2" color="text.secondary" sx={{ py: 4 }}>
                    No resources added yet. Click "Add Resource" to get started.
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              resources.map((resource) => (
                <TableRow key={resource.id}>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <DragIndicator fontSize="small" color="disabled" />
                      <Typography variant="body2">{resource.displayOrder}</Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <LinkIcon fontSize="small" color="primary" />
                      <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                        {resource.title}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={resource.category}
                      size="small"
                      color="primary"
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {resource.description || '-'}
                    </Typography>
                  </TableCell>
                  <TableCell align="center">
                    <IconButton
                      size="small"
                      onClick={() => handleToggleActive(resource)}
                      color={resource.isActive ? 'success' : 'default'}
                    >
                      {resource.isActive ? <Visibility /> : <VisibilityOff />}
                    </IconButton>
                  </TableCell>
                  <TableCell align="center">
                    <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                      <IconButton
                        size="small"
                        onClick={() => handleOpenDialog(resource)}
                        color="primary"
                      >
                        <Edit />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={() => handleDelete(resource)}
                        color="error"
                      >
                        <Delete />
                      </IconButton>
                    </Box>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingResource ? 'Edit Resource' : 'Add New Resource'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}>
            <TextField
              label="Title"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="URL"
              value={formData.url}
              onChange={(e) => setFormData({ ...formData, url: e.target.value })}
              fullWidth
              required
              placeholder="https://example.com"
            />
            <TextField
              label="Description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
            />
            <FormControl fullWidth>
              <InputLabel>Category</InputLabel>
              <Select
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                label="Category"
              >
                {RESOURCE_CATEGORIES.map((category) => (
                  <MenuItem key={category} value={category}>
                    {category}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Display Order"
              type="number"
              value={formData.displayOrder}
              onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
              fullWidth
            />
            <FormControlLabel
              control={
                <Switch
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                />
              }
              label="Active"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button
            onClick={handleSave}
            variant="contained"
            disabled={saving}
          >
            {saving ? <CircularProgress size={20} /> : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AdditionalResourcesManagement;