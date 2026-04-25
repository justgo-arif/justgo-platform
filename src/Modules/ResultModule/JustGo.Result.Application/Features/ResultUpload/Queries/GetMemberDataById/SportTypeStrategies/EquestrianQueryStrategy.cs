using System.Data;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById.SportTypeStrategies;

public class EquestrianQueryStrategy : IGetMemberDataQueryStrategy
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;

    public EquestrianQueryStrategy(IReadRepositoryFactory readRepositoryFactory)
    {
        _readRepositoryFactory = readRepositoryFactory;
    }

    public async Task<Result<object>> ExecuteAsync(GetMemberDataByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        // const string query = """
        //                      DECLARE @BaseUrl NVARCHAR(500);
        //                      SELECT @BaseUrl = ss.[Value] FROM SystemSettings ss WHERE ss.ItemKey = 'SYSTEM.SITEADDRESS';
        //                                                                                                                                                                  
        //                      SELECT 
        //                      		u.MemberId AS MemberId, 
        //                       CASE 
        //                           WHEN u.ProfilePicURL IS NULL OR u.ProfilePicURL = '' 
        //                               THEN NULL 
        //                           ELSE @BaseUrl + 'store/downloadPublic?f=' + REPLACE(u.ProfilePicURL, '\"', '\\\"') + '&t=user&p=1'
        //                       END AS MemberProfilePicURL,
        //                       u.Mobile AS Mobile,
        //                       u.EmailAddress AS EmailAddress,
        //                       CONCAT_WS(' ', u.FirstName, u.LastName) as FullName,
        //                       mem.MembershipsName AS Memberships,
        //                       mem.MembershipTypeCount AS MembershipCount,
        //                       mem.Expires as Expires,
        //                       md.MemberData         
        //                      FROM ResultUploadedMemberData md
        //                      INNER JOIN ResultUploadedMember m on md.UploadedMemberId = m.UploadedMemberId
        //                      LEFT JOIN [User] u ON m.MemberId = u.MemberId
        //                      OUTER APPLY (
        //                      	SELECT
        //                      		COUNT(DISTINCT pd.DocId) AS MembershipTypeCount,
        //                      		STRING_AGG(pd.[Name], ',') AS MembershipsName,
        //                      	STRING_AGG(UM.EndDate, ',') AS Expires 
        //                      	FROM [User] tu
        //                      	JOIN UserMemberships um ON um.UserId = tu.UserId
        //                      	JOIN processInfo pr ON pr.PrimaryDocId = um.MemberLicenseDocId AND pr.CurrentStateId = 62
        //                      	JOIN Products_Default pd ON pd.DocId = um.ProductId
        //                      	WHERE tu.MemberId = u.MemberId
        //                      	GROUP BY tu.UserId
        //                      ) mem
        //                      WHERE md.UploadedMemberDataId = @UploadedMemberId;
        //                      """;
        
        const string query = """
                             DECLARE @BaseUrl NVARCHAR(500);
                             SELECT @BaseUrl = ss.[Value] FROM SystemSettings ss WHERE ss.ItemKey = 'SYSTEM.SITEADDRESS';
                             
                             SELECT 
                                 u.MemberId AS MemberId, 
                             	CASE 
                             		WHEN u.ProfilePicURL IS NULL OR u.ProfilePicURL = '' 
                             			THEN NULL 
                             		ELSE @BaseUrl + 'store/downloadPublic?f=' + REPLACE(u.ProfilePicURL, '\"', '\\\"') + '&t=user&p=1'
                             	END AS MemberProfilePicURL,
                             	u.Mobile AS Mobile,
                             	u.EmailAddress AS EmailAddress,
                             	CONCAT_WS(' ', u.FirstName, u.LastName) as FullName,
                             	md.MemberData         
                             FROM ResultUploadedMemberData md
                             INNER JOIN ResultUploadedMember m on md.UploadedMemberId = m.UploadedMemberId
                             LEFT JOIN [User] u ON m.MemberId = u.MemberId
                             WHERE md.UploadedMemberDataId = @UploadedMemberId;
                             """;

        var repository = _readRepositoryFactory.GetRepository<MemberDataDto>();

        var memberData = await repository.GetAsync(query, cancellationToken,
            new { UploadedMemberId = request.MemberDataId },
            null, QueryType.Text);

        return memberData ?? Result<object>.Failure("Member data not found.", ErrorType.BadRequest);
    }
}