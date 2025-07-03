import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  Chip,
  Tooltip,
  CircularProgress,
} from '@mui/material';
import {
  Delete as DeleteIcon,
  Edit as EditIcon,
  Refresh as RefreshIcon,
  CheckCircle as ImportedIcon,
  Schedule as PendingIcon,
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import VoterStagingImport from './VoterStagingImport';

interface StagingTable {
  tableName: string;
  rowCount: number;
  createdAt: string;
  uploadedBy?: string;
  fileName?: string;
  isImported: boolean;
  importedAt?: string;
}

interface StagingTableManagerProps {
  onImportComplete?: () => void;
}

const StagingTableManager: React.FC<StagingTableManagerProps> = ({ onImportComplete }) => {
  const [tables, setTables] = useState<StagingTable[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedTable, setSelectedTable] = useState<string | null>(null);
  const [remapDialogOpen, setRemapDialogOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [tableToDelete, setTableToDelete] = useState<string | null>(null);

  useEffect(() => {
    fetchStagingTables();
  }, []);

  const fetchStagingTables = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await fetch(`${API_BASE_URL}/api/voter-import/staging-tables`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch staging tables');
      }

      const data = await response.json();
      setTables(data.tables || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteTable = async (tableName: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/voter-import/staging-tables/${tableName}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to delete staging table');
      }

      setDeleteConfirmOpen(false);
      setTableToDelete(null);
      await fetchStagingTables();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    }
  };

  const handleRemap = (tableName: string) => {
    setSelectedTable(tableName);
    setRemapDialogOpen(true);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ mb: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5">Staging Tables</Typography>
        <Button
          startIcon={<RefreshIcon />}
          onClick={fetchStagingTables}
          disabled={loading}
        >
          Refresh
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Table Name</TableCell>
              <TableCell>Records</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Created</TableCell>
              <TableCell>Uploaded By</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {tables.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  No staging tables found
                </TableCell>
              </TableRow>
            ) : (
              tables.map((table) => (
                <TableRow key={table.tableName}>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                      {table.tableName}
                    </Typography>
                    {table.fileName && (
                      <Typography variant="caption" color="textSecondary" display="block">
                        {table.fileName}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{table.rowCount.toLocaleString()}</TableCell>
                  <TableCell>
                    {table.isImported ? (
                      <Chip
                        icon={<ImportedIcon />}
                        label="Imported"
                        color="success"
                        size="small"
                      />
                    ) : (
                      <Chip
                        icon={<PendingIcon />}
                        label="Pending"
                        color="warning"
                        size="small"
                      />
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {formatDate(table.createdAt)}
                    </Typography>
                    {table.importedAt && (
                      <Typography variant="caption" color="textSecondary" display="block">
                        Imported: {formatDate(table.importedAt)}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{table.uploadedBy || 'Unknown'}</TableCell>
                  <TableCell>
                    <Tooltip title={table.isImported ? "Re-map and Import" : "Map and Import"}>
                      <IconButton
                        onClick={() => handleRemap(table.tableName)}
                        color="primary"
                        size="small"
                      >
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton
                        onClick={() => {
                          setTableToDelete(table.tableName);
                          setDeleteConfirmOpen(true);
                        }}
                        color="error"
                        size="small"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Remap Dialog */}
      <Dialog
        open={remapDialogOpen}
        onClose={() => setRemapDialogOpen(false)}
        maxWidth="lg"
        fullWidth
      >
        <DialogTitle>
          Re-map Staging Table: {selectedTable}
        </DialogTitle>
        <DialogContent>
          {selectedTable && (
            <VoterStagingImport
              existingStagingTable={selectedTable}
              onComplete={() => {
                setRemapDialogOpen(false);
                fetchStagingTables();
                if (onImportComplete) {
                  onImportComplete();
                }
              }}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRemapDialogOpen(false)}>
            Cancel
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
      >
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete the staging table "{tableToDelete}"?
            This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmOpen(false)}>
            Cancel
          </Button>
          <Button
            onClick={() => tableToDelete && handleDeleteTable(tableToDelete)}
            color="error"
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default StagingTableManager;