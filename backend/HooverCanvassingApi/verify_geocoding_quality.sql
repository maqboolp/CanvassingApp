-- Check geocoding quality for Cahaba River addresses
SELECT 
    lalvoterid,
    addressline,
    city,
    zip,
    latitude,
    longitude,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', latitude, ',', longitude) as current_location,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', REPLACE(addressline, ' ', '+'), '+', REPLACE(city, ' ', '+'), '+AL+', zip) as search_address
FROM "Voters"
WHERE addressline LIKE '%Cahaba River%'
ORDER BY addressline;

-- Find all voters that might be in water (suspicious coordinates)
-- These coordinates are approximate water body areas in Hoover
SELECT COUNT(*) as voters_possibly_in_water
FROM "Voters"
WHERE latitude IS NOT NULL 
  AND longitude IS NOT NULL
  AND (
    -- Lake Purdy area
    (latitude BETWEEN 33.430 AND 33.450 AND longitude BETWEEN -86.630 AND -86.600) OR
    -- Cahaba River areas
    (latitude BETWEEN 33.380 AND 33.400 AND longitude BETWEEN -86.820 AND -86.800) OR
    -- Check for coordinates that are exactly on round numbers (often indicates approximation)
    (ROUND(latitude::numeric, 3) = latitude AND ROUND(longitude::numeric, 3) = longitude)
  );