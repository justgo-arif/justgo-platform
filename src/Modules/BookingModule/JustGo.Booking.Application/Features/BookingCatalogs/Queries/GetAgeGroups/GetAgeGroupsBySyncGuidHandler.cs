using Dapper;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration;
using System.Data;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetAgeGroups;

public class GetAgeGroupsBySyncGuidHandler : IRequestHandler<GetAgeGroupsBySyncGuidQuery, IEnumerable<AgeGroupCategoryDto>?>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IHybridCacheService _cache;
    private readonly IMediator _mediator;
    public GetAgeGroupsBySyncGuidHandler(IReadRepositoryFactory readRepository, IHybridCacheService cache, IMediator mediator)
    {
        _readRepository = readRepository;
        _cache = cache;
        _mediator = mediator;
    }
    public async Task<IEnumerable<AgeGroupCategoryDto>?> Handle(GetAgeGroupsBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"justgobooking:age-groups:{request.SyncGuid}";
        string conditionSql = await GetWebletAgeGroupFilterAsync(request.WebletGuid, cancellationToken);

        string sql = $"""
                     DECLARE @BaseUrl Varchar(100) = (SELECT [Value] FROM SystemSettings Where ItemKey = 'SYSTEM.SITEADDRESS')
                     SET @BaseUrl = IIF(RIGHT(@BaseUrl, 1) = '/', LEFT(@BaseUrl, LEN(@BaseUrl) - 1), @BaseUrl);

                     DECLARE @OwnerId INT;
                     IF EXISTS (
                         SELECT 1 from merchantprofile_default mpd 
                         Inner join Document d on d.docid=mpd.docid
                         where d.syncguid = @SyncGuid
                     )
                     BEGIN
                         SET @OwnerId = 0
                     END
                     ELSE
                     BEGIN
                        	SET @OwnerId = (
                        	    SELECT TOP 1 C.DocId
                        	    FROM Clubs_Default C 
                        	    INNER JOIN Document D ON D.DocId = C.DocId
                        	    WHERE D.SyncGuid = @SyncGuid
                        	)
                     END;

                     SELECT DISTINCT Id, AG.[Name], AG.MinAge, AG.MaxAge, 
                     CONCAT(@BaseUrl, '/store/downloadpublic?f=', BA.[Name], '&t=justgobookingattachment&p=', BA.EntityId, '&p1=', BA.EntityTypeId) AS ImageUrl
                     FROM JustGoBookingAgeGroup AG
                     INNER JOIN JustGoBookingClassSession SOP ON SOP.AgeGroupId = AG.Id AND SOP.IsDeleted != 1
                     INNER JOIN JustGoBookingClass C ON C.ClassId = SOP.ClassId AND C.IsDeleted != 1
                     LEFT JOIN JustGoBookingAttachment BA ON BA.EntityId = AG.ID AND BA.EntityTypeId = 6 AND AG.IsActive = 1
                     WHERE AG.OwnerId = @OwnerId AND C.StateId = 2 AND c.ClassBookingType = 2
                     AND SOP.SessionBookingEndDate >= CAST(GETUTCDATE() AS DATE) AND CAST(GETUTCDATE() AS DATE) >= SOP.SessionBookingStartDate
                     {conditionSql};
                     """;

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@SyncGuid", request.SyncGuid.ToString(), DbType.String, size: 200);
        var result = await _cache.GetOrSetAsync<IEnumerable<AgeGroupCategoryDto>>(
                                         cacheKey,
                                         async _ => await _readRepository.GetLazyRepository<AgeGroupCategoryDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text"),
                                         TimeSpan.FromMinutes(10),
                                         [nameof(CacheTag.Class)],
                                         cancellationToken
                                         );
        return result;
    }
    private async Task<string> GetWebletAgeGroupFilterAsync(Guid? webletGuid, CancellationToken cancellationToken)
    {
        if (!webletGuid.HasValue || webletGuid.Value == Guid.Empty)
        {
            return string.Empty;
        }
        try
        {
            var webletConfig = await _mediator.Send(
                new GetWebletConfigurationQuery(webletGuid.Value),
                cancellationToken);
            if (webletConfig?.Config?.Filter?.AgeGroups is null or { Count: 0 })
            {
                return string.Empty;
            }
            var ageGroupConditions = string.Join(", ", webletConfig.Config.Filter.AgeGroups);
            return $" AND AG.Id IN ({ageGroupConditions}) ";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading weblet configuration: {ex.Message}");
            return string.Empty;
        }
    }
}
