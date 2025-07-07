import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { authService } from './authService';
import { API_BASE_URL } from '../config';

export interface WalkHubUpdate {
  type: 'HouseClaimed' | 'HouseReleased' | 'HouseCompleted' | 'CanvasserLocationUpdate' | 'CanvasserJoined' | 'CanvasserLeft';
  houseId?: number;
  address?: string;
  volunteerId?: number;
  volunteerName?: string;
  latitude?: number;
  longitude?: number;
  timestamp: string;
}

export interface ActiveCanvasser {
  volunteerId: number;
  name: string;
  latitude: number;
  longitude: number;
  distanceMeters: number;
  housesVisited: number;
  lastUpdateTime: string;
}

export interface WalkHubCallbacks {
  onHouseStatusUpdate?: (update: WalkHubUpdate) => void;
  onCanvasserLocationUpdate?: (update: WalkHubUpdate) => void;
  onCanvasserUpdate?: (update: WalkHubUpdate) => void;
  onNearbyCanvassers?: (canvassers: ActiveCanvasser[]) => void;
  onConnectionStateChange?: (connected: boolean) => void;
}

class WalkHubService {
  private connection: HubConnection | null = null;
  private callbacks: WalkHubCallbacks = {};
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;

  constructor() {
    this.setupConnection();
  }

  private setupConnection() {
    if (this.connection) {
      this.connection.stop();
    }

    const token = authService.getToken();
    if (!token) {
      console.warn('No authentication token available for SignalR connection');
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/walk`
        // Temporarily disable authentication for testing
        // , {
        //   accessTokenFactory: () => token
        // }
      )
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount < 3) {
            return 2000; // 2 seconds
          } else if (retryContext.previousRetryCount < 6) {
            return 5000; // 5 seconds
          } else {
            return 10000; // 10 seconds
          }
        }
      })
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    this.connection.on('HouseStatusUpdate', (update: WalkHubUpdate) => {
      console.log('House status update:', update);
      this.callbacks.onHouseStatusUpdate?.(update);
    });

    this.connection.on('CanvasserLocationUpdate', (update: WalkHubUpdate) => {
      console.log('Canvasser location update:', update);
      this.callbacks.onCanvasserLocationUpdate?.(update);
    });

    this.connection.on('CanvasserUpdate', (update: WalkHubUpdate) => {
      console.log('Canvasser update:', update);
      this.callbacks.onCanvasserUpdate?.(update);
    });

    this.connection.on('NearbyCanvassers', (canvassers: ActiveCanvasser[]) => {
      console.log('Nearby canvassers:', canvassers);
      this.callbacks.onNearbyCanvassers?.(canvassers);
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error);
      this.isConnected = false;
      this.callbacks.onConnectionStateChange?.(false);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      this.isConnected = true;
      this.reconnectAttempts = 0;
      this.callbacks.onConnectionStateChange?.(true);
    });

    this.connection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      this.isConnected = false;
      this.callbacks.onConnectionStateChange?.(false);
      
      // Attempt to reconnect after a delay
      setTimeout(() => {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
          this.reconnectAttempts++;
          this.connect();
        }
      }, 5000);
    });
  }

  public setCallbacks(callbacks: WalkHubCallbacks) {
    this.callbacks = { ...this.callbacks, ...callbacks };
  }

  public async connect(): Promise<void> {
    if (!this.connection) {
      this.setupConnection();
    }

    if (!this.connection) {
      console.error('Failed to setup SignalR connection');
      return;
    }

    if (this.connection.state === 'Disconnected') {
      try {
        console.log(`Attempting to connect to SignalR at: ${API_BASE_URL}/hubs/walk`);
        await this.connection.start();
        this.isConnected = true;
        this.reconnectAttempts = 0;
        console.log('SignalR connected successfully');
        this.callbacks.onConnectionStateChange?.(true);
      } catch (error) {
        console.error('SignalR connection failed:', error);
        this.isConnected = false;
        this.callbacks.onConnectionStateChange?.(false);
        // Don't throw the error to prevent component from crashing
        console.warn('SignalR connection will be retried later');
      }
    } else if (this.connection.state === 'Connected') {
      console.log('SignalR already connected');
      this.isConnected = true;
      this.callbacks.onConnectionStateChange?.(true);
    }
  }

  public async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.isConnected = false;
      this.callbacks.onConnectionStateChange?.(false);
    }
  }

  public async joinWalkSession(latitude: number, longitude: number): Promise<void> {
    if (!this.isConnected || !this.connection) {
      throw new Error('SignalR not connected');
    }

    try {
      await this.connection.invoke('JoinWalkSession', latitude, longitude);
      console.log('Joined walk session');
    } catch (error) {
      console.error('Failed to join walk session:', error);
      throw error;
    }
  }

  public async leaveWalkSession(): Promise<void> {
    if (!this.isConnected || !this.connection) {
      return;
    }

    try {
      await this.connection.invoke('LeaveWalkSession');
      console.log('Left walk session');
    } catch (error) {
      console.error('Failed to leave walk session:', error);
    }
  }

  public async updateLocation(latitude: number, longitude: number): Promise<void> {
    if (!this.isConnected || !this.connection) {
      return;
    }

    try {
      await this.connection.invoke('UpdateLocation', latitude, longitude);
    } catch (error) {
      console.error('Failed to update location:', error);
    }
  }

  public async getNearbyCanvassers(
    latitude: number, 
    longitude: number, 
    radiusKm: number = 2.0
  ): Promise<void> {
    if (!this.isConnected || !this.connection) {
      return;
    }

    try {
      await this.connection.invoke('GetNearbyCanvassers', latitude, longitude, radiusKm);
    } catch (error) {
      console.error('Failed to get nearby canvassers:', error);
    }
  }

  public get connected(): boolean {
    return this.isConnected;
  }

  public get connectionState(): string {
    return this.connection?.state || 'Disconnected';
  }
}

export const walkHubService = new WalkHubService();