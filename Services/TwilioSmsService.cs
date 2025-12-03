using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace MMGC.Services;

public class TwilioSmsService : ISmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhoneNumber;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
    {
        // Try environment variables first, then fall back to configuration
        _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") 
            ?? configuration["Twilio:AccountSid"] 
            ?? throw new InvalidOperationException("Twilio AccountSid not configured. Set TWILIO_ACCOUNT_SID environment variable or add to appsettings.json");
        
        _authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") 
            ?? configuration["Twilio:AuthToken"] 
            ?? throw new InvalidOperationException("Twilio AuthToken not configured. Set TWILIO_AUTH_TOKEN environment variable or add to appsettings.json");
        
        _fromPhoneNumber = Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE_NUMBER") 
            ?? configuration["Twilio:FromPhoneNumber"] 
            ?? throw new InvalidOperationException("Twilio FromPhoneNumber not configured. Set TWILIO_FROM_PHONE_NUMBER environment variable or add to appsettings.json");
        
        _logger = logger;

        // Log configuration (without exposing full credentials)
        _logger.LogInformation("Twilio SMS Service initialized. AccountSid: {AccountSidPrefix}..., FromNumber: {FromNumber}", 
            _accountSid.Substring(0, Math.Min(8, _accountSid.Length)), _fromPhoneNumber);

        // Initialize Twilio client
        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
    {
        try
        {
            // Ensure Twilio client is initialized (reinitialize to be safe)
            TwilioClient.Init(_accountSid, _authToken);

            // Format phone number to E.164 format if needed
            var formattedPhoneNumber = FormatPhoneNumber(toPhoneNumber);
            
            _logger.LogDebug("Sending SMS. From: {From}, To: {To}, MessageLength: {Length}, MessagePreview: {Preview}", 
                _fromPhoneNumber, formattedPhoneNumber, message?.Length ?? 0, 
                message?.Length > 50 ? message.Substring(0, 50) + "..." : message);

            // Match the working example exactly - use synchronous Create
            var messageOptions = new CreateMessageOptions(
                new PhoneNumber(formattedPhoneNumber));
            messageOptions.From = new PhoneNumber(_fromPhoneNumber);
            messageOptions.Body = message;

            // Use synchronous Create (like the working example)
            var messageResource = await Task.Run(() => MessageResource.Create(messageOptions));
            
            // Log detailed information
            _logger.LogInformation("SMS Response. SID: {MessageSid}, To: {To}, Status: {Status}, ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}, Price: {Price}, PriceUnit: {PriceUnit}, Uri: {Uri}", 
                messageResource.Sid, formattedPhoneNumber, messageResource.Status, 
                messageResource.ErrorCode, messageResource.ErrorMessage, messageResource.Price, messageResource.PriceUnit, messageResource.Uri);
            
            // Log the actual message body being sent (first 100 chars)
            _logger.LogDebug("Message body sent: {MessageBody}", 
                message?.Length > 100 ? message.Substring(0, 100) + "..." : message);

            // Check for errors
            if (messageResource.ErrorCode.HasValue && messageResource.ErrorCode.Value != 0)
            {
                _logger.LogWarning("SMS has error code: {ErrorCode}, Message: {ErrorMessage}", 
                    messageResource.ErrorCode, messageResource.ErrorMessage);
                
                // Common error codes:
                // 21211 - Invalid 'To' Phone Number
                // 21610 - Unsubscribed recipient
                // 21614 - 'To' number is not a valid mobile number
                // 30008 - Unknown destination handset
                return false;
            }

            // Status can be: queued, sending, sent, failed, delivered, undelivered
            // For trial accounts, messages might be queued but not delivered to unverified numbers
            if (messageResource.Status == MessageResource.StatusEnum.Queued || 
                messageResource.Status == MessageResource.StatusEnum.Sending ||
                messageResource.Status == MessageResource.StatusEnum.Sent ||
                messageResource.Status == MessageResource.StatusEnum.Delivered)
            {
                _logger.LogInformation("SMS accepted by Twilio. Status: {Status}. Note: If using a trial account, SMS will only be delivered to verified numbers.", 
                    messageResource.Status);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}: {ErrorMessage}", toPhoneNumber, ex.Message);
            return false;
        }
    }

    private string FormatPhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // If the number doesn't start with +, add country code
        if (!phoneNumber.StartsWith("+"))
        {
            // If it's a Pakistani number (starts with 0 or 92)
            if (digitsOnly.StartsWith("92"))
            {
                return "+" + digitsOnly;
            }
            else if (digitsOnly.StartsWith("0"))
            {
                // Replace leading 0 with +92
                return "+92" + digitsOnly.Substring(1);
            }
            else if (digitsOnly.Length == 10)
            {
                // Assume it's a Pakistani number without country code
                return "+92" + digitsOnly;
            }
            else
            {
                // Default: add + if not present
                return "+" + digitsOnly;
            }
        }

        return phoneNumber;
    }
}

