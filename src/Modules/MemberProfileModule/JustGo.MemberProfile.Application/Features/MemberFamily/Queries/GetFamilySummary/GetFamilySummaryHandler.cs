using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilySummary;

public class GetFamilySummaryHandler : IRequestHandler<GetFamilySummaryQuery, Family?>
{
    private readonly IReadRepositoryFactory _readRepository;
    public GetFamilySummaryHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Family?> Handle(GetFamilySummaryQuery request, CancellationToken cancellationToken)
    {
        return await GetFamilySummaryAsync(request, cancellationToken);
    }

    private async Task<Family?> GetFamilySummaryAsync(GetFamilySummaryQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserSyncId", request.Id);

        var sql = $"""
               DECLARE @FamilyId INT = (
               SELECT TOP 1 UF.FamilyId FROM UserFamilies UF
               INNER JOIN Families F ON F.FamilyId = UF.FamilyId
               WHERE UF.UserId = (SELECT TOP 1 UserId FROM [User] WHERE UserSyncId = @UserSyncId)
               ORDER BY F.FamilyName ASC
               );

               SELECT TOP 1 F.Reference, F.FamilyName, F.RecordGuid
               FROM Families F 
               WHERE F.FamilyId = @FamilyId;

               SELECT DISTINCT UF.RecordGuid, UF.UserFamilyId, UF.IsAdmin,
               U.FirstName, U.LastName, U.MemberId, U.EmailAddress, U.UserSyncId, 
               CASE 
                   WHEN U.ProfilePicURL IS NULL OR LTRIM(RTRIM(U.ProfilePicURL)) = '' 
                   THEN NULL
                   ELSE '/store/download?f=' + U.ProfilePicURL + '&t=user&p=' + CAST(U.Userid AS NVARCHAR(50))
               END AS ProfilePicURL,
               CASE WHEN ISNULL(UF.[Status],0)=0 THEN 1 ELSE 0 END IsPendingApproval
               FROM UserFamilies UF
               INNER JOIN [User] U ON U.Userid = UF.UserId
               WHERE UF.FamilyId = @FamilyId AND UF.[Status] IN (0,1);
               """;

        await using var result = await _readRepository.GetLazyRepository<object>().Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");

        if (result is null)
        {
            return null;
        }

        var family = await result.ReadSingleOrDefaultAsync<Family>();
        if (family is null)
        {
            return null;
        }

        var members = await result.ReadAsync<FamilyMember>();
        family.Members = members?.AsList() ?? [];

        return family;
    }

}
