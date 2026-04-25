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

namespace AuthModule.Application.Features.ClubMembers.Queries.GetAdminClubsWithChildByUserId
{
    public class GetAdminClubsWithChildByUserIdHandler : IRequestHandler<GetAdminClubsWithChildByUserIdQuery, List<Club>>
    {
        private readonly LazyService<IReadRepository<Club>> _readRepository;

        public GetAdminClubsWithChildByUserIdHandler(LazyService<IReadRepository<Club>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<Club>> Handle(GetAdminClubsWithChildByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = "[dbo].[GetAdminClubsWithChildByUserId]";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            var clubs = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters);
            return clubs.AsList();
        }
    }
}
