// Geocoding service using Mapbox Geocoding API
const MAPBOX_TOKEN = process.env.REACT_APP_MAPBOX_TOKEN || 'pk.eyJ1IjoibWFxYm9vbHAiLCJhIjoiY21jcnlkZHRoMHJwMTJrcTc0OW53YjI4ayJ9.Z-OEVxJnAN8QmLeN57yqJg';

interface GeocodeResult {
  lat: number;
  lng: number;
  confidence: number;
  type: string;
}

class GeocodingService {
  private cache: Map<string, GeocodeResult> = new Map();
  private pendingRequests: Map<string, Promise<GeocodeResult | null>> = new Map();

  async geocodeAddress(address: string, city: string = 'Hoover', state: string = 'AL', zip: string = ''): Promise<GeocodeResult | null> {
    // Build full address
    const fullAddress = `${address}, ${city}, ${state} ${zip}`.trim();
    
    // Check cache first
    if (this.cache.has(fullAddress)) {
      return this.cache.get(fullAddress)!;
    }

    // Check if request is already pending
    if (this.pendingRequests.has(fullAddress)) {
      return this.pendingRequests.get(fullAddress)!;
    }

    // Create new geocoding request
    const requestPromise = this.performGeocode(fullAddress);
    this.pendingRequests.set(fullAddress, requestPromise);

    try {
      const result = await requestPromise;
      if (result) {
        this.cache.set(fullAddress, result);
      }
      return result;
    } finally {
      this.pendingRequests.delete(fullAddress);
    }
  }

  private async performGeocode(address: string): Promise<GeocodeResult | null> {
    try {
      // Encode the address for URL
      const encodedAddress = encodeURIComponent(address);
      
      // Add bbox for Hoover, AL area to improve results
      const bbox = '-87.0,33.3,-86.6,33.5'; // West, South, East, North
      
      const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodedAddress}.json?` +
        `access_token=${MAPBOX_TOKEN}&` +
        `bbox=${bbox}&` +
        `limit=1&` +
        `types=address`;

      const response = await fetch(url);
      
      if (!response.ok) {
        console.error('Geocoding failed:', response.status, response.statusText);
        return null;
      }

      const data = await response.json();
      
      if (data.features && data.features.length > 0) {
        const feature = data.features[0];
        const [lng, lat] = feature.center;
        
        // Check if the result is in a reasonable area (not in water)
        if (this.isLocationReasonable(lat, lng)) {
          return {
            lat,
            lng,
            confidence: feature.relevance || 0,
            type: feature.place_type?.[0] || 'unknown'
          };
        } else {
          console.warn(`Geocoded location appears to be in water for ${address}:`, lat, lng);
          // Try alternative geocoding with street-level approximation
          return this.approximateStreetLocation(address, lat, lng);
        }
      }

      console.warn('No geocoding results found for:', address);
      return null;
    } catch (error) {
      console.error('Geocoding error:', error);
      return null;
    }
  }

  private isLocationReasonable(lat: number, lng: number): boolean {
    // Known water body areas in Hoover
    const waterBodies = [
      { name: 'Lake Purdy', minLat: 33.430, maxLat: 33.450, minLng: -86.630, maxLng: -86.600 },
      { name: 'Cahaba River', minLat: 33.380, maxLat: 33.400, minLng: -86.820, maxLng: -86.800 }
    ];

    // Check if location is in any known water body
    for (const water of waterBodies) {
      if (lat >= water.minLat && lat <= water.maxLat && 
          lng >= water.minLng && lng <= water.maxLng) {
        return false;
      }
    }

    return true;
  }

  private async approximateStreetLocation(address: string, waterLat: number, waterLng: number): Promise<GeocodeResult | null> {
    // Extract street name from address
    const streetMatch = address.match(/^\d+\s+(.+?)(?:,|$)/);
    if (!streetMatch) return null;

    const streetName = streetMatch[1];
    
    try {
      // Search for the street without house number
      const encodedStreet = encodeURIComponent(`${streetName}, Hoover, AL`);
      const bbox = '-87.0,33.3,-86.6,33.5';
      
      const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodedStreet}.json?` +
        `access_token=${MAPBOX_TOKEN}&` +
        `bbox=${bbox}&` +
        `limit=1&` +
        `types=address,street`;

      const response = await fetch(url);
      
      if (!response.ok) return null;

      const data = await response.json();
      
      if (data.features && data.features.length > 0) {
        const feature = data.features[0];
        const [streetLng, streetLat] = feature.center;
        
        // If street center is also in water, offset it slightly
        if (!this.isLocationReasonable(streetLat, streetLng)) {
          // Move the point slightly north/east (typical direction away from water in this area)
          return {
            lat: streetLat + 0.001, // ~111 meters north
            lng: streetLng + 0.001, // ~90 meters east at this latitude
            confidence: 0.5,
            type: 'approximated'
          };
        }
        
        return {
          lat: streetLat,
          lng: streetLng,
          confidence: 0.7,
          type: 'street_center'
        };
      }
    } catch (error) {
      console.error('Street approximation failed:', error);
    }

    return null;
  }

  // Batch geocode multiple addresses efficiently
  async batchGeocode(houses: Array<{ address: string; city?: string; state?: string; zip?: string }>): Promise<Map<string, GeocodeResult | null>> {
    const results = new Map<string, GeocodeResult | null>();
    
    // Process in batches to avoid rate limiting
    const batchSize = 10;
    for (let i = 0; i < houses.length; i += batchSize) {
      const batch = houses.slice(i, i + batchSize);
      
      // Process batch in parallel
      const batchPromises = batch.map(house => 
        this.geocodeAddress(house.address, house.city, house.state, house.zip)
          .then(result => ({ address: house.address, result }))
      );
      
      const batchResults = await Promise.all(batchPromises);
      
      for (const { address, result } of batchResults) {
        results.set(address, result);
      }
      
      // Add small delay between batches to respect rate limits
      if (i + batchSize < houses.length) {
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    }
    
    return results;
  }

  // Clear cache if needed
  clearCache() {
    this.cache.clear();
  }

  // Get cache stats
  getCacheStats() {
    return {
      size: this.cache.size,
      entries: Array.from(this.cache.entries()).map(([address, result]) => ({
        address,
        lat: result.lat,
        lng: result.lng,
        confidence: result.confidence
      }))
    };
  }
}

// Export singleton instance
export const geocodingService = new GeocodingService();

// Also export the class for testing
export { GeocodingService };