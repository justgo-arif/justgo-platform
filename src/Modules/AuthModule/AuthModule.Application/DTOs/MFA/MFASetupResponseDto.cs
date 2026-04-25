namespace AuthModule.Application.DTOs.MFA;

public class MFASetupResponseDto
{
    public string SharedKey { get; set; }
    public string ManualEntryKey { get; set; }
    public string AuthenticatorUri { get; set; }
    public string To { get; set; }
    public int Attempt { get; set; }
    public string StatusMessage { get; set; }
    public bool IsSuccess { get; set; }
}
