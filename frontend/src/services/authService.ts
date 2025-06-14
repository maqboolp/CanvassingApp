import { AuthUser, LoginRequest, ApiResponse } from '../types';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:8080';

class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'auth_user';

  async login(credentials: LoginRequest): Promise<AuthUser> {
    const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credentials),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || 'Login failed');
    }

    const data: ApiResponse<AuthUser> = await response.json();
    
    if (!data.success || !data.data) {
      throw new Error(data.error || 'Login failed');
    }

    const user = data.data;
    
    // Store token and user data
    localStorage.setItem(this.TOKEN_KEY, user.token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    
    return user;
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

    const headers = {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
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
        // Refresh failed, redirect to login
        this.logout();
        window.location.href = '/login';
        throw new Error('Authentication failed');
      }
    }

    return response;
  }

  async forgotPassword(email: string): Promise<{ success: boolean; message: string }> {
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
  }

  async resetPassword(email: string, token: string, newPassword: string): Promise<{ success: boolean; message: string }> {
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
  }
}

export const authService = new AuthService();