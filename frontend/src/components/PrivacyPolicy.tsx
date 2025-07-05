import React from 'react';
import { Container, Typography, Box, Link, List, ListItem } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { campaignConfig } from '../config/customerConfig';

const PrivacyPolicy: React.FC = () => {
  const campaignName = campaignConfig.campaignName || 'Campaign';
  const campaignTitle = campaignConfig.campaignTitle || 'Campaign';
  const campaignWebsite = campaignConfig.campaignWebsite || window.location.origin;
  // Generate contact email from campaign website domain or use configured email
  const getContactEmail = () => {
    if (campaignConfig.contactEmail) return campaignConfig.contactEmail;
    if (campaignConfig.supportEmail) return campaignConfig.supportEmail;
    if (campaignConfig.campaignWebsite) {
      // Extract domain from website URL
      try {
        const url = new URL(campaignConfig.campaignWebsite);
        const domain = url.hostname.replace('www.', '');
        return `info@${domain}`;
      } catch (e) {
        // If URL parsing fails, fallback
      }
    }
    return 'info@campaign.com';
  };
  const contactEmail = getContactEmail();
  const optInUrl = `${window.location.origin}/opt-in`;

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Typography variant="h3" component="h1" gutterBottom>
        Privacy Policy - {campaignName}
      </Typography>
      
      <Typography variant="body2" color="text.secondary" paragraph>
        <strong>Last Updated: {new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}</strong>
      </Typography>

      <Typography paragraph>
        Welcome to the {campaignName} campaign. This Privacy Policy explains how we collect, use, disclose, and protect your
        personal information when you opt-in to receive text messages and robocalls from our campaign. We are committed to protecting your
        privacy and complying with the Telephone Consumer Protection Act (TCPA), CTIA Messaging Principles, and other applicable
        regulations.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        1. Information We Collect
      </Typography>
      <Typography paragraph>
        We collect the following information when you opt-in via our website:
      </Typography>
      <List>
        <ListItem>
          <Typography>
            <strong>Phone Number</strong>: Required to send you text messages and robocalls, collected via our opt-in form at{' '}
            <Link component={RouterLink} to="/opt-in">{optInUrl}</Link>.
          </Typography>
        </ListItem>
        <ListItem>
          <Typography>
            <strong>Optional Information</strong>: First name, last name, email address, and ZIP code, if provided through the
            opt-in form.
          </Typography>
        </ListItem>
      </List>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        2. How We Use Your Information
      </Typography>
      <Typography paragraph>
        Your information is used solely for the following purposes:
      </Typography>
      <List>
        <ListItem>
          <Typography>
            To send you campaign updates, event notifications, voting reminders, and information related to direct lending or loan
            arrangements.
          </Typography>
        </ListItem>
        <ListItem>
          <Typography>
            To deliver age-gated content, restricted to users 18 and older, in compliance with CTIA guidelines.
          </Typography>
        </ListItem>
        <ListItem>
          <Typography>
            To manage your opt-in and opt-out preferences.
          </Typography>
        </ListItem>
      </List>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        3. Data Sharing
      </Typography>
      <Typography paragraph>
        No mobile data will be shared with third parties or affiliates for marketing or promotional purposes. We may share data with
        service providers (e.g., messaging platforms like Twilio) to facilitate campaign communications, but only under strict
        confidentiality agreements. We may also disclose data as required by law or to comply with legal processes.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        4. Data Security
      </Typography>
      <Typography paragraph>
        We use industry-standard security measures to protect your personal information from unauthorized access, use, or disclosure.
        However, no method of transmission over the internet is 100% secure, and we cannot guarantee absolute security.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        5. Your Rights and Choices
      </Typography>
      <Typography paragraph>
        You have the following rights:
      </Typography>
      <List>
        <ListItem>
          <Typography>
            <strong>Opt-Out</strong>: Reply STOP to any message to stop receiving communications.
          </Typography>
        </ListItem>
        <ListItem>
          <Typography>
            <strong>Access or Delete Data</strong>: Contact us at{' '}
            <Link href={`mailto:${contactEmail}`}>{contactEmail}</Link> to request access to or deletion of your
            personal information.
          </Typography>
        </ListItem>
        <ListItem>
          <Typography>
            <strong>Age Restriction</strong>: You must be 18 or older to opt-in to receive age-gated content.
          </Typography>
        </ListItem>
      </List>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        6. Message Content and Frequency
      </Typography>
      <Typography paragraph>
        Messages may include embedded links, phone numbers, direct lending offers, and age-gated content. You may receive up to 5
        messages per week, though frequency may vary. Standard message and data rates may apply.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        7. Compliance with Laws
      </Typography>
      <Typography paragraph>
        We comply with the TCPA, CTIA Messaging Principles, and other applicable laws. Our opt-in process ensures explicit consent,
        and our messages adhere to guidelines for sensitive content, including direct lending and age-gated material.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        8. Contact Us
      </Typography>
      <Typography paragraph>
        For questions about this Privacy Policy, contact us at{' '}
        <Link href={`mailto:${contactEmail}`}>{contactEmail}</Link>.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        9. Changes to This Privacy Policy
      </Typography>
      <Typography paragraph>
        We may update this Privacy Policy from time to time. Changes will be posted on this page with an updated "Last Updated"
        date.
      </Typography>

      <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
        <Typography>
          <Link component={RouterLink} to="/opt-in">Return to Opt-in Page</Link> |{' '}
          <Link component={RouterLink} to="/terms">View Terms of Service</Link>
        </Typography>
      </Box>
    </Container>
  );
};

export default PrivacyPolicy;