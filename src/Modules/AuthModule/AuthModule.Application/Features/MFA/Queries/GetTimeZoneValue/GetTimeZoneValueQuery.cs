using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetTimeZoneValue;

public class GetTimeZoneValueQuery : IRequest<TimeZoneMFA>
{
}
