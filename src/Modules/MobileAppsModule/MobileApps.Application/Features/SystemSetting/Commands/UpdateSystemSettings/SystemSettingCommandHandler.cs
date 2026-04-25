using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;

namespace MobileApps.Application.Features.SystemSetting.Commands.UpdateSystemSettings
{
    class SystemSettingCommandHandler : IRequestHandler<SystemSettingCommand, bool>
    {
        private readonly LazyService<IWriteRepository<SystemSettings>> _writeRepository;
        private readonly IUtilityService _utilityService;
        public SystemSettingCommandHandler(LazyService<IWriteRepository<SystemSettings>> writeRepository, IUtilityService utilityService)
        {
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }
        public async Task<bool> Handle(SystemSettingCommand request, CancellationToken cancellationToken)
        {

            try
            {
                string sql = @"update GlobalSettings set Value=@Values WHERE ItemKey=@ItemKey";

                var queryParameters = new DynamicParameters();
                queryParameters.Add("@ItemKey", request.ItemKey);

                if (request.Restricted)
                    queryParameters.Add("@Values", _utilityService.EncryptData(request.Value));
                else
                    queryParameters.Add("@Values", request.Value);



                var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

                return result > 0;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
