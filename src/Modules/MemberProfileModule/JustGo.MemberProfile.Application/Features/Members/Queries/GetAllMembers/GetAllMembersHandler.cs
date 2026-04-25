using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetAllMembers
{
    public class GetAllMembersHandler: IRequestHandler<GetAllMembersQuery,List<MemberSummary>>
    {
        private readonly LazyService<IReadRepository<MemberSummary>> _readRepository;

        public GetAllMembersHandler(LazyService<IReadRepository<MemberSummary>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<MemberSummary>> Handle(GetAllMembersQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT TOP(1000)
                            U.[Userid], 
                            U.[LoginId], 
                            U.[FirstName], 
                            U.[LastName], 
                            P.[Number] AS [Mobile],
                            U.[CreationDate], 
                            U.[LastLoginDate], 
                            U.[IsActive], 
                            U.[IsLocked],
                            U.[EmailAddress], 
                            U.[ProfilePicURL], 
                            U.[DOB], 
                            U.[Gender],
                            U.[Address1], 
                            U.[Address2], 
                            U.[Address3], 
                            U.[Town], 
                            U.[County],
                            U.[Country], 
                            U.[PostCode], 
                            U.[EmailVerified], 
                            U.[MemberId], 
                            U.[UserSyncId], 
                            U.[SuspensionLevel]
                        FROM [dbo].[User] U
                        OUTER APPLY (
                            SELECT TOP (1) [Number]
                            FROM [dbo].[UserPhoneNumber]
                            WHERE [UserId] = U.[Userid] AND [Type] = 'Mobile'
                            ORDER BY [Id] ASC
                        ) P";
            var queryParameters = new DynamicParameters();
            var members = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return members;
        }
    }
}
