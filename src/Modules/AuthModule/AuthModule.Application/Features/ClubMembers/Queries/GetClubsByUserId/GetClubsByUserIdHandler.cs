using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.ClubMembers.Queries.GetClubsByUserId
{
    public class GetClubsByUserIdHandler : IRequestHandler<GetClubsByUserIdQuery, List<Club>>
    {
        private readonly LazyService<IReadRepository<Club>> _readRepository;

        public GetClubsByUserIdHandler(LazyService<IReadRepository<Club>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<Club>> Handle(GetClubsByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select d.SyncGuid,cd.DocId,cd.ClubName from ClubMembers_Default CMD inner join 
                                ClubMembers_Links CML on CML.DocId=CMD.DocId
                                  inner join ClubMembers_Links CML2 on CML2.DocId = cmd.DocId
                                  inner join Clubs_Default Cd on cd.DocId = CML2.Entityid
								  inner join Document d on d.Docid = Cd.DocId and d.RepositoryId=2
                                  inner join [User] U on u.MemberDocId = cml.Entityid
                                WHERE u.Userid=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var clubs = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return clubs.AsList();
        }
    }
}
