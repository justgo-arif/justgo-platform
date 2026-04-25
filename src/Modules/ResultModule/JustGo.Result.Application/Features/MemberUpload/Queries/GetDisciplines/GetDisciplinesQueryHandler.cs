using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Domain.Entities;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetDisciplines
{
    public class GetDisciplinesQueryHandler : IRequestHandler<GetDisciplinesQuery, List<ResultDiscipline>>
    {
        private readonly LazyService<IReadRepository<ResultDiscipline>> _readRepository;
        private readonly IUtilityService _utilityService;



        public GetDisciplinesQueryHandler(LazyService<IReadRepository<ResultDiscipline>> readRepository,
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<List<ResultDiscipline>> Handle(GetDisciplinesQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ScopeType <= 0)
            {
                throw new ArgumentException("Validation Scope Type Missing.");
            }

            var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
            string ownerSqlCondition = string.Empty;

            var parameters = new DynamicParameters();
            if (ownerId >= 0)
            {
                parameters.Add("@OwnerId", ownerId);
                ownerSqlCondition = " AND VS.OwnerId = @OwnerId ";
            }

            var SqlQuery = $@"
                              select RET.RecordGuid AS ResultEventTypeId,VS.ValidationScopeId as DisciplineId, VS.ValidationScopeName as Name, VS.Status
                              from ValidationScopes VS
                              INNER JOIN RESULTEVENTTYPE RET ON VS.ResultEventTypeId = RET.ResultEventTypeId
                              where VS.ValidationScopeType = @ScopeType AND VS.SportTypeId = @SportTypeId AND VS.OwnerId = -1
                              and isnull(VS.Status,0) = 1

                              UNION ALL

                            select RET.RecordGuid AS ResultEventTypeId,VS.ValidationScopeId as DisciplineId, VS.ValidationScopeName as Name, VS.Status
                            from ValidationScopes VS
                            INNER JOIN RESULTEVENTTYPE RET ON VS.ResultEventTypeId = RET.ResultEventTypeId
                              where VS.ValidationScopeType = @ScopeType AND VS.SportTypeId = @SportTypeId {ownerSqlCondition}
                              and isnull(VS.Status,0) = 1
                              order by name asc
                                        ";

            return (await _readRepository.Value.GetListAsync(SqlQuery, cancellationToken: cancellationToken,
                new { ScopeType = request.ScopeType, SportTypeId = request.SportTypeId, OwnerId = ownerId }, null,
                commandType: QueryType.Text)).ToList();
        }
    }
}