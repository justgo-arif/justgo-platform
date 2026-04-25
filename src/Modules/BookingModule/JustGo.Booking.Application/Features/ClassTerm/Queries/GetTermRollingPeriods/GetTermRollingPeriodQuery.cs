using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Domain.Entities;

namespace JustGo.Booking.Application.Features.ClassTerm.Queries.GetTermRollingPeriods
{
    public class GetTermRollingPeriodQuery : IRequest<List<TermRollingPeriod>>
    {
    }
}
