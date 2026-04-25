using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.SearchMembersForFamily;

public class SearchMembersForFamilyHandler : IRequestHandler<SearchMembersForFamilyQuery, List<FindMemberDto>>
{
    private readonly IReadRepositoryFactory _readRepository;

    public SearchMembersForFamilyHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<List<FindMemberDto>> Handle(SearchMembersForFamilyQuery request, CancellationToken cancellationToken)
    {
        var decodedRequest = new SearchMembersForFamilyQuery
        {
            Email = Uri.UnescapeDataString(request.Email),
            MID = !string.IsNullOrWhiteSpace(request.MID) ? Uri.UnescapeDataString(request.MID) : request.MID,
            DateOfBirth = request.DateOfBirth
        };
        return await GetMembersAsync(decodedRequest, cancellationToken);
    }

    private async Task<List<FindMemberDto>> GetMembersAsync(SearchMembersForFamilyQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("Email", request.Email);

        string condition = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.MID))
        {
            condition = " AND U.MemberId = @MID ";
            queryParameters.Add("MID", request.MID);
        }
        else if (request.DateOfBirth.HasValue)
        {
            condition = " AND U.DOB = @DOB ";
            queryParameters.Add("DOB", request.DateOfBirth.Value);
        }

        var sql = $"""
                   DECLARE @MergedStateId int =
                   (
                   	SELECT TOP (1) StateId
                   	FROM dbo.[State]
                   	WHERE Name = 'Merged'
                   );

                   SELECT U.MemberDocId, U.MemberId MID, U.UserId, U.FirstName, U.LastName, U. MiddleName Surname, U.EmailAddress, U.Gender,
                   U.DOB, U.UserSyncId,
                   CASE 
                       WHEN U.ProfilePicURL IS NULL OR LTRIM(RTRIM(U.ProfilePicURL)) = '' 
                       THEN NULL
                       ELSE '/store/download?f=' + U.ProfilePicURL + '&t=user&p=' + CAST(U.Userid AS NVARCHAR(50))
                   END AS ProfilePicURL
                   FROM [User] U
                   INNER JOIN ProcessInfo p2 on p2.PrimaryDocId = U.MemberDocId AND p2.ProcessId = 1 AND p2.CurrentStateId <> @MergedStateId
                   WHERE U.EmailAddress = @Email
                   {condition}
                   ;
                   """;
        return (await _readRepository.GetLazyRepository<FindMemberDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).AsList();
    }
}
