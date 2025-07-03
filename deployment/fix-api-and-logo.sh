#!/bin/bash

# Fix API URL and logo for cindymyrex app
APP_ID="5103bfe7-8bef-43d9-b9c5-dd29e5c41808"

echo "Fixing API URL and logo configuration..."

doctl apps update ${APP_ID} --spec - <<'EOF'
name: canvassing-cindymyrex
region: nyc
services:
- name: api
  github:
    repo: maqboolp/CanvassingApp
    branch: main
    deploy_on_push: true
  dockerfile_path: deployment/Dockerfile
  http_port: 8080
  instance_count: 1
  instance_size_slug: basic-xxs
  health_check:
    http_path: /api/health
    initial_delay_seconds: 30
    period_seconds: 30
  routes:
  - path: /api
  envs:
  - key: ASPNETCORE_URLS
    value: "http://+:8080"
  - key: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - key: ConnectionStrings__DefaultConnection
    value: "${db.DATABASE_URL}"
  - key: JwtSettings__Secret
    value: "4OlmIkGMuiYjxJGK+Cc5dvwHxCqlu1MvQOiGY9UdKho="
    type: SECRET
  - key: JwtSettings__Issuer
    value: "https://canvassing-cindymyrex-yjopq.ondigitalocean.app"
  - key: JwtSettings__Audience
    value: "https://canvassing-cindymyrex-yjopq.ondigitalocean.app"
  - key: JwtSettings__ExpirationMinutes
    value: "480"
  - key: SENDGRID_API_KEY
    value: "YOUR_SENDGRID_API_KEY_HERE"
    type: SECRET
  - key: Twilio__AccountSid
    value: "YOUR_TWILIO_ACCOUNT_SID_HERE"
    type: SECRET
  - key: Twilio__AuthToken
    value: "YOUR_TWILIO_AUTH_TOKEN_HERE"
    type: SECRET
  - key: Twilio__FromPhoneNumber
    value: "+1XXXXXXXXXX"
  - key: CORS__AllowedOrigins__0
    value: "https://canvassing-cindymyrex-yjopq.ondigitalocean.app"
  - key: CORS__AllowedOrigins__1
    value: "http://localhost:3000"
  - key: FileStorage__Provider
    value: "S3"
  - key: FileStorage__S3__AccessKey
    value: "YOUR_DO_SPACES_KEY_HERE"
    type: SECRET
  - key: FileStorage__S3__SecretKey
    value: "YOUR_DO_SPACES_SECRET_HERE"
    type: SECRET
  - key: FileStorage__S3__ServiceUrl
    value: "https://nyc3.digitaloceanspaces.com"
  - key: FileStorage__S3__BucketName
    value: "hoover-canvassing-audio"

- name: frontend
  github:
    repo: maqboolp/CanvassingApp
    branch: main
    deploy_on_push: true
  source_dir: frontend
  build_command: npm ci && npm run build
  environment_slug: node-js
  http_port: 3000
  instance_count: 1
  instance_size_slug: basic-xxs
  routes:
  - path: /
  run_command: npx serve -s build -l 3000
  envs:
  - key: NODE_ENV
    value: "production"
  - key: REACT_APP_API_URL
    value: "https://canvassing-cindymyrex-yjopq.ondigitalocean.app/api"
  - key: REACT_APP_LOGO_URL
    value: "/cindymyrex-logo.png"
  - key: REACT_APP_LOGO_ALT
    value: "Cindy Myrex for Alabama House District 12"
  - key: REACT_APP_TITLE
    value: "Cindy Myrex Campaign Canvassing"

databases:
- name: db
  engine: PG
  version: "15"
  size: db-s-1vcpu-1gb
  num_nodes: 1
EOF

echo "Configuration updated!"
echo ""
echo "Note: You need to upload the logo to DigitalOcean Spaces first:"
echo "1. Save the Cindy Myrex logo as cindymyrex-logo.png"
echo "2. Upload it with:"
echo "   doctl spaces upload cindymyrex-logo.png s3://hoover-canvassing-audio/logos/cindymyrex-logo.png --acl public-read"
echo ""
echo "Monitor deployment:"
echo "doctl apps logs ${APP_ID} --follow"