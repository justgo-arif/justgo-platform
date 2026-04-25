using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;
using JustGo.Authentication.Helper.Paginations.Offset;
using auditLog = JustGo.Authentication.Infrastructure.Logging;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.AssetManagement.Application.DTOs.Common;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetAssetAudits
{
    public class GetAssetAuditsHandler : IRequestHandler<GetAssetAuditsQuery, PagedResult<AssetAuditItemDTO>>
    {

        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMediator _mediator;

        public GetAssetAuditsHandler(
            IReadRepositoryFactory readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PagedResult<AssetAuditItemDTO>> Handle(GetAssetAuditsQuery request, CancellationToken cancellationToken)
        {
            int module = auditLog.AuditScheme.AssetManagement.Value;
            int area = -1;
            int entityId = 0;

            if(request.EntityType == EntityType.Asset)
            {
                area = auditLog.AuditScheme.AssetManagement.General.Value;

                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { request.EntityId },
                    Entity = AssetTables.AssetRegisters
                }))[0];
            }
            else if (request.EntityType == EntityType.Lease)
            {
                area = auditLog.AuditScheme.AssetManagement.AssetLease.Value;
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { request.EntityId },
                    Entity = AssetTables.AssetLeases
                }))[0];
            }
            else if (request.EntityType == EntityType.License)
            {
                area = auditLog.AuditScheme.AssetManagement.AssetLicense.Value;
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { request.EntityId },
                    Entity = AssetTables.AssetLicenses
                }))[0];
            }
            else if (request.EntityType == EntityType.Credential)
            {
                area = auditLog.AuditScheme.AssetManagement.AssetCredential.Value;
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { request.EntityId },
                    Entity = AssetTables.AssetCredentials
                }))[0];
            }


            string SQL = @"SELECT 
                        u.UserId UserId,
                        cast(u.UserSyncId as nvarchar(255)) UserSyncId,
                        CONCAT(u.FirstName, ' ', u.LastName)  Fullname,
                        u.ProfilePicURL ProfilePicURL,
                        u.MemberDocId MemberDocId,
                        sed.[Name] EventName,
                        se.AuditDate EventDate
                    FROM SystemEvent se
                    INNER JOIN SystemEventData sed ON sed.Id = se.Id
                    INNER JOIN [User] u ON u.UserId = se.ActionUserId
                    WHERE se.Category = @module
                      AND se.SubCategory = @area
                      And se.AffectedEntityId = @entityId
                    Order by se.Id desc
                    OFFSET (@PageNumber - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY
                    ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@module", module, dbType: DbType.Int32);
            queryParameters.Add("@area", area, dbType: DbType.Int32);
            queryParameters.Add("@entityId", entityId, dbType: DbType.Int32);
            queryParameters.Add("@PageNumber", request.PageNumber, dbType: DbType.Int32);
            queryParameters.Add("@PageSize", request.PageSize, dbType: DbType.Int32);
            var result = (await _readRepository.GetLazyRepository<AssetAuditItemDTO>().Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();

            string countSql = @"SELECT 
                            count(*) TotalRowCount
                            FROM SystemEvent se
                            INNER JOIN SystemEventData sed ON sed.Id = se.Id
                            INNER JOIN [User] u ON u.UserId = se.ActionUserId
                            WHERE se.Category = @module
                              AND se.SubCategory = @area
                              And se.AffectedEntityId = @entityId
                            ";

            var countData = await _readRepository.GetLazyRepository<CountDTO>().Value.GetAsync(countSql, cancellationToken, queryParameters, null, "text");


            return new PagedResult<AssetAuditItemDTO>()
            {
                Items = result,
                TotalCount = countData.TotalRowCount,

            };
        }
    }
}
