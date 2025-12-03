namespace MMGC.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string toPhoneNumber, string message);
}

