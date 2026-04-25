using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Domain.Entities;
using MapsterMapper;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyMembers;

public class GetFamilyMembersHandler : IRequestHandler<GetFamilyMembersQuery, List<FamilyMember>>
{
    private readonly IReadRepositoryFactory _readRepository;
    public GetFamilyMembersHandler(IReadRepositoryFactory readRepository,
        IHybridCacheService cache,
        IMapper mapper,
        IUtilityService utilityService)
    {
        _readRepository = readRepository;
    }

    public async Task<List<FamilyMember>> Handle(GetFamilyMembersQuery request, CancellationToken cancellationToken = default)
    {
        return await GetFamilyMembersAsync(request, cancellationToken);
    }

    private async Task<List<FamilyMember>> GetFamilyMembersAsync(GetFamilyMembersQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserSyncId", request.Id);

        var sql = $"""
               DECLARE @FamilyId INT = (
               SELECT TOP 1 UF.FamilyId FROM UserFamilies UF
               INNER JOIN Families F ON F.FamilyId = UF.FamilyId
               WHERE UF.UserId = (SELECT TOP 1 UserId FROM [User] WHERE UserSyncId = @UserSyncId)
               AND UF.[Status] IN (0,1)
               ORDER BY F.FamilyName ASC
               );

               WITH MEMBERS AS (
                   SELECT UF.UserFamilyId, UF.UserId   
                   FROM UserFamilies UF
                   WHERE UF.FamilyId = @FamilyId
               )  

               --VERIFIED_MEMBER AS (
               --  --SELECT U.UserId
               --  --FROM MEMBERS M
               --  --INNER JOIN [User] U ON U.UserId = M.UserId
               --  --INNER JOIN ( 
               --  --      SELECT H.ActionTokenId, H.[Value] MemberDocId FROM ActionTokenHandlerArguments H 
               --  --      WHERE H.[Name] = 'TargetMemberDocId' AND ISNUMERIC(H.[Value]) = 1
               --  --      ) ARG ON ARG.MemberDocId = CAST(U.UserId AS VARCHAR(50))
               --  --INNER JOIN ActionToken A ON A.Id = ARG.ActionTokenId AND A.HandlerType = 'FamilyManager' AND A.[Status] = 3 --Executed
               --  --INNER JOIN ActionTokenLog L ON L.ActionTokenId = A.Id

               --  SELECT DISTINCT M.UserId
               --  FROM MEMBERS M
               --  INNER JOIN [User] U ON U.UserId = M.UserId
               --  INNER JOIN ActionTokenHandlerArguments ARG ON ARG.[Name] = 'TargetMemberDocId' AND ISNUMERIC(ARG.[Value]) = 1 AND ARG.[Value] = CAST(U.MemberDocId AS VARCHAR(50))
               --  INNER JOIN ActionToken A ON A.Id = ARG.ActionTokenId AND A.HandlerType = 'FamilyManager' AND A.[Status] = 3 --Executed
               --  --INNER JOIN ActionTokenLog L ON L.ActionTokenId = A.Id
               --)

               SELECT DISTINCT UF.RecordGuid,
               UF.UserFamilyId,UF.FamilyId, UF.IsAdmin,
               U.FirstName, U.LastName, U.MemberId, U.EmailAddress, U.UserSyncId, U.UserId, U.MemberDocId,
               CASE 
                   WHEN U.ProfilePicURL IS NULL
                   THEN NULL
                   ELSE '/store/download?f=' + U.ProfilePicURL + '&t=user&p=' + CAST(U.Userid AS NVARCHAR(50))
               END AS ProfilePicURL,
               --CASE WHEN V.Userid IS NULL THEN 1 ELSE 0 END IsPendingApproval
               CASE WHEN ISNULL(UF.[Status],0)=0 THEN 1 ELSE 0 END IsPendingApproval
               FROM UserFamilies UF
               INNER JOIN MEMBERS M ON M.UserFamilyId = UF.UserFamilyId
               INNER JOIN [User] U ON U.Userid = UF.UserId
               --LEFT JOIN VERIFIED_MEMBER V ON V.Userid = U.Userid;
               WHERE UF.[Status] IN (0,1)

               SELECT DISTINCT UF.UserId [Id], PD.[Name] [Text]
               FROM UserFamilies UF
               INNER JOIN UserMemberships UM ON UM.UserId = UF.UserId --AND UM.StatusID IN (62, 64)
               INNER JOIN Products_Default PD ON PD.DocId = UM.ProductId
               WHERE UF.FamilyId = @FamilyId AND UF.[Status] IN (0,1);
               """;

        await using var result = await _readRepository.GetLazyRepository<object>().Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");

        var members = await result.ReadAsync<FamilyMember>();
        var membershipData = await result.ReadAsync<SelectModelDto>();
        foreach (var member in members)
        {
            member.Memberships = membershipData
                .Where(m => m.Id == member.UserId)
                .Select(m => m.Text)
                .ToArray();
        }

        return members.AsList();
    }

}
