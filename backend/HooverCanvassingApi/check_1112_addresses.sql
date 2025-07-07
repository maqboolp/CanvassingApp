-- Check for voters with addresses containing 1112
SELECT 
    lalvoterid,
    firstname,
    lastname,
    addressline,
    latitude,
    longitude,
    CASE 
        WHEN latitude IS NULL OR longitude IS NULL THEN 'Missing coordinates'
        WHEN ABS(latitude) < 1 AND ABS(longitude) < 1 THEN 'Near 0,0 - likely error'
        WHEN latitude < 33.0 OR latitude > 34.0 OR longitude < -87.5 OR longitude > -86.0 THEN 'Outside Hoover area'
        ELSE 'Coordinates present'
    END as coord_status,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', latitude, ',', longitude) as map_url
FROM "Voters"
WHERE addressline LIKE '%1112%'
ORDER BY addressline;

-- Check for any voters in typical water body coordinates
-- These are approximate ranges for common water bodies in the area
SELECT 
    COUNT(*) as voters_in_water,
    MIN(addressline) as sample_address,
    AVG(latitude) as avg_lat,
    AVG(longitude) as avg_lng
FROM "Voters"
WHERE latitude IS NOT NULL 
  AND longitude IS NOT NULL
  AND (
    -- Lake Purdy area (approximate)
    (latitude BETWEEN 33.430 AND 33.450 AND longitude BETWEEN -86.630 AND -86.600) OR
    -- Cahaba River areas
    (latitude BETWEEN 33.380 AND 33.400 AND longitude BETWEEN -86.820 AND -86.800)
  );