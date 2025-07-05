import React from 'react';
import { Container, Typography, Box, Link, List, ListItem } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { campaignConfig } from '../config/customerConfig';

const TermsOfService: React.FC = () => {
  const campaignName = campaignConfig.campaignName || 'Campaign';
  const campaignTitle = campaignConfig.campaignTitle || 'Campaign';
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
        Terms of Service - {campaignName}
      </Typography>
      
      <Typography variant="body2" color="text.secondary" paragraph>
        <strong>Last Updated: {new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}</strong>
      </Typography>

      <Typography paragraph>
        These Terms of Service ("Terms") govern your participation in the {campaignName} campaign's text messaging and robocall
        program. By opting in, you agree to these Terms.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        1. Program Description
      </Typography>
      <Typography paragraph>
        The {campaignName} campaign provides updates, event notifications, voting reminders, and information related to direct
        lending or loan arrangements via text messages and robocalls. Messages may include embedded links, phone numbers, and age-gated
        content restricted to users 18 and older.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        2. Opt-in Process
      </Typography>
      <Typography paragraph>
        You can opt-in by submitting your phone number at{' '}
        <Link component={RouterLink} to="/opt-in">{optInUrl}</Link> and
        checking the box to consent to texts and robocalls, confirming you are 18 or older.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        3. Message Frequency and Costs
      </Typography>
      <Typography paragraph>
        You may receive up to 5 messages per week, though frequency may vary. Standard message and data rates may apply, as
        determined by your mobile carrier.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        4. Opting Out
      </Typography>
      <Typography paragraph>
        You can opt-out at any time by replying STOP to any message. After opting out, you will receive a confirmation message and no
        further campaign messages unless you opt-in again.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        5. User Responsibilities
      </Typography>
      <Typography paragraph>
        You agree to:
      </Typography>
      <List>
        <ListItem>
          <Typography>Provide accurate information when opting in.</Typography>
        </ListItem>
        <ListItem>
          <Typography>Be 18 or older to receive age-gated content.</Typography>
        </ListItem>
        <ListItem>
          <Typography>Comply with applicable laws and these Terms.</Typography>
        </ListItem>
      </List>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        6. Compliance with Laws
      </Typography>
      <Typography paragraph>
        We comply with the Telephone Consumer Protection Act (TCPA), CTIA Messaging Principles, and other applicable laws. Our
        messages adhere to guidelines for sensitive content, including direct lending and age-gated material.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        7. Limitation of Liability
      </Typography>
      <Typography paragraph>
        The {campaignName} campaign is not liable for any damages arising from your participation in this program, including
        issues related to message delivery or content.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        8. Contact Us
      </Typography>
      <Typography paragraph>
        For questions about these Terms, contact us at{' '}
        <Link href={`mailto:${contactEmail}`}>{contactEmail}</Link>.
      </Typography>

      <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3 }}>
        9. Changes to These Terms
      </Typography>
      <Typography paragraph>
        We may update these Terms from time to time. Changes will be posted on this page with an updated "Last Updated" date.
      </Typography>

      <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
        <Typography>
          <Link component={RouterLink} to="/opt-in">Return to Opt-in Page</Link> |{' '}
          <Link component={RouterLink} to="/privacy-policy">View Privacy Policy</Link>
        </Typography>
      </Box>
    </Container>
  );
};

export default TermsOfService;