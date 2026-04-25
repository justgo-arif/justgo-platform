using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetMyClubs
{
    public class GetMyClubsHandler : IRequestHandler<GetMyClubsQuery, List<ClubMemberDTO>>
    {

        private readonly LazyService<IReadRepository<ClubMemberDTO>> _readRepository;
        private readonly IMediator _mediator;

        public GetMyClubsHandler(LazyService<IReadRepository<ClubMemberDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<ClubMemberDTO>> Handle(GetMyClubsQuery request, CancellationToken cancellationToken)
        {

            int memberDocId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { request.MemberId.ToString() } }))[0];
            string SQL = @"select club.SyncGuid as ClubGuid ,cd.ClubName,cd.Location as Image,cd.ClubId,cd.DocId,cd.ClubType from ClubMembers_Default CMD inner join ClubMembers_Links CML ON CMD.DocId = CML.DocId
                            inner join [User] U on U.MemberDocId = CML.Entityid
                            inner join ClubMembers_Links cml2 on cml2.DocId = CMD.DocId
                            inner join Clubs_Default cd on cd.DocId = cml2.Entityid
                            inner join Document  club on club.DocId = cd.DocId
                            where u.MemberDocId = @MemberDocId
                            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@MemberDocId", memberDocId, dbType: DbType.Int32);

            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
