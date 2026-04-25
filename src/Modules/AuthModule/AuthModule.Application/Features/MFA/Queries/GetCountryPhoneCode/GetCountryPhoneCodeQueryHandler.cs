using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.GetCountryPhoneCode;

public class GetCountryPhoneCodeQueryHandler : IRequestHandler<GetCountryPhoneCodeQuery, IList<CountryCodes>>
{
    private readonly LazyService<IReadRepository<CountryCodes>> _readRepository;

    public GetCountryPhoneCodeQueryHandler(LazyService<IReadRepository<CountryCodes>> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IList<CountryCodes>> Handle(GetCountryPhoneCodeQuery request, CancellationToken cancellationToken)
    {
        string sql = @"select *,(select Value from SystemSettings where ItemKey ='SYSTEM.SITEADDRESS')+'/Media/Images/flag/'+trim(CountryCode)+'.png' as URL
                        from countryphonecodes";

        return (await _readRepository.Value.GetListAsync(sql, null, null, "text")).ToList();
    }
}
