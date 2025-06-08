# Setting Up Custom Domain for T4H Canvas

## Option 1: Using Your Own Domain

### Prerequisites
- Own a domain (e.g., t4h.com, tanveer4hoover.com)
- Access to domain DNS settings

### Steps:

#### 1. Add Domain to DigitalOcean App
1. Go to https://cloud.digitalocean.com/apps/4210df4e-200d-4397-82d5-c3157127f965
2. Click **"Settings"** tab
3. Click **"Domains"** section
4. Click **"Add Domain"**
5. Enter your domain (e.g., `canvas.tanveer4hoover.com` or `t4h.com`)
6. Choose **"You manage your domain"**

#### 2. Configure DNS Records
Add these DNS records in your domain registrar:

**For subdomain (e.g., canvas.tanveer4hoover.com):**
```
Type: CNAME
Name: canvas
Value: t4h-canvas-2uwxt.ondigitalocean.app
TTL: 300
```

**For root domain (e.g., t4h.com):**
```
Type: A
Name: @
Value: [IP provided by DigitalOcean]
TTL: 300

Type: CNAME  
Name: www
Value: t4h-canvas-2uwxt.ondigitalocean.app
TTL: 300
```

#### 3. Verify Domain
- DigitalOcean will automatically verify DNS configuration
- SSL certificate will be generated automatically
- Usually takes 5-15 minutes

## Option 2: DigitalOcean Subdomain

If you don't want to use a custom domain, you can request a shorter subdomain from DigitalOcean support, though this is not always available.

## Option 3: URL Shortener Service

Use a service like Bitly, TinyURL, or your own URL shortener to create:
- `https://bit.ly/t4h-canvas` 
- `https://tinyurl.com/t4h-canvas`

## Recommended Domains to Register

If you need to register a new domain, consider:
- `t4h.org` - Political/campaign appropriate
- `tanveer4hoover.com` - Full campaign name
- `canvas4hoover.com` - Descriptive
- `t4hcanvas.com` - App specific

## After Custom Domain Setup

Update the following files with your new domain:

1. **frontend/src/config.ts**
2. **backend/HooverCanvassingApi/Program.cs** (CORS)
3. **backend/HooverCanvassingApi/appsettings.Production.json**
4. **deployment/.do-app-spec.yaml**
5. **frontend/public/index.html** (meta tags)

## Benefits of Custom Domain

- ✅ Professional appearance
- ✅ Easier to remember and share
- ✅ Better for campaign materials
- ✅ Custom SSL certificate
- ✅ Better SEO
- ✅ Campaign branding consistency