export interface Voter {
  lalVoterId: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  addressLine: string;
  city: string;
  state: string;
  zip: string;
  age: number;
  ethnicity?: string;
  gender: string;
  voteFrequency: 'frequent' | 'infrequent' | 'non-voter';
  partyAffiliation?: string;
  cellPhone?: string;
  email?: string;
  latitude?: number;
  longitude?: number;
  isContacted: boolean;
  lastContactStatus?: ContactStatus;
  voterSupport?: VoterSupport;
  distanceKm?: number;
  tags?: VoterTag[];
}

export interface Volunteer {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  role: 'volunteer' | 'admin' | 'superadmin';
  isActive: boolean;
  createdAt: Date;
}

export type ContactStatus = 'reached' | 'not-home' | 'refused' | 'needs-follow-up';

export type VoterSupport = 'strongyes' | 'leaningyes' | 'undecided' | 'leaningno' | 'strongno';

export interface Contact {
  id: string;
  voterId: string;
  volunteerId: string;
  status: ContactStatus;
  voterSupport?: VoterSupport;
  notes?: string;
  timestamp: Date;
  location?: {
    latitude: number;
    longitude: number;
  };
}

// Backend ContactDto response format (JSON camelCase)
export interface ContactDto {
  id: string;
  voterId: string;
  volunteerId: string;
  status: string;
  voterSupport?: string;
  notes?: string;
  timestamp: string;
  voterName?: string;
  volunteerName?: string;
  location?: {
    latitude: number;
    longitude: number;
  };
}

export interface ContactListResponse {
  contacts: ContactDto[];
  total: number;
  page: number;
  totalPages: number;
}

export interface VoterTag {
  id: number;
  tagName: string;
  color?: string;
}

export interface VoterTagDetail extends VoterTag {
  description?: string;
  voterCount: number;
  createdAt: string;
  createdBy?: string;
}

export interface VoterFilter {
  zipCode?: string;
  voteFrequency?: 'frequent' | 'infrequent' | 'non-voter';
  ageGroup?: '18-30' | '31-50' | '51+';
  contactStatus?: 'contacted' | 'not-contacted';
  searchName?: string;
  partyAffiliation?: string;
  tagIds?: number[];
  sortBy?: string;
}

export interface PaginationParams {
  page: number;
  limit: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface VoterListResponse {
  voters: Voter[];
  total: number;
  page: number;
  totalPages: number;
}

export interface Analytics {
  totalVoters: number;
  totalContacted: number;
  contactStatusBreakdown: {
    reached: number;
    notHome: number;
    refused: number;
    needsFollowUp: number;
  };
  volunteerActivity: Array<{
    volunteerId: string;
    volunteerName: string;
    contactsToday: number;
    contactsTotal: number;
  }>;
  contactsByZip: Array<{
    zipCode: string;
    contacted: number;
    total: number;
  }>;
}

export interface RouteOptimization {
  currentLocation: {
    latitude: number;
    longitude: number;
  };
  optimizedRoute: Array<{
    voter: Voter;
    distance: number;
    estimatedTravelTime: number;
  }>;
  totalDistance: number;
  totalEstimatedTime: number;
}

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'volunteer' | 'admin' | 'superadmin';
  token: string;
  avatarUrl: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

export interface OptInRequest {
  phoneNumber: string;
  consentGiven: boolean;
  firstName?: string;
  lastName?: string;
  email?: string;
  zipCode?: string;
}

export interface OptInResponse {
  success: boolean;
  message: string;
  voterId?: string;
}