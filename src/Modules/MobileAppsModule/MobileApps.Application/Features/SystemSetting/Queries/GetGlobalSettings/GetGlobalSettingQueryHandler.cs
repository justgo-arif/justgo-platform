using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using MobileApps.Domain.Entities;

namespace MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings
{
    class GetGlobalSettingQueryHandler : IRequestHandler<GetGlobalSettingQuery, string>
    {
        private readonly LazyService<IReadRepository<GlobalSettings>> _readRepository;
        private readonly IUtilityService _utilityService;
        public GetGlobalSettingQueryHandler(LazyService<IReadRepository<GlobalSettings>> readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }
        public async Task<string> Handle(GetGlobalSettingQuery request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();

            string sql = @"select gs.ItemKey,gs.Value,ISNULL(IsEncrypted,0) IsEncrypted  from GlobalSettings gs  where gs.ItemKey=@ItemKey";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ItemKey", request.ItemKey);

            var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
            if (result == null) return "";

            if (!string.IsNullOrEmpty(result.Value) && result.IsEncrypted)
            {
                result.Value = _utilityService.DecryptData(result.Value);
            }
            return result.Value;

        }
    }
}
