using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.FindMember;

public class FindMemberHandler : IRequestHandler<FindMemberQuery, FindMemberDto?>
{
    private readonly IReadRepositoryFactory _readRepository;

    public FindMemberHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<FindMemberDto?> Handle(FindMemberQuery request, CancellationToken cancellationToken)
    {
        var decodedRequest = new FindMemberQuery
        {
            MID = Uri.UnescapeDataString(request.MID),
            Email = !string.IsNullOrWhiteSpace(request.Email) ? Uri.UnescapeDataString(request.Email) : request.Email,
            LastName = !string.IsNullOrWhiteSpace(request.LastName) ? Uri.UnescapeDataString(request.LastName) : request.LastName
        };
        return await GetMemberAsync(decodedRequest, cancellationToken);
        //return await GetMemberAsync(request, cancellationToken);
    }

    private async Task<FindMemberDto?> GetMemberAsync(FindMemberQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("MID", request.MID);

        string condition = string.Empty;
        if(!string.IsNullOrWhiteSpace(request.Email))
        {
            condition = " AND U.EmailAddress = @Email ";
            queryParameters.Add("Email", request.Email);
        }
        else if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            condition = " AND U.LastName = @LastName ";
            queryParameters.Add("LastName", request.LastName);
        }

        var sql = $"""
                   SELECT TOP 1 U.MemberDocId DocId, U.MemberId MID, U.UserId, U.FirstName, U.LastName, U. MiddleName Surname, U.EmailAddress, U.Gender,
                   U.ProfilePicURL, U.DOB, U.UserSyncId MemberSyncGuid
                   FROM [User] U
                   WHERE U.MemberId = @MID 
                   {condition}
                   ;
                   """;

        return await _readRepository.GetLazyRepository<FindMemberDto>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
    }

}