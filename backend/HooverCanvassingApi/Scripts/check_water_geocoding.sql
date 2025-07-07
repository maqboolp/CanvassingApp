-- Check for voters with addresses containing "1112" or similar patterns
-- and their geocoding coordinates

-- 1. Find all voters with addresses containing "1112"
SELECT 
    lalvoterid,
    firstname,
    lastname,
    addressline,
    city,
    state,
    zip,
    latitude,
    longitude,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', latitude, ',', longitude) as google_maps_url
FROM Voters
WHERE addressline LIKE '%1112%'
AND latitude IS NOT NULL 
AND longitude IS NOT NULL
ORDER BY addressline;

-- 2. Find voters with similar address patterns (1111, 1113, 1114, etc.)
SELECT 
    lalvoterid,
    firstname,
    lastname,
    addressline,
    city,
    state,
    zip,
    latitude,
    longitude,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', latitude, ',', longitude) as google_maps_url
FROM Voters
WHERE (
    addressline LIKE '%1111%' OR
    addressline LIKE '%1112%' OR
    addressline LIKE '%1113%' OR
    addressline LIKE '%1114%' OR
    addressline LIKE '%1110%'
)
AND latitude IS NOT NULL 
AND longitude IS NOT NULL
ORDER BY addressline;

-- 3. Check for voters with coordinates that might be in water bodies
-- (Known water body areas in Hoover/Birmingham)
SELECT 
    lalvoterid,
    firstname,
    lastname,
    addressline,
    city,
    state,
    zip,
    latitude,
    longitude,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', latitude, ',', longitude) as google_maps_url,
    CASE 
        -- Lake Purdy area
        WHEN latitude BETWEEN 33.420 AND 33.460 AND longitude BETWEEN -86.680 AND -86.620 THEN 'Possible Lake Purdy'
        -- Cahaba River area
        WHEN latitude BETWEEN 33.300 AND 33.500 AND longitude BETWEEN -86.850 AND -86.750 THEN 'Possible Cahaba River'
        -- Black Creek area
        WHEN latitude BETWEEN 33.350 AND 33.450 AND longitude BETWEEN -86.820 AND -86.780 THEN 'Possible Black Creek'
        ELSE 'Check coordinates'
    END as potential_water_body
FROM Voters
WHERE latitude IS NOT NULL 
AND longitude IS NOT NULL
AND (
    -- Check for water body coordinates
    (latitude BETWEEN 33.420 AND 33.460 AND longitude BETWEEN -86.680 AND -86.620) OR
    (latitude BETWEEN 33.300 AND 33.500 AND longitude BETWEEN -86.850 AND -86.750) OR
    (latitude BETWEEN 33.350 AND 33.450 AND longitude BETWEEN -86.820 AND -86.780) OR
    -- Check for water-related keywords in address
    addressline LIKE '%Lake%' OR
    addressline LIKE '%River%' OR
    addressline LIKE '%Creek%' OR
    addressline LIKE '%Water%' OR
    addressline LIKE '%Bridge%'
)
ORDER BY potential_water_body, addressline;

-- 4. Summary statistics
SELECT 
    COUNT(*) as total_voters,
    COUNT(CASE WHEN latitude IS NOT NULL AND longitude IS NOT NULL THEN 1 END) as voters_with_coordinates,
    COUNT(CASE WHEN latitude IS NULL OR longitude IS NULL THEN 1 END) as voters_without_coordinates,
    COUNT(CASE WHEN addressline LIKE '%1112%' THEN 1 END) as voters_with_1112_address,
    COUNT(CASE WHEN addressline LIKE '%1112%' AND latitude IS NOT NULL AND longitude IS NOT NULL THEN 1 END) as voters_1112_with_coords
FROM Voters;

-- 5. Find voters with suspicious coordinate precision (too few decimal places)
SELECT 
    lalvoterid,
    firstname,
    lastname,
    addressline,
    city,
    state,
    zip,
    latitude,
    longitude,
    LENGTH(CAST(latitude AS VARCHAR)) - LENGTH(REPLACE(CAST(latitude AS VARCHAR), '.', '')) as lat_decimals,
    LENGTH(CAST(longitude AS VARCHAR)) - LENGTH(REPLACE(CAST(longitude AS VARCHAR), '.', '')) as lon_decimals
FROM Voters
WHERE latitude IS NOT NULL 
AND longitude IS NOT NULL
AND (
    LENGTH(SUBSTRING(CAST(latitude AS VARCHAR), CHARINDEX('.', CAST(latitude AS VARCHAR)) + 1, 100)) < 4 OR
    LENGTH(SUBSTRING(CAST(longitude AS VARCHAR), CHARINDEX('.', CAST(longitude AS VARCHAR)) + 1, 100)) < 4
)
ORDER BY addressline;

-- 6. Export data for manual review
-- This query exports voters with potentially incorrect geocoding for manual verification
SELECT 
    lalvoterid,
    CONCAT(firstname, ' ', lastname) as full_name,
    addressline,
    city,
    state,
    zip,
    latitude,
    longitude,
    CONCAT(addressline, ', ', city, ', ', state, ' ', zip) as full_address,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', latitude, ',', longitude) as current_location_url,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', REPLACE(addressline, ' ', '+'), ',+', REPLACE(city, ' ', '+'), ',+', state, '+', zip) as address_search_url
FROM Voters
WHERE addressline LIKE '%1112%'
AND latitude IS NOT NULL 
AND longitude IS NOT NULL
ORDER BY addressline;