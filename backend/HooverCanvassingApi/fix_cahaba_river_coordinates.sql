-- First, let's see what coordinates we have for Cahaba River Est addresses
SELECT 
    lalvoterid,
    addressline,
    latitude,
    longitude,
    CONCAT('https://www.google.com/maps/search/?api=1&query=', latitude, ',', longitude) as current_location_url
FROM "Voters"
WHERE addressline LIKE '%Cahaba River%'
ORDER BY addressline;

-- Manual coordinate fixes for specific addresses
-- You'll need to get the correct coordinates from Google Maps by:
-- 1. Go to Google Maps
-- 2. Search for the address
-- 3. Right-click on the correct location
-- 4. Click "What's here?"
-- 5. Copy the coordinates from the popup

-- Example fixes (you need to replace with actual correct coordinates):
UPDATE "Voters"
SET latitude = 33.4057, -- Replace with correct latitude
    longitude = -86.8123 -- Replace with correct longitude
WHERE addressline = '671 Cahaba River Est';

-- Fix multiple addresses at once if they're on the same street
-- This estimates positions based on house numbers
WITH street_fixes AS (
    SELECT 
        lalvoterid,
        addressline,
        -- Extract house number
        CAST(SUBSTRING(addressline FROM '^\d+') AS INTEGER) as house_number,
        -- Base coordinates for the street (you need to set these)
        33.4055 as street_start_lat,  -- Latitude at beginning of street
        -86.8125 as street_start_lng, -- Longitude at beginning of street
        33.4060 as street_end_lat,    -- Latitude at end of street
        -86.8120 as street_end_lng    -- Longitude at end of street
    FROM "Voters"
    WHERE addressline LIKE '%Cahaba River Est'
      AND addressline ~ '^\d+'  -- Has house number
)
SELECT 
    lalvoterid,
    addressline,
    house_number,
    -- Interpolate position based on house number
    -- Assuming house numbers 600-700 on this street
    street_start_lat + (house_number - 600) * (street_end_lat - street_start_lat) / 100 as new_latitude,
    street_start_lng + (house_number - 600) * (street_end_lng - street_start_lng) / 100 as new_longitude
FROM street_fixes
ORDER BY house_number;

-- Once you verify the calculated positions look good, apply them:
-- UPDATE "Voters" v
-- SET 
--     latitude = sf.new_latitude,
--     longitude = sf.new_longitude
-- FROM (
--     -- Same CTE as above
-- ) sf
-- WHERE v.lalvoterid = sf.lalvoterid;