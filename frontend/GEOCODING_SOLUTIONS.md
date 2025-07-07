# Geocoding Solutions for Hoover Canvassing App

## Problem Summary
The current implementation has markers appearing in water bodies (ponds, lakes) instead of at correct house addresses. This is due to Google's geocoding API placing certain addresses incorrectly. The issue is compounded by markers appearing to move unpredictably during map zoom operations.

## Implemented Solutions

### 1. Map Provider Comparison Tool
Created `MapComparison.tsx` that allows switching between three different map implementations:

#### a) Original Mapbox Implementation
- Individual markers for each house
- May experience shifting during zoom due to projection issues
- Uses stored coordinates from database

#### b) Improved Mapbox with Clustering
- Groups houses by street for better visualization
- Includes satellite view option
- Address search functionality
- Reduces visual clutter with clustering
- More stable marker positioning

#### c) OpenStreetMap with Leaflet
- Uses Leaflet library with OpenStreetMap tiles
- Automatic marker clustering
- Often provides better residential address accuracy
- More predictable zoom behavior
- Option to switch between street and satellite views

### 2. Frontend Geocoding Service
Created `geocodingService.ts` that:
- Detects coordinates in known water bodies
- Attempts to re-geocode using Mapbox API
- Caches results to reduce API calls
- Falls back to street-level approximation when needed

### 3. Backend Geocoding Controller
Created `GeocodingController.cs` that:
- Verifies water-placed coordinates
- Re-geocodes addresses using Google Maps API with bounds
- Bulk fixes water locations
- Only updates with ROOFTOP or RANGE_INTERPOLATED accuracy

## How to Use the Solutions

### For End Users
1. Click the map provider selector at the top of the map
2. Try different providers to see which works best for your area:
   - **Mapbox (Original)**: If you need to see exact stored coordinates
   - **Mapbox (Clustered)**: For cleaner visualization and street grouping
   - **OpenStreetMap**: Often best for residential accuracy

### For Administrators
1. Run the geocoding verification endpoint to identify addresses in water:
   ```
   GET /api/geocoding/verify-water-locations
   ```

2. Fix individual addresses:
   ```
   POST /api/geocoding/regeoccode-address
   {
     "voterId": "12345",
     "oldLatitude": 33.4055,
     "oldLongitude": -86.8125
   }
   ```

3. Bulk fix all water-placed addresses:
   ```
   POST /api/geocoding/bulk-fix-water-locations
   ```

## Database Cleanup SQL
Use the provided SQL script to manually fix known problematic addresses:
```sql
-- See backend/HooverCanvassingApi/fix_cahaba_river_coordinates.sql
```

## Recommendations

### Short Term
1. Use the **OpenStreetMap** view for canvassing operations as it typically provides better residential accuracy
2. Run the bulk geocoding fix to correct existing water-placed addresses
3. Train canvassers to use the clustering view to reduce confusion

### Long Term
1. Consider switching to a geocoding provider that specializes in residential addresses (e.g., SmartyStreets, HERE)
2. Implement address validation during data import
3. Add manual coordinate adjustment feature for administrators
4. Store both original and corrected coordinates for audit trail

## Technical Details

### Known Water Bodies in Hoover
- Lake Purdy: 33.430-33.450 N, 86.600-86.630 W
- Cahaba River: 33.380-33.400 N, 86.800-86.820 W

### Geocoding Accuracy Levels
- **ROOFTOP**: Most accurate, exact building location
- **RANGE_INTERPOLATED**: Good accuracy, interpolated along street
- **GEOMETRIC_CENTER**: Less accurate, center of area
- **APPROXIMATE**: Least accurate, general area only

### API Rate Limits
- Google Maps: 50 requests/second
- Mapbox: 600 requests/minute
- Implement delays between bulk operations

## Troubleshooting

### Markers Still in Water
1. Check if coordinates are in the known water body ranges
2. Verify geocoding API key is configured correctly
3. Try manual geocoding with tighter bounds
4. Consider using street view to verify actual location

### Markers Moving During Zoom
1. Switch to OpenStreetMap view for more stable behavior
2. Use clustering view to group nearby markers
3. Check browser console for WebGL errors
4. Ensure map container has fixed dimensions

### Performance Issues
1. Limit number of visible markers using clustering
2. Implement viewport-based loading
3. Cache geocoding results
4. Use web workers for heavy calculations