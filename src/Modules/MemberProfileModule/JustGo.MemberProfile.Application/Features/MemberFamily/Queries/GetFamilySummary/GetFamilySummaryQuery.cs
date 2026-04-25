using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilySummary;

public class GetFamilySummaryQuery : IRequest<Family?>
{
    public Guid Id { get; set; }
    public GetFamilySummaryQuery(Guid id)
    {
        Id = id;
    }
}
