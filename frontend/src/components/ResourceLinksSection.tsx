import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Link,
  Chip,
  Grid,
  CircularProgress,
  Alert,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Divider
} from '@mui/material';
import {
  OpenInNew,
  Edit,
  Delete,
  Add,
  DragIndicator
} from '@mui/icons-material';
import { AuthUser } from '../types';
import { API_BASE_URL } from '../config';

interface ResourceLink {
  id: string;
  title: string;
  url: string;
  description?: string;
  category: string;
  displayOrder: number;
  isActive: boolean;
}

interface ResourceLinksSectionProps {
  user?: AuthUser;
  isAdmin?: boolean;
}

const ResourceLinksSection: React.FC<ResourceLinksSectionProps> = ({ user, isAdmin }) => {
  const [voterResources, setVoterResources] = useState<ResourceLink[]>([]);
  const [campaignInfo, setCampaignInfo] = useState<ResourceLink[]>([]);
  const [generalResources, setGeneralResources] = useState<ResourceLink[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Admin states
  const [editDialog, setEditDialog] = useState(false);
  const [editingLink, setEditingLink] = useState<ResourceLink | null>(null);
  const [formData, setFormData] = useState({
    title: '',
    url: '',
    description: '',
    category: 'VoterResources',
    displayOrder: 0,
    isActive: true
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    fetchResourceLinks();
  }, []);

  const fetchResourceLinks = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/resourcelinks`, {
        headers: user ? { 'Authorization': `Bearer ${user.token}` } : {}
      });

      if (response.ok) {
        const data = await response.json();
        
        // Group links by category
        const voter = data.filter((link: ResourceLink) => 
          link.category === 'VoterResources' && link.isActive
        );
        const campaign = data.filter((link: ResourceLink) => 
          link.category === 'CampaignInformation' && link.isActive
        );
        const general = data.filter((link: ResourceLink) => 
          link.category === 'GeneralResources' && link.isActive
        );
        
        setVoterResources(voter);
        setCampaignInfo(campaign);
        setGeneralResources(general);
      } else {
        setError('Failed to load resource links');
      }
    } catch (err) {
      setError('Failed to load resource links');
      console.error('Error fetching resource links:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAddNew = (category: string) => {
    setEditingLink(null);
    setFormData({
      title: '',
      url: '',
      description: '',
      category,
      displayOrder: 0,
      isActive: true
    });
    setEditDialog(true);
  };

  const handleEdit = (link: ResourceLink) => {
    setEditingLink(link);
    setFormData({
      title: link.title,
      url: link.url,
      description: link.description || '',
      category: link.category,
      displayOrder: link.displayOrder,
      isActive: link.isActive
    });
    setEditDialog(true);
  };

  const handleSave = async () => {
    if (!formData.title || !formData.url) {
      return;
    }

    setSaving(true);
    try {
      const url = editingLink 
        ? `${API_BASE_URL}/api/resourcelinks/${editingLink.id}`
        : `${API_BASE_URL}/api/resourcelinks`;
      
      const method = editingLink ? 'PUT' : 'POST';
      
      const response = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user?.token}`
        },
        body: JSON.stringify(formData)
      });

      if (response.ok) {
        setEditDialog(false);
        fetchResourceLinks();
      } else {
        const error = await response.json();
        console.error('Failed to save resource link:', error);
      }
    } catch (err) {
      console.error('Error saving resource link:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (link: ResourceLink) => {
    if (!window.confirm(`Are you sure you want to delete "${link.title}"?`)) {
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/resourcelinks/${link.id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${user?.token}`
        }
      });

      if (response.ok) {
        fetchResourceLinks();
      }
    } catch (err) {
      console.error('Error deleting resource link:', err);
    }
  };

  const renderResourceCategory = (
    title: string, 
    resources: ResourceLink[], 
    category: string
  ) => (
    <Card sx={{ mb: 3 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">{title}</Typography>
          {isAdmin && (
            <IconButton
              size="small"
              color="primary"
              onClick={() => handleAddNew(category)}
              title="Add new link"
            >
              <Add />
            </IconButton>
          )}
        </Box>
        
        {resources.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No resources available
          </Typography>
        ) : (
          <List disablePadding>
            {resources.map((resource, index) => (
              <React.Fragment key={resource.id}>
                {index > 0 && <Divider />}
                <ListItem
                  sx={{
                    px: 0,
                    py: 1,
                    '&:hover': isAdmin ? { backgroundColor: 'action.hover' } : {}
                  }}
                >
                  <ListItemText
                    primary={
                      <Link
                        href={resource.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        sx={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: 0.5,
                          textDecoration: 'none',
                          '&:hover': { textDecoration: 'underline' }
                        }}
                      >
                        {resource.title}
                        <OpenInNew fontSize="small" />
                      </Link>
                    }
                    secondary={resource.description}
                  />
                  {isAdmin && (
                    <ListItemSecondaryAction>
                      <IconButton
                        edge="end"
                        size="small"
                        onClick={() => handleEdit(resource)}
                      >
                        <Edit fontSize="small" />
                      </IconButton>
                      <IconButton
                        edge="end"
                        size="small"
                        onClick={() => handleDelete(resource)}
                      >
                        <Delete fontSize="small" />
                      </IconButton>
                    </ListItemSecondaryAction>
                  )}
                </ListItem>
              </React.Fragment>
            ))}
          </List>
        )}
      </CardContent>
    </Card>
  );

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" py={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 3 }}>
        {error}
      </Alert>
    );
  }

  return (
    <>
      <Box sx={{ mb: 4 }}>
        {renderResourceCategory('Voter Resources', voterResources, 'VoterResources')}
        {renderResourceCategory('Campaign Information', campaignInfo, 'CampaignInformation')}
        {renderResourceCategory('General Resources', generalResources, 'GeneralResources')}
      </Box>

      {/* Edit Dialog */}
      <Dialog open={editDialog} onClose={() => setEditDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingLink ? 'Edit Resource Link' : 'Add Resource Link'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
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
              type="url"
              placeholder="https://example.com"
            />
            <TextField
              label="Description (optional)"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              fullWidth
              multiline
              rows={2}
            />
            <FormControl fullWidth>
              <InputLabel>Category</InputLabel>
              <Select
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                label="Category"
              >
                <MenuItem value="VoterResources">Voter Resources</MenuItem>
                <MenuItem value="CampaignInformation">Campaign Information</MenuItem>
                <MenuItem value="GeneralResources">General Resources</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Display Order"
              type="number"
              value={formData.displayOrder}
              onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialog(false)} disabled={saving}>
            Cancel
          </Button>
          <Button 
            onClick={handleSave} 
            variant="contained" 
            disabled={saving || !formData.title || !formData.url}
          >
            {saving ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ResourceLinksSection;