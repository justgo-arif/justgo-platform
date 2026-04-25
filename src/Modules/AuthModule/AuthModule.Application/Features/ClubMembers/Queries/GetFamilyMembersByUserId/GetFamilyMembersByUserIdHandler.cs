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

namespace AuthModule.Application.Features.ClubMembers.Queries.GetFamilyMembersByUserId
{
    public class GetFamilyMembersByUserIdHandler:IRequestHandler<GetFamilyMembersByUserIdQuery,List<User>>
    {
        private readonly LazyService<IReadRepository<User>> _readRepository;

        public GetFamilyMembersByUserIdHandler(LazyService<IReadRepository<User>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<User>> Handle(GetFamilyMembersByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select u.MemberDocId,ISNULL(u.UserSyncId, '00000000-0000-0000-0000-000000000000') as UserSyncId,u.FirstName,u.LastName from [User] u
	                                inner join Family_Links fl on u.MemberDocId=fl.Entityid
	                                inner join Family_Default fd on fl.DocId=fd.DocId
                                where fl.DocId in (select DocId from Family_Links flm inner join [User] us 
                                on flm.Entityid=us.MemberDocId where us.Userid=@UserId)";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var users = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return users.AsList();
        }
    }
}
