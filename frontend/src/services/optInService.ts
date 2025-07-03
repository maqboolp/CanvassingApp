import { OptInRequest, OptInResponse } from '../types';
import { API_BASE_URL } from '../config';

class OptInService {
  private apiUrl = `${API_BASE_URL}/api/optin`;

  async submitOptIn(data: OptInRequest): Promise<OptInResponse> {
    const response = await fetch(`${this.apiUrl}/web-form`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    const responseData = await response.json();

    if (!response.ok) {
      throw new Error(responseData.message || 'Failed to submit opt-in');
    }

    return responseData;
  }

  async checkOptInStatus(phoneNumber: string): Promise<{
    phoneNumber: string;
    consentStatus: string;
    optInDate?: string;
    optOutDate?: string;
    optInMethod?: string;
  }> {
    const response = await fetch(`${this.apiUrl}/status/${encodeURIComponent(phoneNumber)}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error('Failed to check opt-in status');
    }

    return response.json();
  }
}

export const optInService = new OptInService();