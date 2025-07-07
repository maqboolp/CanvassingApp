export interface WalkSession {
  id: number;
  volunteerId: string;
  startedAt: string;
  endedAt?: string;
  status: 'active' | 'paused' | 'completed' | 'abandoned';
  housesVisited: number;
  votersContacted: number;
  totalDistanceMeters: number;
  durationMinutes: number;
  activeClaims: HouseClaim[];
}

export interface HouseClaim {
  id: number;
  address: string;
  latitude: number;
  longitude: number;
  claimedAt: string;
  expiresAt: string;
  status: 'claimed' | 'visiting' | 'visited' | 'expired' | 'released';
  visitedAt?: string;
}

export interface AvailableHouse {
  address: string;
  latitude: number;
  longitude: number;
  distanceMeters: number;
  voterCount: number;
  voters: AvailableHouseVoter[];
}

export interface AvailableHouseVoter {
  voterId: string;
  name: string;
  age: number;
  partyAffiliation?: string;
  voteFrequency?: 'NonVoter' | 'Infrequent' | 'Frequent';
}

export interface RouteHouse {
  address: string;
  latitude: number;
  longitude: number;
  order: number;
  distanceFromPreviousMeters: number;
  voterCount: number;
}

export interface OptimizedRoute {
  houses: RouteHouse[];
  totalDistanceMeters: number;
  estimatedDurationMinutes: number;
}

export interface ActiveCanvasser {
  volunteerId: number;
  name: string;
  latitude: number;
  longitude: number;
  lastUpdateTime: string;
  housesVisited: number;
  distanceMeters: number;
}

export interface WalkActivity {
  id: number;
  activityType: 'sessionStarted' | 'routeGenerated' | 'houseClaimed' | 'houseReleased' | 
                 'arrivedAtHouse' | 'departedHouse' | 'contactMade' | 'sessionPaused' | 
                 'sessionResumed' | 'sessionEnded';
  latitude: number;
  longitude: number;
  timestamp: string;
  description?: string;
}