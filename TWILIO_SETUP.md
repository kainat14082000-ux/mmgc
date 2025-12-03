# Twilio SMS Setup Guide

This application uses Twilio for sending SMS notifications for appointments.

## Setup Instructions

### Option 1: Using appsettings.Development.json (Recommended for Local Development)

1. Copy `appsettings.example.json` to `appsettings.Development.json`:
   ```bash
   cp appsettings.example.json appsettings.Development.json
   ```

2. Edit `appsettings.Development.json` and add your Twilio credentials:
   ```json
   {
     "Twilio": {
       "AccountSid": "YOUR_TWILIO_ACCOUNT_SID",
       "AuthToken": "YOUR_TWILIO_AUTH_TOKEN",
       "FromPhoneNumber": "YOUR_TWILIO_PHONE_NUMBER"
     }
   }
   ```

**Note:** `appsettings.Development.json` is in `.gitignore` and will not be committed to the repository.

### Option 2: Using Environment Variables (Recommended for Production)

Set the following environment variables:

- `TWILIO_ACCOUNT_SID` - Your Twilio Account SID
- `TWILIO_AUTH_TOKEN` - Your Twilio Auth Token
- `TWILIO_FROM_PHONE_NUMBER` - Your Twilio phone number (e.g., +17575406051)

#### For Docker:
Add to your `docker-compose.yml`:
```yaml
environment:
  - TWILIO_ACCOUNT_SID=your_account_sid
  - TWILIO_AUTH_TOKEN=your_auth_token
  - TWILIO_FROM_PHONE_NUMBER=+17575406051
```

#### For Local Development (Windows PowerShell):
```powershell
$env:TWILIO_ACCOUNT_SID="your_account_sid"
$env:TWILIO_AUTH_TOKEN="your_auth_token"
$env:TWILIO_FROM_PHONE_NUMBER="+17575406051"
```

#### For Local Development (Linux/Mac):
```bash
export TWILIO_ACCOUNT_SID="your_account_sid"
export TWILIO_AUTH_TOKEN="your_auth_token"
export TWILIO_FROM_PHONE_NUMBER="+17575406051"
```

## Priority Order

The application checks for credentials in this order:
1. Environment variables (highest priority)
2. `appsettings.Development.json` (for development)
3. `appsettings.json` (fallback, should be empty)

## Getting Your Twilio Credentials

1. Sign up for a Twilio account at https://www.twilio.com/
2. Go to the Twilio Console: https://console.twilio.com/
3. Find your Account SID and Auth Token on the dashboard
4. Get a phone number from Twilio (or use your existing one)

## Important Notes

- **Trial Accounts**: If you're using a Twilio trial account, you can only send SMS to verified phone numbers. Verify your numbers in the Twilio Console under "Phone Numbers" → "Verified Caller IDs".

- **Security**: Never commit your Twilio credentials to the repository. They are automatically ignored by `.gitignore`.

- **Production**: For production deployments, always use environment variables or a secure secrets management system (Azure Key Vault, AWS Secrets Manager, etc.).

