using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubDetails
{
    public class GetClubDetailsHandler : IRequestHandler<GetClubDetailsQuery, ClubMemberDTO>
    {

        private readonly LazyService<IReadRepository<ClubMemberDTO>> _readRepository;

        public GetClubDetailsHandler(LazyService<IReadRepository<ClubMemberDTO>> readRepository)
        {
            _readRepository = readRepository;

        }

        public async Task<ClubMemberDTO> Handle(GetClubDetailsQuery request, CancellationToken cancellationToken)
        {
     
            string SQL = @"declare @ClubId int = (select top 1 Docid from Document where SyncGuid = @SyncGuid )

                             select @SyncGuid as ClubGuid ,cd.ClubName,cd.Location as Image,cd.ClubId,cd.DocId,cd.ClubType
                             from Clubs_default CD
                             where CD.DocId = @ClubId
                            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@SyncGuid", request.ClubId, dbType: DbType.String);

            return await _readRepository.Value.GetAsync(SQL, cancellationToken, queryParameters, null, "text");
        }
    }
}
