import React, { useState, useEffect } from 'react';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { Box, CircularProgress, Typography } from '@mui/material';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';

import { AuthUser, LoginRequest } from './types';
import { authService } from './services/authService';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import AdminDashboard from './components/AdminDashboard';
import ResetPassword from './components/ResetPassword';
import CompleteRegistration from './components/CompleteRegistration';
import SelfRegistration from './components/SelfRegistration';

// Create Material-UI theme matching tanveer4hoover.com
const theme = createTheme({
  palette: {
    primary: {
      main: '#673de6', // Primary purple from campaign site
      light: '#8c85ff', // Meteorite soft lavender blue
      dark: '#5025d1', // Primary dark purple
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#fc5185', // Danger/accent pink from campaign
      light: '#ff8a9f',
      dark: '#c73e6a',
      contrastText: '#ffffff',
    },
    success: {
      main: '#00b090', // Success teal from campaign
    },
    warning: {
      main: '#ffcd35', // Warning yellow from campaign
    },
    background: {
      default: '#f2f3f6', // Light gray from campaign
      paper: '#ffffff',
    },
    text: {
      primary: '#1d1e20', // Near black from campaign
      secondary: '#6c757d',
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h4: {
      fontWeight: 600,
      color: '#2f1c6a', // Meteorite dark for headers
    },
    h6: {
      fontWeight: 500,
      color: '#2f1c6a',
    },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: 8,
          fontWeight: 500,
        },
        contained: {
          background: 'linear-gradient(45deg, #673de6 30%, #8c85ff 90%)',
          boxShadow: '0 3px 5px 2px rgba(103, 61, 230, .3)',
          '&:hover': {
            background: 'linear-gradient(45deg, #5025d1 30%, #673de6 90%)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: '0 2px 8px rgba(47, 28, 106, 0.08)',
          border: '1px solid rgba(213, 223, 255, 0.3)',
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          background: 'linear-gradient(45deg, #2f1c6a 30%, #673de6 90%)',
          boxShadow: '0 3px 5px 2px rgba(47, 28, 106, .3)',
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 16,
        },
        colorPrimary: {
          backgroundColor: '#ebe4ff', // Primary light from campaign
          color: '#2f1c6a',
        },
      },
    },
  },
});

function App() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState<string | null>(null);

  useEffect(() => {
    // Check if user is already logged in
    const currentUser = authService.getCurrentUser();
    if (currentUser) {
      setUser(currentUser);
    }
    setLoading(false);
  }, []);

  const handleLogin = async (credentials: LoginRequest) => {
    setLoginLoading(true);
    setLoginError(null);
    
    try {
      const user = await authService.login(credentials);
      setUser(user);
    } catch (error) {
      setLoginError(error instanceof Error ? error.message : 'Login failed');
    } finally {
      setLoginLoading(false);
    }
  };

  const handleLogout = () => {
    authService.logout();
    setUser(null);
  };

  // Show loading spinner while checking authentication
  if (loading) {
    return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Box
          display="flex"
          justifyContent="center"
          alignItems="center"
          minHeight="100vh"
          flexDirection="column"
        >
          <CircularProgress size={60} />
          <Typography variant="h6" sx={{ mt: 2 }}>
            Loading...
          </Typography>
        </Box>
      </ThemeProvider>
    );
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Router>
        <Routes>
          {/* Public routes */}
          <Route
            path="/login"
            element={
              user ? (
                <Navigate to="/" replace />
              ) : (
                <Login
                  onLogin={handleLogin}
                  isLoading={loginLoading}
                  error={loginError}
                />
              )
            }
          />
          
          <Route
            path="/reset-password"
            element={
              user ? (
                <Navigate to="/" replace />
              ) : (
                <ResetPassword />
              )
            }
          />

          <Route
            path="/complete-registration"
            element={
              user ? (
                <Navigate to="/" replace />
              ) : (
                <CompleteRegistration />
              )
            }
          />

          <Route
            path="/register"
            element={
              user ? (
                <Navigate to="/" replace />
              ) : (
                <SelfRegistration />
              )
            }
          />

          {/* Protected routes */}
          <Route
            path="/"
            element={
              user ? (
                user.role === 'admin' || user.role === 'superadmin' ? (
                  <AdminDashboard user={user} onLogout={handleLogout} />
                ) : (
                  <Dashboard user={user} onLogout={handleLogout} />
                )
              ) : (
                <Navigate to="/login" replace />
              )
            }
          />

          <Route
            path="/dashboard"
            element={
              user ? (
                <Dashboard user={user} onLogout={handleLogout} />
              ) : (
                <Navigate to="/login" replace />
              )
            }
          />

          <Route
            path="/admin"
            element={
              user && (user.role === 'admin' || user.role === 'superadmin') ? (
                <AdminDashboard user={user} onLogout={handleLogout} />
              ) : (
                <Navigate to="/" replace />
              )
            }
          />

          {/* Catch all route - redirect based on auth status */}
          <Route 
            path="*" 
            element={
              user ? (
                <Navigate to="/" replace />
              ) : (
                <Navigate to="/login" replace />
              )
            } 
          />
        </Routes>
      </Router>
    </ThemeProvider>
  );
}

export default App;
