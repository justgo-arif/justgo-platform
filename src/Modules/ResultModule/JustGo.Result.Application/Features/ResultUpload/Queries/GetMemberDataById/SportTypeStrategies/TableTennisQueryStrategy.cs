using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById.SportTypeStrategies;

public class TableTennisQueryStrategy : IGetMemberDataQueryStrategy
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;

    public TableTennisQueryStrategy(IReadRepositoryFactory readRepositoryFactory)
    {
        _readRepositoryFactory = readRepositoryFactory;
    }

    public async Task<Result<object>> ExecuteAsync(GetMemberDataByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _readRepositoryFactory.GetRepository<object>();

            const string query = """
                                     DECLARE @BaseUrl NVARCHAR(500);
                                     SELECT @BaseUrl = ss.[Value] FROM SystemSettings ss WHERE ss.ItemKey = 'SYSTEM.SITEADDRESS';
                                                                                                                                                
                                     SELECT 
                                     winner.MemberId AS Winner, 
                                     CASE 
                                         WHEN winner.ProfilePicURL IS NULL OR winner.ProfilePicURL = '' 
                                             THEN NULL 
                                         ELSE @BaseUrl + 'store/downloadPublic?f=' + REPLACE(winner.ProfilePicURL, '\"', '\\\"') + '&t=user&p=1'
                                     END AS WinnerProfilePicURL,
                                     winner.Mobile AS WinnerMobile,
                                     winner.EmailAddress AS WinnerEmailAddress,
                                     JSON_VALUE(md.MemberData, '$."Winner Name"') AS WinnerName,
                                     mem.MembershipsName AS WinnerMemberships,
                                     mem.MembershipTypeCount AS WinnerMembershipCount,
                                     mem.Expires as WinnerExpires,
                                                                      
                                     loser.MemberId AS Loser,
                                     CASE 
                                         WHEN loser.ProfilePicURL IS NULL OR loser.ProfilePicURL = '' 
                                             THEN NULL 
                                         ELSE @BaseUrl + 'store/downloadPublic?f=' + REPLACE(loser.ProfilePicURL, '\"', '\\\"') + '&t=user&p=1'
                                     END AS LoserProfilePicURL,
                                     loser.Mobile AS LoserMobile,
                                     loser.EmailAddress AS LoserEmailAddress,
                                     JSON_VALUE(md.MemberData, '$."Loser Name"') AS LoserName,
                                     mem2.MembershipsName AS LoserMemberships,
                                     mem2.MembershipTypeCount AS LoserMembershipCount,
                                     mem2.Expires as LoserExpires,
                                     
                                     JSON_MODIFY(
                                         JSON_MODIFY(
                                             JSON_MODIFY(
                                                 JSON_MODIFY(md.MemberData, '$.Winner', NULL), 
                                                 '$.Loser', NULL
                                             ), 
                                             '$."Winner Name"', NULL
                                         ), 
                                         '$."Loser Name"', NULL
                                     ) AS ModifiedMemberData
                                     FROM ResultUploadedMemberData md
                                     LEFT JOIN [User] winner ON winner.MemberId = JSON_VALUE(md.MemberData, '$.Winner')
                                     LEFT JOIN [User] loser ON loser.MemberId = JSON_VALUE(md.MemberData, '$.Loser')
                                     OUTER APPLY (
                                         SELECT COUNT(pd.DocId) AS MembershipTypeCount,
                                                 STRING_AGG(pd.[Name], ',') AS MembershipsName,
                                             STRING_AGG(a.EndDate, ',') AS Expires 
                                         FROM [User] U
                                         LEFT JOIN [UserMemberships] a
                                            ON a.UserId = u.UserId
                                         LEFT JOIN Products_Default pd
                                            ON pd.DocId = a.ProductId
                                         LEFT JOIN Products_Links pl
                                            ON pl.DocId = pd.DocId
                                         LEFT JOIN License_Default ld
                                            ON ld.DocId = pl.EntityId
                                         LEFT JOIN [State] s
                                            ON s.StateId = a.StatusId AND s.StateId = 62
                                         WHERE u.MemberId = winner.MemberId
                                         GROUP BY u.UserId
                                     ) mem
                                     OUTER APPLY (
                                         SELECT COUNT(pd.DocId) AS MembershipTypeCount,
                                                 STRING_AGG(pd.[Name], ',') AS MembershipsName,
                                             STRING_AGG(a.EndDate, ',') AS Expires 
                                         FROM [User] U
                                         LEFT JOIN [UserMemberships] a
                                            ON a.UserId = u.UserId
                                         LEFT JOIN Products_Default pd
                                            ON pd.DocId = a.ProductId
                                         LEFT JOIN Products_Links pl
                                            ON pl.DocId = pd.DocId
                                         LEFT JOIN License_Default ld
                                            ON ld.DocId = pl.EntityId
                                         LEFT JOIN [State] s
                                            ON s.StateId = a.StatusId AND s.StateId = 62
                                         WHERE u.MemberId = loser.MemberId
                                         GROUP BY u.UserId
                                     ) mem2
                                 WHERE md.UploadedMemberDataId = @UploadedMemberId;
                                 """;
            
            var queryResult = await repository.GetListAsync<dynamic>(query,
                new { UploadedMemberId = request.MemberDataId },
                null, QueryType.Text, cancellationToken);

            var enumerable = queryResult.ToList();
            if (enumerable.Count == 0)
            {
                return new TableTennisResultDto();
            }
            
            var mappedResult = MapToTableTennisResultDto(enumerable.First());

            return mappedResult;
        }
        catch (Exception ex)
        {
            return Result<object>.Failure($"Error retrieving Table Tennis member data: {ex.Message}",
                ErrorType.InternalServerError);
        }
    }
    
    private static TableTennisResultDto MapToTableTennisResultDto(dynamic dynamicResult)
    {
	    var dict = (IDictionary<string, object>)dynamicResult;
	    
        return new TableTennisResultDto
        {
            WinnerDto = new WinnerPlayerDto
            {
                MemberId = GetPropertyValue<int>(dict, "Winner"),
                Name = GetPropertyValue<string>(dict, "WinnerName") ?? string.Empty,
                ProfilePicUrl = GetPropertyValue<string>(dict, "WinnerProfilePicURL"),
                Mobile = GetPropertyValue<string>(dict, "WinnerMobile"),
                EmailAddress = GetPropertyValue<string>(dict, "WinnerEmailAddress"),
                Memberships = GetPropertyValue<string>(dict, "WinnerMemberships"),
				MembershipCount = GetPropertyValue<int>(dict, "WinnerMembershipCount"),
                MembershipsExpiresOn = GetPropertyValue<string>(dict, "WinnerExpires")
            },
            LoserDto = new LoserPlayerDto
            {
                MemberId = GetPropertyValue<int>(dict, "Loser"),
                Name = GetPropertyValue<string>(dict, "LoserName") ?? string.Empty,
                ProfilePicUrl = GetPropertyValue<string>(dict, "LoserProfilePicURL"),
                Mobile = GetPropertyValue<string>(dict, "LoserMobile"),
                EmailAddress = GetPropertyValue<string>(dict, "LoserEmailAddress"),
                Memberships = GetPropertyValue<string>(dict, "LoserMemberships"),
                MembershipCount = GetPropertyValue<int>(dict, "LoserMembershipCount"),
                MembershipsExpiresOn = GetPropertyValue<string>(dict, "LoserExpires")
            },
            ModifiedMemberData = GetPropertyValue<string>(dict, "ModifiedMemberData") ?? string.Empty
        };
    }
    
    private static T? GetPropertyValue<T>(IDictionary<string, object> dict, string propertyName)
    {
        try
        {
            if (dict.TryGetValue(propertyName, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch
        {
            return default(T);
        }

        return default(T);
    }
}