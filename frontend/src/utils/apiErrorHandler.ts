import { authService } from '../services/authService';

export interface ApiError {
  message: string;
  status: number;
  isAuthError: boolean;
}

export class ApiErrorHandler {
  static async handleResponse(response: Response): Promise<any> {
    if (response.ok) {
      try {
        return await response.json();
      } catch (error) {
        // Handle empty response bodies
        return {};
      }
    }

    // Handle authentication errors
    if (response.status === 401) {
      this.handleAuthError();
      throw new ApiError('Your session has expired. Please log in again.', 401, true);
    }

    // Handle other errors
    let errorMessage = `Request failed with status ${response.status}`;
    try {
      const errorData = await response.json();
      errorMessage = errorData.error || errorData.message || errorMessage;
    } catch (error) {
      // If response body is not JSON, use status text
      errorMessage = response.statusText || errorMessage;
    }

    throw new ApiError(errorMessage, response.status, false);
  }

  static handleAuthError() {
    // Clear authentication data
    authService.logout();
    
    // Show user-friendly message
    alert('Your session has expired. You will be redirected to the login page.');
    
    // Redirect to login (or home page which will redirect to login)
    window.location.href = '/';
  }

  static async makeAuthenticatedRequest(url: string, options: RequestInit = {}): Promise<any> {
    try {
      const response = await authService.authenticatedFetch(url, options);
      return await this.handleResponse(response);
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }
      // Handle network errors or other unexpected errors
      throw new ApiError('Network error. Please check your connection and try again.', 0, false);
    }
  }
}

// Custom error class for API errors
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public isAuthError: boolean
  ) {
    super(message);
    this.name = 'ApiError';
  }
}