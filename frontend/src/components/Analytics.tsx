import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  CircularProgress,
  Alert,
  Tabs,
  Tab,
  useTheme,
  useMediaQuery,
  Chip,
  Paper
} from '@mui/material';
import {
  BarChart,
  Bar,
  PieChart,
  Pie,
  LineChart,
  Line,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer
} from 'recharts';
import {
  People,
  HowToVote,
  LocationOn,
  Timeline,
  ContactPhone,
  ThumbUp
} from '@mui/icons-material';
import { API_BASE_URL } from '../config';
import { AuthUser } from '../types';

interface AnalyticsProps {
  user: AuthUser;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`analytics-tabpanel-${index}`}
      aria-labelledby={`analytics-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
}

const Analytics: React.FC<AnalyticsProps> = ({ user }) => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const [tabValue, setTabValue] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [voterData, setVoterData] = useState<any>(null);
  const [contactData, setContactData] = useState<any>(null);

  // Color scheme for charts
  const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D'];

  useEffect(() => {
    fetchAnalytics();
  }, []);

  const fetchAnalytics = async () => {
    try {
      setLoading(true);
      
      // Fetch voter demographics
      const voterResponse = await fetch(`${API_BASE_URL}/api/analytics/voter-demographics`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (!voterResponse.ok) {
        throw new Error('Failed to fetch voter demographics');
      }
      
      const voterData = await voterResponse.json();
      setVoterData(voterData);
      
      // Fetch contact analytics
      const contactResponse = await fetch(`${API_BASE_URL}/api/analytics/contact-analytics`, {
        headers: {
          'Authorization': `Bearer ${user.token}`
        }
      });
      
      if (!contactResponse.ok) {
        throw new Error('Failed to fetch contact analytics');
      }
      
      const contactData = await contactResponse.json();
      setContactData(contactData);
      
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">{error}</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ width: '100%', p: 2 }}>
      <Typography variant="h4" component="h1" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <Timeline /> Analytics Dashboard
      </Typography>

      {/* Summary Cards */}
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' }, gap: 3, mb: 3 }}>
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Box>
                <Typography color="textSecondary" gutterBottom>
                  Total Voters
                </Typography>
                <Typography variant="h4">
                  {voterData?.totalVoters?.toLocaleString() || 0}
                </Typography>
              </Box>
              <People color="primary" sx={{ fontSize: 40 }} />
            </Box>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Box>
                <Typography color="textSecondary" gutterBottom>
                  Contacted
                </Typography>
                <Typography variant="h4">
                  {voterData?.contactStats?.contacted?.toLocaleString() || 0}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  {voterData?.contactStats?.contactedPercentage || 0}%
                </Typography>
              </Box>
              <ContactPhone color="success" sx={{ fontSize: 40 }} />
            </Box>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Box>
                <Typography color="textSecondary" gutterBottom>
                  Total Contacts
                </Typography>
                <Typography variant="h4">
                  {contactData?.totalContacts?.toLocaleString() || 0}
                </Typography>
              </Box>
              <HowToVote color="info" sx={{ fontSize: 40 }} />
            </Box>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Box>
                <Typography color="textSecondary" gutterBottom>
                  Support Rate
                </Typography>
                <Typography variant="h4">
                  {voterData?.voterSupportStats?.length > 0 
                    ? Math.round(
                        voterData.voterSupportStats
                          .filter((s: any) => s.support === 'StrongYes' || s.support === 'LeaningYes')
                          .reduce((sum: number, s: any) => sum + s.percentage, 0)
                      ) + '%'
                    : 'N/A'}
                </Typography>
              </Box>
              <ThumbUp color="warning" sx={{ fontSize: 40 }} />
            </Box>
          </CardContent>
        </Card>
      </Box>

      <Tabs value={tabValue} onChange={handleTabChange} sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tab label="Demographics" />
        <Tab label="Contact Analytics" />
        <Tab label="Geographic Distribution" />
      </Tabs>

      <TabPanel value={tabValue} index={0}>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' }, gap: 3 }}>
          {/* Gender Distribution */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>Gender Distribution</Typography>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={voterData?.genderStats || []}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={(entry) => `${entry.gender} (${entry.percentage}%)`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {voterData?.genderStats?.map((entry: any, index: number) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </Paper>

          {/* Age Distribution */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>Age Distribution</Typography>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={voterData?.ageGroupStats || []}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="ageGroup" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#0088FE" />
              </BarChart>
            </ResponsiveContainer>
          </Paper>

          {/* Party Affiliation */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>Party Affiliation</Typography>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={voterData?.partyAffiliationStats || []}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={(entry) => `${entry.party} (${entry.percentage}%)`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {voterData?.partyAffiliationStats?.map((entry: any, index: number) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </Paper>

          {/* Vote Frequency */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>Vote Frequency</Typography>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={voterData?.voteFrequencyStats || []}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="frequency" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#00C49F" />
              </BarChart>
            </ResponsiveContainer>
          </Paper>

          {/* Voter Support */}
          {voterData?.voterSupportStats?.length > 0 && (
            <Paper sx={{ p: 2, gridColumn: '1 / -1' }}>
              <Typography variant="h6" gutterBottom>Voter Support Levels</Typography>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={voterData?.voterSupportStats || []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="support" />
                  <YAxis />
                  <Tooltip />
                  <Bar dataKey="count" fill="#FFBB28" />
                </BarChart>
              </ResponsiveContainer>
            </Paper>
          )}

          {/* Ethnicity Distribution */}
          {voterData?.ethnicityStats?.length > 0 && (
            <Paper sx={{ p: 2 }}>
              <Typography variant="h6" gutterBottom>Ethnicity Distribution</Typography>
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={voterData?.ethnicityStats || []}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={(entry) => `${entry.ethnicity} (${entry.percentage}%)`}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="count"
                  >
                    {voterData?.ethnicityStats?.map((entry: any, index: number) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </Paper>
          )}

          {/* Religion Distribution */}
          {voterData?.religionStats?.length > 0 && (
            <Paper sx={{ p: 2 }}>
              <Typography variant="h6" gutterBottom>Religion Distribution</Typography>
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={voterData?.religionStats || []}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={(entry) => `${entry.religion} (${entry.percentage}%)`}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="count"
                  >
                    {voterData?.religionStats?.map((entry: any, index: number) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </Paper>
          )}

          {/* Income Distribution */}
          {voterData?.incomeStats?.length > 0 && (
            <Paper sx={{ p: 2, gridColumn: '1 / -1' }}>
              <Typography variant="h6" gutterBottom>Income Distribution</Typography>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={voterData?.incomeStats || []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="income" />
                  <YAxis />
                  <Tooltip />
                  <Bar dataKey="count" fill="#82CA9D" />
                </BarChart>
              </ResponsiveContainer>
            </Paper>
          )}
        </Box>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' }, gap: 3 }}>
          {/* Contact Status Distribution */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>Contact Status Distribution</Typography>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={contactData?.contactsByStatus || []}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={(entry) => `${entry.status} (${entry.percentage}%)`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {contactData?.contactsByStatus?.map((entry: any, index: number) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </Paper>

          {/* Top Volunteers */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>Top Volunteers</Typography>
            <Box sx={{ mt: 2 }}>
              {contactData?.contactsByVolunteer?.map((volunteer: any, index: number) => (
                <Box key={volunteer.volunteerId} sx={{ mb: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="body1">
                      {index + 1}. {volunteer.volunteerName}
                    </Typography>
                    <Chip label={`${volunteer.contactCount} contacts`} color="primary" size="small" />
                  </Box>
                </Box>
              ))}
            </Box>
          </Paper>

          {/* Contacts Over Time */}
          <Paper sx={{ p: 2, gridColumn: '1 / -1' }}>
            <Typography variant="h6" gutterBottom>Contact Activity (Last 30 Days)</Typography>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={contactData?.contactsOverTime || []}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis 
                  dataKey="date" 
                  tickFormatter={(date) => new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                />
                <YAxis />
                <Tooltip 
                  labelFormatter={(date) => new Date(date).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })}
                />
                <Line type="monotone" dataKey="count" stroke="#8884d8" strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          </Paper>
        </Box>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Box>
          {/* Top Zip Codes */}
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>Top 10 Zip Codes</Typography>
            <ResponsiveContainer width="100%" height={400}>
              <BarChart data={voterData?.zipCodeStats || []} layout="horizontal">
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="zipCode" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#FF8042">
                  {voterData?.zipCodeStats?.map((entry: any, index: number) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Box>
      </TabPanel>
    </Box>
  );
};

export default Analytics;