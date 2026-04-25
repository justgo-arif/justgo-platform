namespace AuthModule.Application.Features.MFA.Queries.VerifyCodeQueryParam;

public class VerifyCodeQueryParam
{
    public int UserId { get; set; }
    public string AuthChannel { get; set; }
    public string Method { get; set; }
    public string Code { get; set; }
    public Dictionary<string, string> Args { get; set; }
}
