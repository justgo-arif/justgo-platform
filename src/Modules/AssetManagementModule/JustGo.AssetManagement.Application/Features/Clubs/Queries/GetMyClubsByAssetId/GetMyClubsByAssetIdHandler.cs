using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetMyClubsByAssetId
{
    public class GetMyClubsByAssetIdHandler : IRequestHandler<GetMyClubsByAssetIdQuery, List<ClubMemberDTO>>
    {

        private readonly LazyService<IReadRepository<ClubMemberDTO>> _readRepository;
        private readonly IMediator _mediator;

        public GetMyClubsByAssetIdHandler(LazyService<IReadRepository<ClubMemberDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<ClubMemberDTO>> Handle(GetMyClubsByAssetIdQuery request, CancellationToken cancellationToken)
        {
            //int assetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { request.AssetRegisterId.ToString() } }))[0];
            string SQL = @"declare @AssetId int = (select top 1 AssetId from AssetRegisters where RecordGuid = @AssetRegisterId )

                             select Distinct  D.SyncGuid as ClubGuid ,cd.ClubName,cd.Location as Image,cd.ClubId,cd.DocId,cd.ClubType
                             from ClubMemberroles CMR inner join [User] U on U.UserId = CMR.UserId
                             inner join Clubs_default CD on CD.DocId = CMR.ClubDocId
                             inner join Document D on D.DocId = CD.DocId
                             INNER JOIN AssetOwners AO ON  AO.OwnerId = CMR.UserId AND AO.AssetId = @AssetId
                            ";

            var queryParameters = new DynamicParameters();
            //queryParameters.Add("@MemberDocId", assetId, dbType: DbType.Int32);
            queryParameters.Add("@AssetRegisterId", request.AssetRegisterId, dbType: DbType.Guid);

            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
