-- Add audio fields to Contacts table
ALTER TABLE "Contacts"
ADD COLUMN "AudioFileUrl" VARCHAR(500),
ADD COLUMN "AudioDurationSeconds" INTEGER;

-- Show the updated table structure
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'Contacts'
ORDER BY ordinal_position;