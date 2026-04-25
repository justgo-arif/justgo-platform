using AuthModule.Application.DTOs.MFA;
using AuthModule.Application.Interfaces.Persistence.Repositories.MFARepository;
using AuthModule.Domain.Entities.MFA;
using Google.Authenticator;
using System.Text;

namespace AuthModule.Infrastructure.Persistence.Repositories.MFA;

public class MfaRepositoryService : IMfaRepository
{
    //private readonly string _apiPath;
    //private readonly string _apiKey;
    //IMediator _mediator;
    //private readonly ISystemSettingsService _systemSettingsService;
    //public MfaRepositoryService(IMediator mediator, ISystemSettingsService systemSettingsService)
    //{
    //    //_mediator = mediator;
    //    //_systemSettingsService = systemSettingsService;
    //    //var mfaConfigTask = Task.Run(async () =>
    //    //{
    //    //    var mfaConfig = await _systemSettingsService.GetSystemSettings(
    //    //        "SYSTEM.MFA.APICONFIG", default, "MFA", 0);

    //    //    if (!string.IsNullOrEmpty(mfaConfig))
    //    //    {
    //    //        var data = JsonConvert.DeserializeObject<MFAConfig>(mfaConfig);
    //    //        return (data.MFAApiKey, data.MFAApiURL);
    //    //    }

    //    //    return (string.Empty, string.Empty);
    //    //});

    //    //(_apiKey, _apiPath) = mfaConfigTask.Result;

    //}

    public string GenerateBackupCode()
    {
        return Guid.NewGuid().ToString().Replace("-", "").ToUpper();
    }

    //public async Task<MFASetupResponseDto> SetupAuthenticator(string authChannel, IDictionary<string, string> args)
    //{
    //    var url = _apiPath + "/api/v1/MfA/SetUp?channel=" + authChannel;

    //    try
    //    {
    //        var serializedData = JsonConvert.SerializeObject(args);

    //        using (var httpClient = new HttpClient())
    //        {
    //            httpClient.DefaultRequestHeaders.Add("XApiKey", _apiKey);

    //            var response = await httpClient.PostAsync(url, new StringContent(serializedData, null, "application/json"));
    //            var data = await response.Content.ReadAsStringAsync();
    //            return JsonConvert.DeserializeObject<MFASetupResponseDto>(data.ToString());
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        //logger.Error(ex);
    //        return new MFASetupResponseDto() { IsSuccess = false };
    //    }
    //}

    public async Task<MFASetupResponseDto> SetupAuthenticator(string authChannel, IDictionary<string, string> args)
    {
        try
        {
            string issuer = args["issuer"];
            string userName = args["username"];
            string email = args["email"];

            string authKey = GenerateKey(24);
            TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
            var info = TwoFacAuth.GenerateSetupCode(issuer, userName, ConvertSecretToBytes(authKey, false), 5);

            return new MFASetupResponseDto
            {
                AuthenticatorUri = info.QrCodeSetupImageUrl,
                SharedKey = authKey,
                ManualEntryKey = info.ManualEntryKey,
                IsSuccess = true,
                StatusMessage = "Success"
            };
        }
        catch (Exception ex)
        {
            return new MFASetupResponseDto() { IsSuccess = false, StatusMessage = ex.Message };
        }
    }

    public static string GenerateKey(int length)
    {
        return Guid.NewGuid().ToString().Replace("-", "").ToUpper();
    }

    private static byte[] ConvertSecretToBytes(string secret, bool secretIsBase32) => secretIsBase32 ? Base32Encoding.ToBytes(secret) : Encoding.UTF8.GetBytes(secret);

    //public async Task<MFASetupResponseDto> GetAuthenticatorKey(string authChannel, IDictionary<string, string> args)
    //{
    //    var url = _apiPath + "/api/v1/MfA/GetAuthenticatorKey?channel=" + authChannel;

    //    try
    //    {
    //        var serializedData = JsonConvert.SerializeObject(args);

    //        using (var httpClient = new HttpClient())
    //        {
    //            httpClient.DefaultRequestHeaders.Add("XApiKey", _apiKey);
    //            var response = await httpClient.PostAsync(url, new StringContent(serializedData, null, "application/json"));
    //            var data = await response.Content.ReadAsStringAsync();
    //            return JsonConvert.DeserializeObject<MFASetupResponseDto>(data.ToString());
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return new MFASetupResponseDto() { IsSuccess = false };
    //    }
    //}

    public async Task<MFASetupResponseDto> GetAuthenticatorKey(string authChannel, IDictionary<string, string> args)
    {
        try
        {
            string issuer = args["issuer"];
            string authKey = args["authKey"];
            string userName = args["username"];

            TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
            var info = TwoFacAuth.GenerateSetupCode(issuer, userName, ConvertSecretToBytes(authKey, true), 5);

            return new MFASetupResponseDto
            {
                AuthenticatorUri = info.QrCodeSetupImageUrl,
                SharedKey = authKey,
                StatusMessage = "Success",
                IsSuccess = true,
            };
        }
        catch (Exception ex)
        {
            return new MFASetupResponseDto() { IsSuccess = false, StatusMessage = ex.Message };
        }
    }

    //public async Task<MFAVerifyDto> VerifyCode(string authChannel, IDictionary<string, string> args, UserMFA userMFA)
    //{
    //    var url = _apiPath + "/api/v1/MfA/VerifyCode?channel=" + authChannel;

    //    switch (authChannel.ToLower())
    //    {
    //        case "whatsapp":
    //            if (!args.ContainsKey("whatsAppNumber"))
    //            {
    //                args.Add("whatsAppNumber", userMFA.WhatsAppNumber);
    //                args.Add("phoneCode", userMFA.PhoneCode);
    //            }
    //            break;
    //        case "authapp":
    //            args.Add("authKey", userMFA.AuthenticatorKey);
    //            break;
    //        default:
    //            break;
    //    }
    //    try
    //    {
    //        var serializedData = JsonConvert.SerializeObject(args);

    //        using (var httpClient = new HttpClient())
    //        {
    //            httpClient.DefaultRequestHeaders.Add("XApiKey", _apiKey);


    //            var response = await httpClient.PostAsync(url, new StringContent(serializedData, null, "application/json"));
    //            var data = await response.Content.ReadAsStringAsync();
    //            return JsonConvert.DeserializeObject<MFAVerifyDto>(data.ToString());
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        return new MFAVerifyDto { IsValid = false };
    //    }
    //}

    public async Task<MFAVerifyDto> VerifyCode(string authChannel, IDictionary<string, string> args, UserMFA userMFA)
    {
        try
        {
            string authKey = userMFA.AuthenticatorKey;
            string code = args["code"];

            TwoFactorAuthenticator TwoFacAuth = new TwoFactorAuthenticator();
            bool isValid = TwoFacAuth.ValidateTwoFactorPIN(authKey, code, TimeSpan.FromSeconds(30));
            return new MFAVerifyDto { IsValid = isValid };
        }
        catch (Exception ex)
        {
            return new MFAVerifyDto { IsValid = false, Message = ex.Message };
        }
    }
}
