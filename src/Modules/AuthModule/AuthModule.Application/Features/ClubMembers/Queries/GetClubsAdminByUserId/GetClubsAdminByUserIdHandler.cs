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

namespace AuthModule.Application.Features.ClubMembers.Queries.GetClubsAdminByUserId
{
    public class GetClubsAdminByUserIdHandler : IRequestHandler<GetClubsAdminByUserIdQuery, List<Club>>
    {
        private readonly LazyService<IReadRepository<Club>> _readRepository;

        public GetClubsAdminByUserIdHandler(LazyService<IReadRepository<Club>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<Club>> Handle(GetClubsAdminByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT d.SyncGuid,cdd.DocId,cdd.ClubName
                                FROM clubmembers_default cmd 
                                  inner join clubmembers_links cml on cml.docid = cmd.docid
                                  inner join Clubs_Default cdd on cdd.Docid = cml.entityId
								  inner join Document d on d.Docid = cdd.DocId and d.RepositoryId=2
                                  inner join members_links mlfc on mlfc.EntityId = cmd.docid
                                  inner join Members_Default md on md.DocId = mlfc.DocId
                                  inner join EntityLink et on et.LinkId = md.DocId
                                  inner join [User] u on u.Userid = et.SourceId
                                  inner join lookup_22 l22 on l22.field_101 = 'Yes'
                                WHERE  cmd.myroles like '%'+l22.field_100+'%'
                                  and u.Userid = @UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var clubs = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return clubs.AsList();
        }
    }
}
