import { AuthUser, LoginRequest, ApiResponse } from '../types';
import { API_BASE_URL } from '../config';

class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'auth_user';

  async login(credentials: LoginRequest): Promise<AuthUser> {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(credentials),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || `Login failed (HTTP ${response.status})`);
      }

      const data: ApiResponse<AuthUser> = await response.json();
      
      if (!data.success || !data.data) {
        throw new Error(data.error || 'Login failed - Invalid response');
      }

      const user = data.data;
      
      // Store token and user data
      localStorage.setItem(this.TOKEN_KEY, user.token);
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
      
      return user;
    } catch (error) {
      // Enhanced error handling to distinguish between different types of failures
      if (error instanceof TypeError && error.message === 'Failed to fetch') {
        // Network error - can't reach the server
        throw new Error(`Cannot connect to server. Please check your internet connection. (API: ${API_BASE_URL})`);
      } else if (error instanceof SyntaxError) {
        // JSON parsing error
        throw new Error('Invalid response from server. Please try again.');
      } else if (error instanceof Error) {
        // Pass through other errors with original message
        throw error;
      } else {
        // Unknown error
        throw new Error('An unexpected error occurred. Please try again.');
      }
    }
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  getCurrentUser(): AuthUser | null {
    try {
      const userJson = localStorage.getItem(this.USER_KEY);
      if (!userJson) return null;
      
      const user = JSON.parse(userJson) as AuthUser;
      
      // Check if token is expired (basic check)
      if (!this.isTokenValid(user.token)) {
        this.logout();
        return null;
      }
      
      return user;
    } catch {
      this.logout();
      return null;
    }
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const user = this.getCurrentUser();
    return user !== null;
  }

  private isTokenValid(token: string): boolean {
    try {
      // Basic JWT token validation (check if not expired)
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      return payload.exp > currentTime;
    } catch {
      return false;
    }
  }

  async refreshToken(): Promise<AuthUser | null> {
    const token = this.getToken();
    if (!token) return null;

    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        this.logout();
        return null;
      }

      const data: ApiResponse<AuthUser> = await response.json();
      
      if (!data.success || !data.data) {
        this.logout();
        return null;
      }

      const user = data.data;
      
      // Update stored token and user data
      localStorage.setItem(this.TOKEN_KEY, user.token);
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
      
      return user;
    } catch {
      this.logout();
      return null;
    }
  }

  // Helper method to make authenticated API calls
  async authenticatedFetch(url: string, options: RequestInit = {}): Promise<Response> {
    const token = this.getToken();
    
    if (!token) {
      throw new Error('No authentication token available');
    }

    // Don't set Content-Type for FormData - let browser set it
    const isFormData = options.body instanceof FormData;
    const defaultHeaders: any = {
      'Authorization': `Bearer ${token}`,
    };
    
    if (!isFormData) {
      defaultHeaders['Content-Type'] = 'application/json';
    }

    const headers = {
      ...defaultHeaders,
      ...options.headers,
    };

    const response = await fetch(url, {
      ...options,
      headers,
    });

    // If unauthorized, try to refresh token once
    if (response.status === 401) {
      const refreshedUser = await this.refreshToken();
      if (refreshedUser) {
        // Retry with new token
        const newHeaders = {
          ...headers,
          'Authorization': `Bearer ${refreshedUser.token}`,
        };
        
        return fetch(url, {
          ...options,
          headers: newHeaders,
        });
      } else {
        // Refresh failed, redirect to home (which will redirect to login)
        this.logout();
        throw new Error('Authentication failed');
      }
    }

    return response;
  }

  async forgotPassword(email: string): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/forgot-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email }),
      });

      const data: ApiResponse<any> = await response.json();
      
      if (!response.ok) {
        throw new Error(data.error || 'Failed to send reset email');
      }

      return {
        success: data.success,
        message: data.message || 'Reset email sent successfully'
      };
    } catch (error) {
      if (error instanceof TypeError && error.message === 'Failed to fetch') {
        throw new Error('Cannot connect to server. Please check your internet connection.');
      } else if (error instanceof Error) {
        throw error;
      } else {
        throw new Error('Failed to send reset email');
      }
    }
  }

  async resetPassword(email: string, token: string, newPassword: string): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/reset-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ 
          email, 
          token, 
          newPassword 
        }),
      });

      const data: ApiResponse<any> = await response.json();
      
      if (!response.ok) {
        throw new Error(data.error || 'Failed to reset password');
      }

      return {
        success: data.success,
        message: data.message || 'Password reset successfully'
      };
    } catch (error) {
      if (error instanceof TypeError && error.message === 'Failed to fetch') {
        throw new Error('Cannot connect to server. Please check your internet connection.');
      } else if (error instanceof Error) {
        throw error;
      } else {
        throw new Error('Failed to reset password');
      }
    }
  }
}

export const authService = new AuthService();