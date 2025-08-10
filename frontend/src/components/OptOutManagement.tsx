import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Typography,
  Button,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Grid,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Alert,
  Tooltip,
  InputAdornment
} from '@mui/material';
import {
  Download as DownloadIcon,
  Delete as DeleteIcon,
  Add as AddIcon,
  Search as SearchIcon,
  Refresh as RefreshIcon,
  PhoneDisabled as PhoneDisabledIcon,
  Sms as SmsIcon,
  Phone as PhoneIcon,
  Block as BlockIcon
} from '@mui/icons-material';
import { format } from 'date-fns';
import { API_BASE_URL } from '../config';
import { AuthUser } from '../types';

interface OptOutRecord {
  id: number;
  phoneNumber: string;
  type: 'All' | 'RoboCalls' | 'SMS';
  method: 'Phone' | 'SMS' | 'Manual' | 'Web';
  optedOutAt: string;
  reason?: string;
  voterId?: string;
  voterName?: string;
  voterAddress?: string;
}

interface OptOutStats {
  totalOptOuts: number;
  roboCallOptOuts: number;
  smsOptOuts: number;
  allOptOuts: number;
  phoneOptOuts: number;
  smsMethodOptOuts: number;
  manualOptOuts: number;
  webOptOuts: number;
  last30Days: number;
  last7Days: number;
  today: number;
}

interface OptOutManagementProps {
  user?: AuthUser;
}

const OptOutManagement: React.FC<OptOutManagementProps> = ({ user: propUser }) => {
  const [optOuts, setOptOuts] = useState<OptOutRecord[]>([]);
  const [stats, setStats] = useState<OptOutStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [totalCount, setTotalCount] = useState(0);
  const [searchPhone, setSearchPhone] = useState('');
  const [filterType, setFilterType] = useState<string>('');
  const [filterMethod, setFilterMethod] = useState<string>('');
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [newOptOut, setNewOptOut] = useState({
    phoneNumber: '',
    type: 'All' as const,
    reason: ''
  });
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const user = propUser || JSON.parse(localStorage.getItem('user') || '{}');
  const isSuperAdmin = user.role === 'superadmin';

  useEffect(() => {
    fetchOptOuts();
    fetchStats();
  }, [page, rowsPerPage, searchPhone, filterType, filterMethod]);

  const fetchOptOuts = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        pageNumber: (page + 1).toString(),
        pageSize: rowsPerPage.toString(),
        ...(searchPhone && { searchPhone }),
        ...(filterType && { type: filterType }),
        ...(filterMethod && { method: filterMethod })
      });

      const response = await fetch(`${API_BASE_URL}/api/OptOut?${params}`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setOptOuts(data);
        
        const totalHeader = response.headers.get('X-Total-Count');
        if (totalHeader) {
          setTotalCount(parseInt(totalHeader));
        }
      } else {
        throw new Error('Failed to fetch opt-outs');
      }
    } catch (err) {
      setError('Failed to load opt-out records');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const fetchStats = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/OptOut/stats`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      if (response.ok) {
        const data = await response.json();
        setStats(data);
      }
    } catch (err) {
      console.error('Failed to fetch stats:', err);
    }
  };

  const handleExport = async () => {
    try {
      const params = new URLSearchParams({
        ...(filterType && { type: filterType }),
        ...(filterMethod && { method: filterMethod })
      });

      const response = await fetch(`${API_BASE_URL}/api/OptOut/export?${params}`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `opt-outs-${format(new Date(), 'yyyy-MM-dd')}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        setSuccess('Export downloaded successfully');
      }
    } catch (err) {
      setError('Failed to export opt-outs');
    }
  };

  const handleAddOptOut = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/OptOut`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${user.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(newOptOut)
      });
      
      if (response.ok) {
        setSuccess('Opt-out added successfully');
        setAddDialogOpen(false);
        setNewOptOut({ phoneNumber: '', type: 'All', reason: '' });
        fetchOptOuts();
        fetchStats();
      } else {
        const error = await response.json();
        setError(error.error || 'Failed to add opt-out');
      }
    } catch (err) {
      setError('Failed to add opt-out');
    }
  };

  const handleRemoveOptOut = async (id: number) => {
    if (!window.confirm('Are you sure you want to remove this opt-out? The person may receive calls/texts again.')) {
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/OptOut/${id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (response.ok) {
        setSuccess('Opt-out removed successfully');
        fetchOptOuts();
        fetchStats();
      } else {
        setError('Failed to remove opt-out');
      }
    } catch (err) {
      setError('Failed to remove opt-out');
    }
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'All':
        return <BlockIcon fontSize="small" />;
      case 'RoboCalls':
        return <PhoneDisabledIcon fontSize="small" />;
      case 'SMS':
        return <SmsIcon fontSize="small" />;
      default:
        return null;
    }
  };

  const getMethodIcon = (method: string) => {
    switch (method) {
      case 'Phone':
        return <PhoneIcon fontSize="small" />;
      case 'SMS':
        return <SmsIcon fontSize="small" />;
      default:
        return null;
    }
  };

  const getTypeColor = (type: string): "error" | "warning" | "info" => {
    switch (type) {
      case 'All':
        return 'error';
      case 'RoboCalls':
        return 'warning';
      case 'SMS':
        return 'info';
      default:
        return 'info';
    }
  };

  return (
    <Box>
      {/* Statistics Cards */}
      {stats && (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Total Opt-Outs
                </Typography>
                <Typography variant="h4">
                  {stats.totalOptOuts}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  All time
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Last 30 Days
                </Typography>
                <Typography variant="h4">
                  {stats.last30Days}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Recent opt-outs
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Last 7 Days
                </Typography>
                <Typography variant="h4">
                  {stats.last7Days}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  This week
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Today
                </Typography>
                <Typography variant="h4">
                  {stats.today}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Opted out today
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Breakdown Cards */}
      {stats && (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  By Type
                </Typography>
                <Box sx={{ display: 'flex', gap: 2 }}>
                  <Chip
                    icon={<BlockIcon />}
                    label={`All: ${stats.allOptOuts}`}
                    color="error"
                  />
                  <Chip
                    icon={<PhoneDisabledIcon />}
                    label={`RoboCalls: ${stats.roboCallOptOuts}`}
                    color="warning"
                  />
                  <Chip
                    icon={<SmsIcon />}
                    label={`SMS: ${stats.smsOptOuts}`}
                    color="info"
                  />
                </Box>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  By Method
                </Typography>
                <Box sx={{ display: 'flex', gap: 2 }}>
                  <Chip
                    icon={<PhoneIcon />}
                    label={`Phone: ${stats.phoneOptOuts}`}
                  />
                  <Chip
                    icon={<SmsIcon />}
                    label={`SMS: ${stats.smsMethodOptOuts}`}
                  />
                  <Chip
                    label={`Manual: ${stats.manualOptOuts}`}
                  />
                  <Chip
                    label={`Web: ${stats.webOptOuts}`}
                  />
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Filters and Actions */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              fullWidth
              label="Search Phone Number"
              value={searchPhone}
              onChange={(e) => setSearchPhone(e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <FormControl fullWidth>
              <InputLabel>Type</InputLabel>
              <Select
                value={filterType}
                onChange={(e) => setFilterType(e.target.value)}
                label="Type"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="All">All Communications</MenuItem>
                <MenuItem value="RoboCalls">RoboCalls Only</MenuItem>
                <MenuItem value="SMS">SMS Only</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <FormControl fullWidth>
              <InputLabel>Method</InputLabel>
              <Select
                value={filterMethod}
                onChange={(e) => setFilterMethod(e.target.value)}
                label="Method"
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="Phone">Phone</MenuItem>
                <MenuItem value="SMS">SMS</MenuItem>
                <MenuItem value="Manual">Manual</MenuItem>
                <MenuItem value="Web">Web</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6} md={5}>
            <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
              <Button
                variant="outlined"
                startIcon={<RefreshIcon />}
                onClick={() => {
                  fetchOptOuts();
                  fetchStats();
                }}
              >
                Refresh
              </Button>
              <Button
                variant="outlined"
                startIcon={<DownloadIcon />}
                onClick={handleExport}
              >
                Export CSV
              </Button>
              {isSuperAdmin && (
                <Button
                  variant="contained"
                  startIcon={<AddIcon />}
                  onClick={() => setAddDialogOpen(true)}
                >
                  Add Opt-Out
                </Button>
              )}
            </Box>
          </Grid>
        </Grid>
      </Paper>

      {/* Success/Error Messages */}
      {success && (
        <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>
          {success}
        </Alert>
      )}
      {error && (
        <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Table */}
      <TableContainer component={Paper}>
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
            <CircularProgress />
          </Box>
        ) : (
          <>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Phone Number</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Method</TableCell>
                  <TableCell>Date</TableCell>
                  <TableCell>Voter</TableCell>
                  <TableCell>Reason</TableCell>
                  {isSuperAdmin && <TableCell>Actions</TableCell>}
                </TableRow>
              </TableHead>
              <TableBody>
                {optOuts.map((optOut) => (
                  <TableRow key={optOut.id}>
                    <TableCell>{optOut.phoneNumber}</TableCell>
                    <TableCell>
                      <Chip
                        icon={getTypeIcon(optOut.type)}
                        label={optOut.type}
                        size="small"
                        color={getTypeColor(optOut.type)}
                      />
                    </TableCell>
                    <TableCell>
                      <Chip
                        icon={getMethodIcon(optOut.method)}
                        label={optOut.method}
                        size="small"
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell>
                      {format(new Date(optOut.optedOutAt), 'MMM d, yyyy h:mm a')}
                    </TableCell>
                    <TableCell>
                      {optOut.voterName ? (
                        <Tooltip title={optOut.voterAddress || ''}>
                          <span>{optOut.voterName}</span>
                        </Tooltip>
                      ) : (
                        <Typography variant="body2" color="textSecondary">
                          Not matched
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      {optOut.reason || '-'}
                    </TableCell>
                    {isSuperAdmin && (
                      <TableCell>
                        <IconButton
                          size="small"
                          onClick={() => handleRemoveOptOut(optOut.id)}
                          color="error"
                        >
                          <DeleteIcon />
                        </IconButton>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
                {optOuts.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={isSuperAdmin ? 7 : 6} align="center">
                      No opt-out records found
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
            <TablePagination
              rowsPerPageOptions={[10, 25, 50, 100]}
              component="div"
              count={totalCount}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={(e, newPage) => setPage(newPage)}
              onRowsPerPageChange={(e) => {
                setRowsPerPage(parseInt(e.target.value, 10));
                setPage(0);
              }}
            />
          </>
        )}
      </TableContainer>

      {/* Add Opt-Out Dialog */}
      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Manual Opt-Out</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              fullWidth
              label="Phone Number"
              value={newOptOut.phoneNumber}
              onChange={(e) => setNewOptOut({ ...newOptOut, phoneNumber: e.target.value })}
              placeholder="(205) 555-1234"
            />
            <FormControl fullWidth>
              <InputLabel>Type</InputLabel>
              <Select
                value={newOptOut.type}
                onChange={(e) => setNewOptOut({ ...newOptOut, type: e.target.value as any })}
                label="Type"
              >
                <MenuItem value="All">All Communications</MenuItem>
                <MenuItem value="RoboCalls">RoboCalls Only</MenuItem>
                <MenuItem value="SMS">SMS Only</MenuItem>
              </Select>
            </FormControl>
            <TextField
              fullWidth
              label="Reason (Optional)"
              value={newOptOut.reason}
              onChange={(e) => setNewOptOut({ ...newOptOut, reason: e.target.value })}
              multiline
              rows={2}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleAddOptOut} variant="contained">
            Add Opt-Out
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default OptOutManagement;