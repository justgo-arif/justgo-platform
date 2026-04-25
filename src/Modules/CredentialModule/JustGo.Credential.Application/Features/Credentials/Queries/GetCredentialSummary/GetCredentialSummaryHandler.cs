using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Credential.Application.DTOs;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialSummary;

public class GetCredentialSummaryHandler : IRequestHandler<GetCredentialSummaryQuery, CredentialSummaryDto>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetCredentialSummaryHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<CredentialSummaryDto> Handle(GetCredentialSummaryQuery request, CancellationToken cancellationToken = default)
    {
        return await GetCredentialSummaryAsync(request, cancellationToken) ?? new CredentialSummaryDto();
    }

    private async Task<CredentialSummaryDto?> GetCredentialSummaryAsync(GetCredentialSummaryQuery request, CancellationToken cancellationToken)
    {
        var sql = """
            WITH UC AS (
                SELECT UC.* 
                FROM UserCredentials UC
                INNER JOIN [User] U ON U.UserId = UC.UserId
                WHERE U.UserSyncId = @UserSyncId
            ),
            Active_ST AS (
            	SELECT StateId, [Name] StateName
            	FROM [State]
            	WHERE Processid =16 AND [Name] = 'Active'
            ), 
            PendingApprovaL_ST AS (
            	SELECT StateId, [Name] StateName
            	FROM [State]
            	WHERE Processid =16 AND [Name] = 'Pending Approval'
            ),
            ActionRequired_ST AS (
            	SELECT StateId, [Name] StateName
            	FROM [State]
            	WHERE Processid =16 AND
            	[Name] IN (
            		'Awaiting Referral',
            		'Awaiting Response',
            		'Inactive Pending Conditions',
            		'Submitted Pending Review'
            	)
            ),
            Expired_ST AS (
            	SELECT StateId, [Name] StateName
            	FROM [State]
            	WHERE Processid =16 AND [Name] = 'Expired'
            )
            SELECT 
            (
            SELECT COUNT(1)
            FROM UC
            INNER JOIN Active_ST ST ON ST.StateId = UC.StatusId
            --WHERE UC.EndDate > DATEADD(DAY, 30, CAST(GETDATE() AS DATETIME))
            ) ActiveCount,
            (
            SELECT COUNT(1)
            FROM UC
            INNER JOIN Active_ST ST ON ST.StateId = UC.StatusId
            WHERE --UC.EndDate >= CAST(GETDATE() AS DATETIME) AND 
            UC.EndDate <= DATEADD(DAY, 30, CAST(GETDATE() AS DATETIME))
            ) ExpiringSoonCount,
            (
            SELECT COUNT(1) A
            FROM UC
            INNER JOIN PendingApprovaL_ST ST ON ST.StateId = UC.StatusId
            ) PendingApprovalCount,
            (
            SELECT COUNT(1) A
            FROM UC
            INNER JOIN ActionRequired_ST ST ON ST.StateId = UC.StatusId
            ) AttentionRequiredCount,
            (
            SELECT COUNT(1) A
            FROM UC
            INNER JOIN Expired_ST ST ON ST.StateId = UC.StatusId
            ) ExpiredCount
            """;

        return await _readRepository.GetLazyRepository<CredentialSummaryDto>().Value.GetAsync(sql, cancellationToken, new { UserSyncId = request.UserGuid }, null, "text");
    }
}
