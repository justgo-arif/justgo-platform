using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetAssetStatuses;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets;
using JustGo.Authentication.Infrastructure.Exceptions;
using Serilog;
using JustGo.Authentication.Infrastructure.Logging;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;



namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands
{
    public class AssetReinstateCommandHandler : IRequestHandler<AssetReinstateCommand, bool>
    {

        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        public AssetReinstateCommandHandler(
            IMediator mediator,
            IWriteRepositoryFactory writeRepository,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService)
        {
            _mediator = mediator;
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(AssetReinstateCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            return await ChangeStatus(command, currentUserId, cancellationToken);
        }

        private async Task<bool> PermissionCheck(string AssetRegisterId)
        {

            var IsInAssetList = await _mediator.Send(new GetAssetsQuery()
            {
                PageNumber = 1,
                PageSize = 1,
                SearchItems = new List<SearchSegmentDTO>() {
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "AssetRecordGuid",
                       FieldId = "",
                       Operator = "equals",
                       Value = AssetRegisterId,
                       ConditionJoiner = "and"

                   },
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "LeaseIn",
                       FieldId = "",
                       Operator = "not equals",
                       Value = "1",
                       ConditionJoiner = ""

                   }
                }
            });

            if (IsInAssetList.TotalCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public async Task<bool> ChangeStatus(AssetReinstateCommand command, int currentUserId, CancellationToken cancellationToken)
        {

            var permissionCheckResult = await PermissionCheck(command.AssetRegisterId);
            if (!permissionCheckResult)
            {
                throw new ForbiddenAccessException("Invalid Attempt!");
            }

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", command.AssetRegisterId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                   select AssetTypeId from AssetRegisters where RecordGuid = @RecordGuid
               )", cancellationToken, queryParameters, null, "text");

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);


            queryParameters.Add("@currentUserId", currentUserId);


            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
                        .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 
                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = @currentUserId", cancellationToken, queryParameters, null, "text");

            if (!userGroups.Any(r => typeConfig.Permission.Update.Contains(r.Value)))
            {
                return false;
            }

            
            return await _mediator.Send(new AssetStateAllocationCommand()
            {
                AssetRegisterId = command.AssetRegisterId
            });



        }

    }
}
