namespace AuthModule.Domain.Entities.MFA;

public class UserMFA
{
    public string AuthenticatorKey { get; set; }
    public bool EnableAuthenticatorApp { get; set; }
    public DateTime? EnableAuthenticatorAppDate { get; set; }
    public string AppStartDateTimezoneName { get; set; }
    public bool EnableWhatsapp { get; set; }
    public DateTime? EnableWhatsappDate { get; set; }
    public string WhatsAppDateTimezoneName { get; set; }
    public string BackUpCode { get; set; }
    public int whatsAppState { get; set; }
    public int AuthenticatorAppState { get; set; }
    public string WhatsAppNumber { get; set; }
    public string PhoneCode { get; set; }
    public string CountryCode { get; set; }
    public bool IsEmailAuthEnabled { get; set; }
    public DateTime? EmailAuthEnableDate { get; set; }
    public string Email { get; set; }
    public int EmailAuthState { get; set; }
    public string EmailAuthEnableDateTimezoneName { get; set; }
    public bool IsVisibleEmailMFA { get; set; } = true;
    public bool IsVisibleWhatsappMFA { get; set; } = true;
    public bool IsVisibleAuthenticatorMFA { get; set; } = true;
}
