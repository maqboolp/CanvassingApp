#!/usr/bin/env python3
"""
Script to update hardcoded campaign references to use configuration variables
"""

import re
import os

# Define replacements for backend C# files
cs_replacements = [
    # Email subjects
    (r'"Reset Your Password - Tanveer for Hoover Campaign"', r'$"Reset Your Password - {_campaignSettings.CampaignName}"'),
    (r'"New Voter Contact - Tanveer for Hoover Campaign"', r'$"New Voter Contact - {_campaignSettings.CampaignName}"'),
    (r'"🗑️ Contact Deleted - Tanveer for Hoover Campaign"', r'$"🗑️ Contact Deleted - {_campaignSettings.CampaignName}"'),
    (r'"You\'re Invited to Join Tanveer for Hoover Campaign"', r'$"You\'re Invited to Join {_campaignSettings.CampaignName}"'),
    (r'"Welcome to Tanveer for Hoover Campaign Team!"', r'$"Welcome to {_campaignSettings.CampaignName} Team!"'),
    (r'"Registration Update - Tanveer for Hoover Campaign"', r'$"Registration Update - {_campaignSettings.CampaignName}"'),
    
    # HTML content
    (r'<h1 style=\'color: #673ab7; margin: 0;\'>Tanveer for Hoover Campaign</h1>', 
     r'<h1 style=\'color: #673ab7; margin: 0;\'>{_campaignSettings.CampaignName}</h1>'),
    (r'Tanveer for Hoover Campaign Team', r'{_campaignSettings.CampaignName} Team'),
    (r'The Tanveer for Hoover Campaign Team', r'The {_campaignSettings.CampaignName} Team'),
    (r'Tanveer Patel for Hoover City Council', r'{_campaignSettings.CampaignTitle}'),
    (r'Paid for by Tanveer for Hoover', r'{_campaignSettings.PaidForBy}'),
    
    # Opt-in text
    (r'"I agree to receive texts and robocalls from Tanveer for Hoover. Message and data rates may apply. Reply STOP to opt out."',
     r'_campaignSettings.OptInConsentText'),
     
    # Canvassing script
    (r'"Hi, my name is \[Your Name\] and I\'m a volunteer for Tanveer Patel\'s campaign for Hoover City Council\\.\\n\\nI\'d like to take just a moment to talk to you about the upcoming election\. Tanveer is running to bring fresh perspectives and innovative solutions to our community\\.\\n\\nAre you planning to vote in the upcoming election\?"',
     r'_campaignSettings.DefaultCanvassingScript'),
]

# Define replacements for TypeScript/React files
ts_replacements = [
    # Frontend references
    (r'Welcome to the Tanveer for Hoover Campaign Team!', r'Welcome to the {campaignName} Team!'),
    (r'How does this voter feel about Tanveer\'s candidacy\?', r'How does this voter feel about {candidateName}\'s candidacy?'),
    (r'Strong Yes - Will vote for Tanveer', r'Strong Yes - Will vote for {candidateName}'),
    (r'Leaning Yes - May vote for Tanveer', r'Leaning Yes - May vote for {candidateName}'),
    (r'Leaning No - Not into Tanveer', r'Leaning No - Not into {candidateName}'),
    (r'Strong No - Definitely not voting for Tanveer', r'Strong No - Definitely not voting for {candidateName}'),
    (r'Thank you for joining Tanveer for Hoover\'s campaign updates!', r'Thank you for joining {campaignName}\'s campaign updates!'),
    (r'I agree to receive texts and robocalls from Tanveer for Hoover\.', r'I agree to receive texts and robocalls from {campaignName}.'),
]

# Update Voter.cs enum comments separately
voter_enum_replacements = [
    (r'// Strong yes - will Vote for Tanveer', r'// Strong yes - will vote for the candidate'),
    (r'// Leaning yes - May vote for Tanveer - but hadn\'t heard of her before, or was a little softer enthusiasm', 
     r'// Leaning yes - May vote for the candidate - but hadn\'t heard of them before, or was a little softer enthusiasm'),
    (r'// Leaning against - Not into Tanveer', r'// Leaning against - Not supportive of the candidate'),
    (r'// Strong no - Definitely not voting for Tanveer', r'// Strong no - Definitely not voting for the candidate'),
]

def update_file(filepath, replacements):
    """Update a file with the given replacements"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        for pattern, replacement in replacements:
            content = re.sub(pattern, replacement, content)
        
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated: {filepath}")
            return True
        return False
    except Exception as e:
        print(f"Error updating {filepath}: {e}")
        return False

def main():
    """Main function to update all files"""
    backend_dir = "/Users/maqbool.patel/Tanveer4Hoover/hoover-canvassing-app/backend"
    frontend_dir = "/Users/maqbool.patel/Tanveer4Hoover/hoover-canvassing-app/frontend"
    
    files_updated = 0
    
    # Update EmailService.cs
    email_service_path = os.path.join(backend_dir, "HooverCanvassingApi/Services/EmailService.cs")
    if update_file(email_service_path, cs_replacements):
        files_updated += 1
    
    # Update other backend files
    backend_files = [
        "HooverCanvassingApi/Controllers/AdminController.cs",
        "HooverCanvassingApi/Controllers/OptInController.cs",
        "HooverCanvassingApi/Controllers/VolunteerResourcesController.cs",
    ]
    
    for file in backend_files:
        filepath = os.path.join(backend_dir, file)
        if update_file(filepath, cs_replacements):
            files_updated += 1
    
    # Update Voter.cs enum comments
    voter_path = os.path.join(backend_dir, "HooverCanvassingApi/Models/Voter.cs")
    if update_file(voter_path, voter_enum_replacements):
        files_updated += 1
    
    # Update frontend files
    frontend_files = [
        "src/components/CompleteRegistration.tsx",
        "src/components/ContactModal.tsx",
        "src/components/OptInForm.tsx",
    ]
    
    for file in frontend_files:
        filepath = os.path.join(frontend_dir, file)
        if update_file(filepath, ts_replacements):
            files_updated += 1
    
    print(f"\nTotal files updated: {files_updated}")
    print("\nNOTE: Frontend files will need environment variables configured:")
    print("- REACT_APP_CANDIDATE_NAME")
    print("- REACT_APP_CAMPAIGN_NAME")
    print("- REACT_APP_CAMPAIGN_TITLE")
    print("- REACT_APP_CONSENT_TEXT")

if __name__ == "__main__":
    main()