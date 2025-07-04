# Email Templates

This directory contains Liquid templates for all system emails. The templates use the [Fluid](https://github.com/sebastienros/fluid) templating engine.

## Template Structure

- `_layout.liquid` - Base layout template with header, footer, and styling
- `[template-name].liquid` - Main HTML content for the email
- `[template-name].subject.liquid` - Email subject line template
- `[template-name].text.liquid` - Plain text version (optional)

## Available Variables

### Global Variables (available in all templates)

- `Campaign` - Campaign settings object containing:
  - `CampaignName` - e.g., "Tanveer for Hoover"
  - `CandidateName` - e.g., "Tanveer Patel"
  - `CampaignTitle` - e.g., "Tanveer Patel for Hoover City Council"
  - `PaidForBy` - e.g., "Paid for by Tanveer for Hoover"
  - `CampaignEmail` - Campaign contact email
  - `CampaignPhone` - Campaign contact phone
  - `CampaignWebsite` - Campaign website URL
  - `CampaignAddress` - Campaign mailing address
  - `Office` - Office being sought
  - `Jurisdiction` - City/County/State
- `CurrentYear` - Current year for copyright

### Template-Specific Variables

#### password-reset
- `FirstName` - User's first name
- `ResetUrl` - Password reset link
- `ResetToken` - Reset token (if needed separately)

#### volunteer-invitation
- `InviterName` - Name of person sending invitation
- `Role` - Role being invited to
- `RegistrationUrl` - Registration link

#### contact-notification
- `VolunteerName` - Name of volunteer who made contact
- `VoterName` - Voter's full name
- `VoterAddress` - Voter's address
- `VoterPhone` - Voter's phone (optional)
- `VoterEmail` - Voter's email (optional)
- `Status` - Contact status
- `VoterSupport` - Support level
- `Timestamp` - Contact date/time
- `Notes` - Contact notes
- `HasIssues` - Boolean if issues were noted
- `Issues` - Issue details
- `DashboardUrl` - Link to admin dashboard

#### registration-welcome
- `FirstName` - New user's first name
- `Email` - User's email
- `Role` - Assigned role
- `LoginUrl` - Link to login page

## Customization

To customize emails for a different campaign:

1. Update the campaign settings in `appsettings.json` or environment variables
2. Modify the color scheme in `_layout.liquid` (currently uses #673ab7 purple)
3. Add any campaign-specific sections to individual templates
4. Add new templates as needed following the naming convention

## Testing Templates

Templates can be tested by:
1. Sending test emails through the application
2. Using the Fluid playground to test template syntax
3. Viewing rendered output in email testing tools

## Adding New Templates

1. Create `[template-name].liquid` with the email content
2. Create `[template-name].subject.liquid` with the subject line
3. Optionally create `[template-name].text.liquid` for plain text version
4. Update the EmailService to use the new template
5. Document the required variables in this README