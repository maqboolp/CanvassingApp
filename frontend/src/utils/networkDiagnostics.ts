import { API_BASE_URL } from '../config';

interface NetworkDiagnosticResult {
  canReachAPI: boolean;
  isHTTPS: boolean;
  hasInternetConnection: boolean;
  apiUrl: string;
  browserInfo: string;
  timestamp: string;
  errors: string[];
}

export class NetworkDiagnostics {
  static async runDiagnostics(): Promise<NetworkDiagnosticResult> {
    const result: NetworkDiagnosticResult = {
      canReachAPI: false,
      isHTTPS: window.location.protocol === 'https:',
      hasInternetConnection: navigator.onLine,
      apiUrl: API_BASE_URL,
      browserInfo: navigator.userAgent,
      timestamp: new Date().toISOString(),
      errors: []
    };

    // Check if we're on HTTPS
    if (!result.isHTTPS) {
      result.errors.push('Not using HTTPS - some browsers may block mixed content');
    }

    // Check internet connection
    if (!result.hasInternetConnection) {
      result.errors.push('No internet connection detected');
      return result;
    }

    // Try to reach the API health endpoint
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 second timeout

      const response = await fetch(`${API_BASE_URL}/api/health/simple`, {
        signal: controller.signal,
        mode: 'cors',
        credentials: 'omit' // Don't send credentials for health check
      });

      clearTimeout(timeoutId);

      result.canReachAPI = response.ok;
      
      if (!response.ok) {
        result.errors.push(`API returned status ${response.status}`);
      }
    } catch (error) {
      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          result.errors.push('Request timed out after 10 seconds');
        } else if (error.message === 'Failed to fetch') {
          result.errors.push('Cannot connect to API - possible CORS or network issue');
        } else {
          result.errors.push(`Connection error: ${error.message}`);
        }
      } else {
        result.errors.push('Unknown connection error');
      }
    }

    // Check for known mobile browser issues
    const userAgent = navigator.userAgent.toLowerCase();
    if (userAgent.includes('mobile')) {
      if (userAgent.includes('safari') && !userAgent.includes('chrome')) {
        result.errors.push('Mobile Safari detected - check for content blockers or private browsing');
      } else if (userAgent.includes('chrome')) {
        result.errors.push('Mobile Chrome detected - check for data saver mode');
      }
    }

    return result;
  }

  static async checkAPIEndpoints(): Promise<Record<string, boolean>> {
    const endpoints = [
      '/api/health/simple',
      '/api/auth/login'
    ];

    const results: Record<string, boolean> = {};

    for (const endpoint of endpoints) {
      try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
          method: endpoint === '/api/auth/login' ? 'POST' : 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
          body: endpoint === '/api/auth/login' ? JSON.stringify({ email: 'test@test.com', password: 'test' }) : undefined
        });

        // For login endpoint, we expect 401 or 400, which still means the endpoint is reachable
        results[endpoint] = response.status !== 0;
      } catch (error) {
        results[endpoint] = false;
      }
    }

    return results;
  }
}