import React from 'react';
import {
  Box,
  Container,
  Typography,
  Card,
  CardContent,
  Button,
  Link,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Grid,
  useTheme,
  useMediaQuery
} from '@mui/material';
import {
  Language,
  VideoLibrary,
  AccountBalance,
  HowToVote,
  Phone,
  Help,
  ExpandMore,
  Launch,
  Campaign,
  Support,
  Info
} from '@mui/icons-material';

const VolunteerResources: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const resources = [
    {
      title: 'Campaign Information',
      icon: <Campaign color="primary" />,
      color: '#1976d2',
      items: [
        {
          label: 'Official Campaign Website',
          value: 'tanveer4hoover.com',
          url: 'https://tanveer4hoover.com',
          icon: <Language />,
          description: 'Learn about Tanveer\'s platform, policies, and vision for Hoover'
        },
        {
          label: 'Campaign YouTube Channel',
          value: '@tanveer4hoover',
          url: 'https://youtube.com/@tanveer4hoover',
          icon: <VideoLibrary />,
          description: 'Watch campaign videos, speeches, and community events'
        }
      ]
    },
    {
      title: 'Financial Support',
      icon: <AccountBalance color="success" />,
      color: '#2e7d32',
      items: [
        {
          label: 'Campaign Donations',
          value: '@Tanveer-Patel-Campaign',
          url: 'https://venmo.com/Tanveer-Patel-Campaign',
          icon: <AccountBalance />,
          description: 'Support the campaign with secure online donations via Venmo'
        }
      ]
    },
    {
      title: 'Voter Resources',
      icon: <HowToVote color="secondary" />,
      color: '#9c27b0',
      items: [
        {
          label: 'Alabama Voter Information',
          value: 'Check Registration & Polling Locations',
          url: 'https://myinfo.alabamavotes.gov/VoterView',
          icon: <HowToVote />,
          description: 'Official Alabama portal for voter registration, polling locations, and ballot information'
        }
      ]
    },
    {
      title: 'Support & Help',
      icon: <Support color="warning" />,
      color: '#ed6c02',
      items: [
        {
          label: 'Volunteer Hotline',
          value: '(205) 555-VOTE',
          url: 'tel:+12055558683',
          icon: <Phone />,
          description: 'Call for immediate assistance, questions, or reporting issues'
        }
      ]
    }
  ];

  const faqs = [
    {
      question: 'What should I do before starting canvassing?',
      answer: 'Always check in with campaign leadership, ensure you have your materials (voter lists, campaign literature), charge your phone, and familiarize yourself with the voter database system. Make sure to dress professionally and wear your campaign identification.'
    },
    {
      question: 'How do I log a voter contact in the system?',
      answer: 'After speaking with a voter, use the "Contact Voter" button next to their name. Select the appropriate contact status (Reached, Not Home, Refused, or Needs Follow-up), indicate their level of support if applicable, and add any relevant notes about your conversation.'
    },
    {
      question: 'What if a voter is not home?',
      answer: 'Mark them as "Not Home" in the system and consider leaving campaign literature if appropriate. You can return to visit them later, or they may be contacted through other outreach methods like phone calls.'
    },
    {
      question: 'How should I handle hostile or uninterested voters?',
      answer: 'Always remain polite and respectful. Thank them for their time and mark them as "Refused" in the system. Do not engage in arguments or debates. If you feel unsafe, leave the area immediately and contact the volunteer hotline.'
    },
    {
      question: 'What information should I collect from supportive voters?',
      answer: 'Record their level of support (Strong Yes, Leaning Yes, etc.), ask if they need assistance with voting information, and see if they would like to volunteer or help spread the word to neighbors and friends.'
    },
    {
      question: 'What if I encounter technical issues with the app?',
      answer: 'Try refreshing the page or restarting the app first. If problems persist, call the volunteer hotline at (205) 555-VOTE for immediate technical support. Always keep a backup paper list when possible.'
    },
    {
      question: 'How do I use the location features effectively?',
      answer: 'Allow the app to access your location to find the nearest uncontacted voters. Use the map integration to get directions to voter addresses. This helps optimize your route and maximize contacts per hour.'
    },
    {
      question: 'What should I bring when canvassing?',
      answer: 'Bring your phone with the canvassing app, campaign literature, a pen, comfortable walking shoes, water, and your campaign ID badge. Consider bringing business cards with campaign information for interested voters.'
    }
  ];

  return (
    <Container maxWidth="lg" sx={{ mt: 3, mb: 3 }}>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
          Volunteer Resources
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Everything you need to effectively canvass for Tanveer Patel's campaign
        </Typography>
      </Box>

      {/* Resource Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        {resources.map((section, index) => (
          <Grid item xs={12} md={6} key={index}>
            <Card 
              sx={{ 
                height: '100%',
                border: `2px solid ${section.color}`,
                '&:hover': {
                  boxShadow: theme.shadows[8],
                  transform: 'translateY(-2px)',
                  transition: 'all 0.2s ease-in-out'
                }
              }}
            >
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  {section.icon}
                  <Typography variant="h6" sx={{ ml: 1, fontWeight: 'bold' }}>
                    {section.title}
                  </Typography>
                </Box>
                
                {section.items.map((item, itemIndex) => (
                  <Box key={itemIndex} sx={{ mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                      {item.icon}
                      <Typography variant="subtitle2" sx={{ ml: 1, fontWeight: 'medium' }}>
                        {item.label}
                      </Typography>
                    </Box>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                      {item.description}
                    </Typography>
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<Launch />}
                      href={item.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      sx={{ 
                        borderColor: section.color,
                        color: section.color,
                        '&:hover': {
                          backgroundColor: `${section.color}10`,
                          borderColor: section.color
                        }
                      }}
                    >
                      {item.value}
                    </Button>
                  </Box>
                ))}
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      {/* Quick Links Section */}
      <Card sx={{ mb: 4, background: 'linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%)' }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
            <Info color="info" />
            <Typography variant="h6" sx={{ ml: 1, fontWeight: 'bold' }}>
              Quick Access
            </Typography>
          </Box>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <Chip
                icon={<Language />}
                label="Campaign Website"
                component="a"
                href="https://tanveer4hoover.com"
                target="_blank"
                clickable
                color="primary"
                sx={{ width: '100%', justifyContent: 'flex-start' }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Chip
                icon={<HowToVote />}
                label="Voter Registration"
                component="a"
                href="https://myinfo.alabamavotes.gov/VoterView"
                target="_blank"
                clickable
                color="secondary"
                sx={{ width: '100%', justifyContent: 'flex-start' }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Chip
                icon={<Phone />}
                label="Call Hotline"
                component="a"
                href="tel:+12055558683"
                clickable
                color="warning"
                sx={{ width: '100%', justifyContent: 'flex-start' }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Chip
                icon={<AccountBalance />}
                label="Donate"
                component="a"
                href="https://venmo.com/Tanveer-Patel-Campaign"
                target="_blank"
                clickable
                color="success"
                sx={{ width: '100%', justifyContent: 'flex-start' }}
              />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* FAQ Section */}
      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
            <Help color="info" />
            <Typography variant="h5" sx={{ ml: 1, fontWeight: 'bold' }}>
              Frequently Asked Questions
            </Typography>
          </Box>
          
          {faqs.map((faq, index) => (
            <Accordion key={index} sx={{ mb: 1 }}>
              <AccordionSummary
                expandIcon={<ExpandMore />}
                aria-controls={`faq-${index}-content`}
                id={`faq-${index}-header`}
              >
                <Typography variant="subtitle1" sx={{ fontWeight: 'medium' }}>
                  {faq.question}
                </Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Typography variant="body2" color="text.secondary">
                  {faq.answer}
                </Typography>
              </AccordionDetails>
            </Accordion>
          ))}
        </CardContent>
      </Card>

      {/* Contact Information Footer */}
      <Box sx={{ mt: 4, p: 3, backgroundColor: 'grey.100', borderRadius: 2 }}>
        <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold' }}>
          Need Additional Help?
        </Typography>
        <Typography variant="body2" color="text.secondary" paragraph>
          If you can't find what you're looking for here, don't hesitate to reach out:
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: isMobile ? 'column' : 'row', gap: 2 }}>
          <Button
            variant="contained"
            startIcon={<Phone />}
            href="tel:+12055558683"
            sx={{ flex: isMobile ? undefined : 1 }}
          >
            Call Volunteer Hotline
          </Button>
          <Button
            variant="outlined"
            startIcon={<Language />}
            href="https://tanveer4hoover.com"
            target="_blank"
            sx={{ flex: isMobile ? undefined : 1 }}
          >
            Visit Campaign Website
          </Button>
        </Box>
      </Box>
    </Container>
  );
};

export default VolunteerResources;