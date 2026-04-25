using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;

namespace MobileApps.Application.Features.SystemSetting.Commands.UpdateGlobalSettings
{
    class GlobalSettingCommandHandler : IRequestHandler<GlobalSettingCommand, bool>
    {
        private readonly LazyService<IWriteRepository<SystemSettings>> _writeRepository;
        private readonly IUtilityService _utilityService;
        public GlobalSettingCommandHandler(LazyService<IWriteRepository<SystemSettings>> writeRepository, IUtilityService utilityService)
        {
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }
        public async Task<bool> Handle(GlobalSettingCommand request, CancellationToken cancellationToken)
        {
            DatabaseSwitcher.UseCentralDatabase();

            string sql = @"IF EXISTS (SELECT 1 FROM GlobalSettings WHERE ItemKey = @ItemKey)
                            BEGIN
                                UPDATE GlobalSettings SET Value = @Values WHERE ItemKey = @ItemKey;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO GlobalSettings (ItemKey, Value,IsEncrypted) VALUES (@ItemKey, @Values,0);
                            END";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ItemKey", request.ItemKey);

            if (request.IsEncrypted)
                queryParameters.Add("@Values", _utilityService.EncryptData(request.Value));
            else
                queryParameters.Add("@Values", request.Value);

            var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

            return result > 0;

        }
    }
}
