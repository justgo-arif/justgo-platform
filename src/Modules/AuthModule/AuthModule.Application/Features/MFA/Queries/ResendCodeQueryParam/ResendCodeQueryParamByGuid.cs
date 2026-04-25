namespace AuthModule.Application.Features.MFA.Queries.ResendCodeQueryParam;

public class ResendCodeQueryParamByGuid
{
    public Guid UserGuid { get; set; }
    public string AuthChannel { get; set; }
    public Dictionary<string, string> Args { get; set; } = new()
    {
        { "issuer", "" },
        { "email", "" },
        { "username", "" }
    };
}
