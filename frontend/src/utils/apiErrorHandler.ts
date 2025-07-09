import { authService } from '../services/authService';

export interface ApiError {
  message: string;
  status: number;
  isAuthError: boolean;
  type?: string;
  correlationId?: string;
  timestamp?: string;
  errors?: Record<string, string[]>;
}

export class ApiErrorHandler {
  private static errorCallbacks: ((error: ApiError) => void)[] = [];

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
    let errorType = 'UnknownError';
    let correlationId: string | undefined;
    let errors: Record<string, string[]> | undefined;
    
    try {
      const errorData = await response.json();
      
      // Handle new structured error response
      if (errorData.type) {
        errorType = errorData.type;
        errorMessage = errorData.message || errorMessage;
        correlationId = errorData.correlationId;
        errors = errorData.errors;
      } else if (errorData.errors) {
        // Handle ASP.NET Core validation errors
        errors = errorData.errors;
        // Extract first error message for display
        const firstError = Object.values(errorData.errors)[0];
        if (Array.isArray(firstError) && firstError.length > 0) {
          errorMessage = firstError[0];
        }
      } else {
        // Handle legacy error format
        errorMessage = errorData.error || errorData.message || errorMessage;
      }
    } catch (error) {
      // If response body is not JSON, use status text
      errorMessage = response.statusText || errorMessage;
    }

    // Create user-friendly messages based on status codes
    const userMessage = this.getUserFriendlyMessage(response.status, errorType, errorMessage);
    
    const apiError = new ApiError(userMessage, response.status, false, errorType, correlationId, errors);
    
    // Notify error callbacks
    this.errorCallbacks.forEach(callback => callback(apiError));
    
    throw apiError;
  }

  static getUserFriendlyMessage(status: number, type: string, defaultMessage: string): string {
    // For client errors (4xx), use the actual error message from the server
    if (status >= 400 && status < 500 && defaultMessage) {
      return defaultMessage;
    }
    
    // For server errors or when no specific message is available, use generic messages
    switch (status) {
      case 403:
        return defaultMessage || 'You do not have permission to perform this action.';
      case 404:
        return defaultMessage || 'The requested resource was not found.';
      case 409:
        return defaultMessage || 'This action conflicts with existing data. Please try again.';
      case 500:
        return 'An unexpected error occurred. Please try again later.';
      case 503:
        return 'The service is temporarily unavailable. Please try again later.';
      default:
        return defaultMessage || 'An error occurred. Please try again.';
    }
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

  static registerErrorCallback(callback: (error: ApiError) => void) {
    this.errorCallbacks.push(callback);
  }

  static unregisterErrorCallback(callback: (error: ApiError) => void) {
    this.errorCallbacks = this.errorCallbacks.filter(cb => cb !== callback);
  }

  static formatValidationErrors(errors: Record<string, string[]>): string {
    const messages: string[] = [];
    Object.entries(errors).forEach(([field, fieldErrors]) => {
      fieldErrors.forEach(error => {
        messages.push(`${field}: ${error}`);
      });
    });
    return messages.join('\n');
  }
}

// Custom error class for API errors
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public isAuthError: boolean,
    public type?: string,
    public correlationId?: string,
    public errors?: Record<string, string[]>
  ) {
    super(message);
    this.name = 'ApiError';
  }

  getDetailedMessage(): string {
    if (this.errors) {
      return `${this.message}\n${ApiErrorHandler.formatValidationErrors(this.errors)}`;
    }
    return this.message;
  }

  shouldShowDetails(): boolean {
    return this.status >= 400 && this.status < 500;
  }
}