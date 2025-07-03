import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Stepper,
  Step,
  StepLabel,
  Typography,
  Alert,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  LinearProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  CheckCircle as CheckIcon,
  Error as ErrorIcon,
  TableChart as TableIcon,
  ImportExport as ImportIcon,
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { ApiErrorHandler, ApiError } from '../utils/apiErrorHandler';

interface VoterStagingImportProps {
  onComplete?: () => void;
}

interface StagingTableInfo {
  tableName: string;
  columns: string[];
  recordCount: number;
  sampleData: Record<string, any>[];
}

interface ColumnMapping {
  voterIdColumn?: string;
  firstNameColumn?: string;
  middleNameColumn?: string;
  lastNameColumn?: string;
  addressColumn?: string;
  cityColumn?: string;
  stateColumn?: string;
  zipColumn?: string;
  ageColumn?: string;
  genderColumn?: string;
  phoneColumn?: string;
  emailColumn?: string;
  partyColumn?: string;
  voteFrequencyColumn?: string;
}

const VoterStagingImport: React.FC<VoterStagingImportProps> = ({ onComplete }) => {
  const [activeStep, setActiveStep] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  
  // Step 1: File upload
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadResult, setUploadResult] = useState<any>(null);
  
  // Step 2: Column mapping
  const [tableInfo, setTableInfo] = useState<StagingTableInfo | null>(null);
  const [columnMapping, setColumnMapping] = useState<ColumnMapping>({});
  
  // Step 3: Import progress
  const [importResult, setImportResult] = useState<any>(null);
  const [showResultDialog, setShowResultDialog] = useState(false);
  
  const steps = ['Upload CSV', 'Map Columns', 'Import Voters'];

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files[0]) {
      const file = event.target.files[0];
      if (file.name.endsWith('.csv')) {
        setSelectedFile(file);
        setError(null);
      } else {
        setError('Please select a CSV file');
      }
    }
  };

  const handleUpload = async () => {
    if (!selectedFile) return;
    
    setLoading(true);
    setError(null);
    
    const formData = new FormData();
    formData.append('file', selectedFile);
    
    try {
      const result = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voterimport/stage`,
        {
          method: 'POST',
          body: formData,
        }
      );
      
      setUploadResult(result);
      setSuccess(result.message);
      
      // Fetch table info
      const info = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voterimport/staging-tables/${result.stagingTableName}`
      );
      
      setTableInfo(info);
      setActiveStep(1);
      
      // Auto-detect common column mappings
      autoDetectColumns(info.columns);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to upload file');
      }
    } finally {
      setLoading(false);
    }
  };

  const autoDetectColumns = (columns: string[]) => {
    const mapping: ColumnMapping = {};
    const lowerColumns = columns.map(c => ({ original: c, lower: c.toLowerCase() }));
    
    // Try to auto-detect columns based on common patterns
    const patterns = {
      voterIdColumn: ['voterid', 'voter_id', 'lalvoterid', 'id', 'voter_registration_id'],
      firstNameColumn: ['firstname', 'first_name', 'fname', 'voters_firstname', 'given_name'],
      lastNameColumn: ['lastname', 'last_name', 'lname', 'voters_lastname', 'surname'],
      middleNameColumn: ['middlename', 'middle_name', 'mname', 'voters_middlename'],
      addressColumn: ['address', 'street', 'addressline', 'residence_addresses_addressline', 'street_address'],
      cityColumn: ['city', 'residence_addresses_city', 'municipality'],
      stateColumn: ['state', 'st', 'residence_addresses_state', 'province'],
      zipColumn: ['zip', 'zipcode', 'zip_code', 'postal', 'residence_addresses_zip'],
      ageColumn: ['age', 'voters_age', 'voter_age'],
      genderColumn: ['gender', 'sex', 'voters_gender'],
      phoneColumn: ['phone', 'cellphone', 'cell', 'mobile', 'votertelephones_cellphoneformatted'],
      emailColumn: ['email', 'email_address', 'voters_email'],
      partyColumn: ['party', 'party_affiliation', 'parties_description', 'political_party'],
      voteFrequencyColumn: ['vote_frequency', 'voting_frequency', 'voter_frequency'],
    };
    
    for (const [field, fieldPatterns] of Object.entries(patterns)) {
      for (const pattern of fieldPatterns) {
        const match = lowerColumns.find(c => c.lower.includes(pattern));
        if (match) {
          (mapping as any)[field] = match.original;
          break;
        }
      }
    }
    
    setColumnMapping(mapping);
  };

  const handleImport = async () => {
    if (!uploadResult || !tableInfo) return;
    
    // Validate required mappings
    if (!columnMapping.firstNameColumn || !columnMapping.lastNameColumn || !columnMapping.addressColumn) {
      setError('Please map at least First Name, Last Name, and Address columns');
      return;
    }
    
    setLoading(true);
    setError(null);
    
    try {
      const result = await ApiErrorHandler.makeAuthenticatedRequest(
        `${API_BASE_URL}/api/voterimport/import`,
        {
          method: 'POST',
          body: JSON.stringify({
            stagingTableName: uploadResult.stagingTableName,
            mapping: columnMapping,
          })
        }
      );
      
      setImportResult(result);
      setSuccess(result.message);
      setActiveStep(2);
      setShowResultDialog(true);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to import voters');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleComplete = () => {
    setShowResultDialog(false);
    if (onComplete) {
      onComplete();
    }
  };

  const renderStepContent = () => {
    switch (activeStep) {
      case 0:
        return (
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Upload CSV File
              </Typography>
              <Typography variant="body2" color="textSecondary" paragraph>
                Upload a CSV file containing voter information. The file will be imported into a staging table
                where you can map columns before importing into the main voter database.
              </Typography>
              
              <Box sx={{ mt: 3 }}>
                <Button
                  variant="outlined"
                  component="label"
                  fullWidth
                  startIcon={<UploadIcon />}
                  sx={{ height: 100, border: '2px dashed' }}
                >
                  {selectedFile ? selectedFile.name : 'Select CSV File'}
                  <input
                    type="file"
                    hidden
                    accept=".csv"
                    onChange={handleFileSelect}
                  />
                </Button>
                
                {selectedFile && (
                  <Box sx={{ mt: 2 }}>
                    <Typography variant="body2">
                      File: {selectedFile.name} ({(selectedFile.size / 1024 / 1024).toFixed(2)} MB)
                    </Typography>
                    <Button
                      variant="contained"
                      onClick={handleUpload}
                      disabled={loading}
                      sx={{ mt: 2 }}
                      fullWidth
                    >
                      {loading ? <CircularProgress size={24} /> : 'Upload File'}
                    </Button>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>
        );
        
      case 1:
        return (
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Map CSV Columns to Voter Fields
              </Typography>
              <Typography variant="body2" color="textSecondary" paragraph>
                Map your CSV columns to the corresponding voter fields. Required fields are marked with *.
              </Typography>
              
              {tableInfo && (
                <Box sx={{ mt: 3 }}>
                  <Alert severity="info" sx={{ mb: 2 }}>
                    Found {tableInfo.recordCount} records with {tableInfo.columns.length} columns
                  </Alert>
                  
                  <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                    {[
                      { field: 'firstNameColumn', label: 'First Name *', required: true },
                      { field: 'lastNameColumn', label: 'Last Name *', required: true },
                      { field: 'addressColumn', label: 'Address *', required: true },
                      { field: 'cityColumn', label: 'City' },
                      { field: 'stateColumn', label: 'State' },
                      { field: 'zipColumn', label: 'ZIP Code' },
                      { field: 'voterIdColumn', label: 'Voter ID' },
                      { field: 'middleNameColumn', label: 'Middle Name' },
                      { field: 'ageColumn', label: 'Age' },
                      { field: 'genderColumn', label: 'Gender' },
                      { field: 'phoneColumn', label: 'Phone' },
                      { field: 'emailColumn', label: 'Email' },
                      { field: 'partyColumn', label: 'Party Affiliation' },
                      { field: 'voteFrequencyColumn', label: 'Vote Frequency' },
                    ].map(({ field, label, required }) => (
                      <FormControl key={field} fullWidth size="small">
                        <InputLabel>{label}</InputLabel>
                        <Select
                          value={(columnMapping as any)[field] || ''}
                          onChange={(e) => setColumnMapping({
                            ...columnMapping,
                            [field]: e.target.value
                          })}
                          label={label}
                          required={required}
                        >
                          <MenuItem value="">
                            <em>-- Not Mapped --</em>
                          </MenuItem>
                          {tableInfo.columns.map(col => (
                            <MenuItem key={col} value={col}>
                              {col}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    ))}
                  </Box>
                  
                  <Button
                    variant="contained"
                    onClick={handleImport}
                    disabled={loading || !columnMapping.firstNameColumn || !columnMapping.lastNameColumn || !columnMapping.addressColumn}
                    sx={{ mt: 3 }}
                    fullWidth
                  >
                    {loading ? <CircularProgress size={24} /> : 'Import Voters'}
                  </Button>
                  
                  {tableInfo.sampleData.length > 0 && (
                    <Box sx={{ mt: 3 }}>
                      <Typography variant="subtitle2" gutterBottom>
                        Sample Data Preview
                      </Typography>
                      <TableContainer component={Paper} sx={{ maxHeight: 300 }}>
                        <Table size="small" stickyHeader>
                          <TableHead>
                            <TableRow>
                              {tableInfo.columns.slice(0, 5).map(col => (
                                <TableCell key={col}>{col}</TableCell>
                              ))}
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {tableInfo.sampleData.map((row, idx) => (
                              <TableRow key={idx}>
                                {tableInfo.columns.slice(0, 5).map(col => (
                                  <TableCell key={col}>
                                    {row[col] || '-'}
                                  </TableCell>
                                ))}
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                    </Box>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        );
        
      case 2:
        return (
          <Card>
            <CardContent>
              <Box sx={{ textAlign: 'center', py: 3 }}>
                <CheckIcon sx={{ fontSize: 64, color: 'success.main' }} />
                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                  Import Complete!
                </Typography>
                {importResult && (
                  <Box sx={{ mt: 2 }}>
                    <Chip label={`${importResult.imported} Imported`} color="success" sx={{ m: 0.5 }} />
                    <Chip label={`${importResult.skipped} Skipped`} color="warning" sx={{ m: 0.5 }} />
                    <Chip label={`${importResult.errors} Errors`} color="error" sx={{ m: 0.5 }} />
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>
        );
        
      default:
        return null;
    }
  };

  return (
    <Box>
      <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
        {steps.map((label) => (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>
      
      {error && (
        <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      
      {success && (
        <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>
          {success}
        </Alert>
      )}
      
      {renderStepContent()}
      
      <Dialog open={showResultDialog} onClose={handleComplete} maxWidth="sm" fullWidth>
        <DialogTitle>Import Results</DialogTitle>
        <DialogContent>
          {importResult && (
            <Box>
              <Typography variant="body1" paragraph>
                Successfully processed {importResult.imported + importResult.skipped + importResult.errors} records:
              </Typography>
              <Box sx={{ mb: 2 }}>
                <Typography variant="body2" color="success.main">
                  ✓ {importResult.imported} voters imported
                </Typography>
                <Typography variant="body2" color="warning.main">
                  ⚠ {importResult.skipped} duplicates skipped
                </Typography>
                {importResult.errors > 0 && (
                  <Typography variant="body2" color="error.main">
                    ✗ {importResult.errors} errors
                  </Typography>
                )}
              </Box>
              {importResult.errorDetails && importResult.errorDetails.length > 0 && (
                <Box>
                  <Typography variant="subtitle2" gutterBottom>
                    Error Details (first 10):
                  </Typography>
                  <Paper variant="outlined" sx={{ p: 1, maxHeight: 200, overflow: 'auto' }}>
                    {importResult.errorDetails.map((error: string, idx: number) => (
                      <Typography key={idx} variant="caption" display="block">
                        {error}
                      </Typography>
                    ))}
                  </Paper>
                </Box>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleComplete} variant="contained">
            Done
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default VoterStagingImport;