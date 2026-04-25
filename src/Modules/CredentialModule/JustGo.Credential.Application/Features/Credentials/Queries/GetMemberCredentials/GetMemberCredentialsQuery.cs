using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Credential.Application.DTOs;
using JustGo.MemberProfile.Application.DTOs;
using System.ComponentModel;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetMemberCredentials;

public class GetMemberCredentialsQuery : IRequest<OperationResultDto<List<CredentialsDto>>>
{
    public Guid UserGuid { get; set; }
    public string? SummaryStatus { get; set; }
    public int[]? Statuses { get; set; }
    public string? Category { get; set; }
    public int? Level { get; set; }
    public decimal? Point { get; set; }
    [DefaultValue(false)]
    public bool HistoryMode { get; set; } = false;
}
