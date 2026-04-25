using Dapper;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetMemberLicenseById
{
    public class GetMemberLicensesByIdHandler : IRequestHandler<GetMemberLicensesByIdQuery, List<MemberLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<MemberLicenseDTO>> _readRepository;
        private IMediator _mediator;
        private readonly IUtilityService _utilityService;
        public GetMemberLicensesByIdHandler(LazyService<IReadRepository<MemberLicenseDTO>> readRepository, IMediator mediator, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
        }
        public async Task<List<MemberLicenseDTO>> Handle(GetMemberLicensesByIdQuery request, CancellationToken cancellationToken = default)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            string dataSQL = $@";WITH cte_memberclub AS
                            (
                                SELECT h.entityid
                                FROM hierarchylinks hl
                                INNER JOIN hierarchies h ON h.Id = hl.HierarchyId
                                INNER JOIN hierarchytypes ht ON ht.id = h.hierarchytypeid
                                WHERE hl.UserId = @UserId)  
                            ,
                            cte_memberLicense AS
                            (
                                SELECT ISNULL(UserMemberships.LicenceOwner,0) as OwnerId
                                FROM UserMemberships
                                INNER JOIN ProcessInfo PR ON PR.primaryDocId = UserMemberships.MemberLicenseDocId  
	                            INNER JOIN cte_memberclub CM on CM.EntityId = ISNULL(UserMemberships.LicenceOwner,0)
	                            where UserMemberships.UserId = @UserId AND PR.CurrentStateId = 62
                            )

							select OwnerId from cte_memberLicense";
            var queryParameters = new DynamicParameters();

            queryParameters.Add("@UserId", currentUserId, dbType: DbType.Int32);
            var result = (await _readRepository.Value.GetListAsync(dataSQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
