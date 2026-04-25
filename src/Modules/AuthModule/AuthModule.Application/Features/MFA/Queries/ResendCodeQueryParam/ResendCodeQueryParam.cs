namespace AuthModule.Application.Features.MFA.Queries.ResendCodeQueryParam;

public class ResendCodeQueryParam
{
    public int UserId { get; set; }
    public string AuthChannel { get; set; }
    public Dictionary<string, string> Args { get; set; }
}
