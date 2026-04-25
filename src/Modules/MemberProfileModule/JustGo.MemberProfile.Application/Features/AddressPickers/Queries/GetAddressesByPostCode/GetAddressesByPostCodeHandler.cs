using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.AddressPickers.Queries.GetAddressesByPostCode;

public class GetAddressesByPostCodeHandler : IRequestHandler<GetAddressesByPostCodeQuery, List<AddressDto>>
{
    private readonly LazyService<IReadRepository<AddressDto>> _readRepository;
    private readonly ISystemSettingsService _systemSettings;

    public GetAddressesByPostCodeHandler(LazyService<IReadRepository<AddressDto>> readRepository, ISystemSettingsService systemSettings)
    {
        _readRepository = readRepository;
        _systemSettings = systemSettings;
    }

    public async Task<List<AddressDto>> Handle(GetAddressesByPostCodeQuery request, CancellationToken cancellationToken)
    {
        string procedureName = request.Mode.ToLower() == "postcodefinder"
            ? "GetAddressesByPostCode"
            : "GetAddressesByKeyword";

        var parameters = new DynamicParameters();

        if (request.Mode.ToLower() == "addressfinder")
        {
            parameters.Add("@pageSize", 10);
            parameters.Add("@pageNumber", 1);
            parameters.Add("@CountryName", request.CountryName);
            parameters.Add("@SearchToken", request.Keyword);
        }
        else if (request.Mode.ToLower() == "postcodefinder")
        {
            string? systemHostId = await _systemSettings.GetSystemSettingsByItemKey("CLUBPLUS.HOSTSYSTEMID", cancellationToken);
            parameters.Add("@CountryName", request.CountryName);
            parameters.Add("@SearchToken", request.Keyword.Replace(" ", ""));
            parameters.Add("@HostId", systemHostId);
        }

        DatabaseSwitcher.UseAddressPickerCoreDatabase();
        var result = (await _readRepository.Value.GetListAsync(procedureName, cancellationToken, parameters, null)).ToList();
        return result;
    }

}
