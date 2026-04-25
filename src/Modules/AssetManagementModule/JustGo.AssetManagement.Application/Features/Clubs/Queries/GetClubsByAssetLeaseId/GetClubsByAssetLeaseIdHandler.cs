using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubsByAssetLeaseId
{
    public class GetClubsByAssetLeaseIdHandler : IRequestHandler<GetClubsByAssetLeaseIdQuery, List<ClubMemberDTO>>
    {

        private readonly LazyService<IReadRepository<ClubMemberDTO>> _readRepository;
        private readonly IMediator _mediator;

        public GetClubsByAssetLeaseIdHandler(LazyService<IReadRepository<ClubMemberDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<ClubMemberDTO>> Handle(GetClubsByAssetLeaseIdQuery request, CancellationToken cancellationToken)
        {

            string SQL = @"declare @AssetLeaseId int = (select top 1 AssetLeaseId from AssetLeases where RecordGuid = @LeaseId )
                            select Distinct  D.SyncGuid as ClubGuid ,cd.ClubName,cd.Location as Image,cd.ClubId,cd.DocId,cd.ClubType
                            from ClubMemberroles CMR inner join [User] U on U.UserId = CMR.UserId
                            inner join Clubs_default CD on CD.DocId = CMR.ClubDocId
                            inner join Document D on D.DocId = CD.DocId
                            INNER JOIN AssetOwnerships AO ON  AO.OwnerId = CMR.UserId AND 
                                  AO.EntityId = @AssetLeaseId and AO.EntityType = 2 AND
                                  AO.OwnerType = 2
                            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LeaseId", request.AssetLeaseId);

            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
