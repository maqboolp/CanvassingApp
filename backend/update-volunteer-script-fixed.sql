-- Update Volunteer Script with new content
-- The [Volunteer Name] placeholder will be replaced dynamically in the frontend

UPDATE "VolunteerResources"
SET "Content" = E'Knock & Greet:\n\nHi, I''m [Volunteer Name] with Tanveer Patel''s campaign for Hoover City Council. She''s running in the August 26 election and truly wants to serve our community. Can I share a quick flyer with you?\n\nTalking Points:\n\n• Tanveer is a longtime resident of Hoover\n• She''s passionate about [insert 2-3 local issues like schools, safety, economic development]\n• She wants to hear from YOU\n\nAsk:\n\nCan we count on your support on August 26?\n(Mark down as: Yes / Undecided / No)\n\nIf supportive:\n\nWould you like a yard sign? Can we stay in touch with a reminder closer to Election Day?\n\nIf Not Home:\n\nLeave door hanger or flyer.\n\nReminders:\n\n• Always smile, be respectful\n• Don''t argue or pressure anyone\n• Keep it under 60 seconds unless they want to talk more\n• Mark your notes and move on to next door',
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "ResourceKey" = 'Script';

-- Show the updated script
SELECT "ResourceKey", "Content", "UpdatedAt"
FROM "VolunteerResources"
WHERE "ResourceKey" = 'Script';