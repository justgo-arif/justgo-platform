using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetCountryPhoneCode;

public class GetCountryPhoneCodeQuery : IRequest<IList<CountryCodes>>
{
}
