namespace AuthModule.Application.Features.MFA.Queries.VerifyCodeQueryParam;

public class VerifyCodeQueryParamByUserGuid
{
    public Guid UserGuid { get; set; }
    public string AuthChannel { get; set; }
    public string Method { get; set; } = null!;
    public string Code { get; set; }
    public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
}
