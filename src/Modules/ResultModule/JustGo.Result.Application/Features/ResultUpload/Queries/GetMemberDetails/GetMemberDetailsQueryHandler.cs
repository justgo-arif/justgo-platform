using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDetails;

public class GetMemberDetailsQueryHandler : IRequestHandler<GetMemberDetailsQuery, Result<ICollection<FindMembersDto>>>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetMemberDetailsQueryHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    private const string SqlQuery = """
                                    DECLARE @BaseUrl NVARCHAR(500);
                                    DECLARE @search_term_no_spaces NVARCHAR(500);
                                                                            
                                    SELECT @BaseUrl = [Value]
                                    FROM SystemSettings
                                    WHERE ItemKey = 'SYSTEM.SITEADDRESS';
                                                                            
                                    SET @search_term_no_spaces = REPLACE(@search_term, ' ', '');
                                                                            
                                    
                                    SELECT TOP 50
                                        u.UserId,
                                        cast(u.UserSyncId as nvarchar(50)) as UserGuid,
                                        u.FirstName,
                                        u.LastName,
                                        u.MemberId,
                                        u.EmailAddress,
                                        u.Mobile,
                                        u.ProfilePicURL,
                                        CASE 
                                            WHEN u.MemberId = @search_term THEN 100
                                            WHEN u.EmailAddress = @search_term THEN 95
                                            WHEN REPLACE(u.Mobile,' ','') = @search_term_no_spaces THEN 90
                                            WHEN CONCAT(u.FirstName,' ',u.LastName) = @search_term THEN 85
                                            WHEN CONCAT(u.LastName,' ',u.FirstName) = @search_term THEN 80
                                            WHEN u.MemberId LIKE @search_term + '%' THEN 70
                                            WHEN u.FirstName LIKE @search_term + '%' THEN 65
                                            WHEN u.LastName LIKE @search_term + '%' THEN 60
                                            WHEN u.EmailAddress LIKE @search_term + '%' THEN 55
                                            WHEN u.MemberId LIKE '%'+@search_term+'%' THEN 50
                                            WHEN u.FirstName LIKE '%'+@search_term+'%' THEN 45
                                            WHEN u.LastName LIKE '%'+@search_term+'%' THEN 40
                                            WHEN u.EmailAddress LIKE '%'+@search_term+'%' THEN 35
                                            WHEN REPLACE(u.Mobile,' ','') LIKE '%'+@search_term_no_spaces+'%' THEN 30
                                            WHEN CONCAT(u.FirstName,' ',u.LastName) LIKE '%'+@search_term+'%' THEN 25
                                            WHEN CONCAT(u.LastName,' ',u.FirstName) LIKE '%'+@search_term+'%' THEN 20
                                            ELSE 0
                                        END AS SearchScore
                                    INTO #TopUsers000
                                    FROM [User] u
                                    WHERE @search_term IS NULL OR @search_term = '' OR
                                          u.MemberId LIKE '%'+@search_term+'%' OR
                                          u.FirstName LIKE '%'+@search_term+'%' OR
                                          u.LastName LIKE '%'+@search_term+'%' OR
                                          u.EmailAddress LIKE '%'+@search_term+'%' OR
                                          REPLACE(u.Mobile,' ','') LIKE '%'+@search_term_no_spaces+'%' OR
                                          CONCAT(u.FirstName,' ',u.LastName) LIKE '%'+@search_term+'%' OR
                                          CONCAT(u.LastName,' ',u.FirstName) LIKE '%'+@search_term+'%';
                                    
                                    SELECT
                                        tu.UserId,
                                        COUNT(DISTINCT pd.DocId) AS MembershipTypeCount,
                                        (SELECT TOP 1 pd2.Name
                                         FROM UserMemberships um2
                                         JOIN processinfo pr2 ON pr2.PrimaryDocId = um2.MemberLicenseDocId AND pr2.CurrentStateId = 62
                                         JOIN Products_Default pd2 ON pd2.DocId = um2.ProductId
                                         WHERE um2.UserId = tu.UserId
                                         ORDER BY um2.MemberLicenseDocId DESC) AS MembershipName
                                    INTO #MembershipData000
                                    FROM #TopUsers000 tu
                                    JOIN UserMemberships um ON um.UserId = tu.UserId
                                    JOIN processinfo pr ON pr.PrimaryDocId = um.MemberLicenseDocId AND pr.CurrentStateId = 62
                                    JOIN Products_Default pd ON pd.DocId = um.ProductId
                                    GROUP BY tu.UserId;
                                    
                                    
                                    SELECT 
                                        tu.UserId,
                                        tu.UserGuid,
                                        tu.FirstName,
                                        tu.LastName,
                                        tu.MemberId,
                                        tu.EmailAddress,
                                        tu.Mobile,
                                        CASE 
                                            WHEN tu.ProfilePicURL IS NULL OR tu.ProfilePicURL = '' THEN ''
                                            ELSE @BaseUrl + 'store/downloadPublic?f=' + tu.ProfilePicURL + '&t=user&p=1'
                                        END AS ProfilePicUrl,
                                        ISNULL(md.MembershipName,'') AS MembershipName,
                                        ISNULL(md.MembershipTypeCount,0) AS MembershipTypeCount,
                                        tu.SearchScore
                                    FROM #TopUsers000 tu
                                    LEFT JOIN #MembershipData000 md ON md.UserId = tu.UserId
                                    WHERE tu.SearchScore > 0 OR @search_term IS NULL OR @search_term = ''
                                    ORDER BY tu.SearchScore DESC, tu.FirstName, tu.LastName, tu.MemberId;
                                    
                                    DROP TABLE #MembershipData000;
                                    DROP TABLE #TopUsers000;
                                    
                                    
                                    """;

    public async Task<Result<ICollection<FindMembersDto>>> Handle(GetMemberDetailsQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepository.GetRepository<FindMembersDto>();
        var item = (await repo.GetListAsync(SqlQuery, cancellationToken, new
        {
            search_term = request.SearchTerm
        }, null, QueryType.Text)).ToList();
        return item;
    }
}