using Dapper;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using System.Data;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetDisciplines;

public class GetDisciplinesBySyncGuidHandler : IRequestHandler<GetDisciplinesBySyncGuidQuery, IEnumerable<DisciplineCategoryDto>?>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IMediator _mediator;
    private readonly IHybridCacheService _cache;
    public GetDisciplinesBySyncGuidHandler(IReadRepositoryFactory readRepository, IHybridCacheService cache, IMediator mediator)
    {
        _readRepository = readRepository;
        _cache = cache;
        _mediator = mediator;
    }
    public async Task<IEnumerable<DisciplineCategoryDto>?> Handle(GetDisciplinesBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"justgobooking:disciplines:{request.SyncGuid}:{request.WebletGuid}";

        string conditionSql = await GetWebletCategoryFilterAsync(request.WebletGuid, cancellationToken);

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

                           SELECT DISTINCT BC.CategoryGuid, BC.DisplayName AS [Name], 
                           CONCAT(@BaseUrl, '/store/downloadpublic?f=', BA.[Name], '&t=justgobookingattachment&p=', BA.EntityId, '&p1=', BA.EntityTypeId) AS ImageUrl
                           FROM JustGoBookingCategory BC
                           JOIN  JustGoBookingClassCategory CC ON BC.CategoryId = CC.CategoryId
                           JOIN JustGoBookingClass C ON C.ClassId = CC.ClassId AND C.IsDeleted != 1
                           JOIN JustGoBookingClassSession CS ON CS.ClassId = C.ClassId AND CS.IsDeleted != 1
                           LEFT JOIN JustGoBookingAttachment BA ON BC.CategoryId = BA.EntityId AND BA.EntityTypeId = 2
                           WHERE BC.OwnerId = @OwnerId AND CC.CategoryType = 1  AND C.ClassBookingType = 2 AND C.StateId = 2
                           AND CS.SessionBookingEndDate >= CAST(GETUTCDATE() AS DATE) AND CAST(GETUTCDATE() AS DATE) >= CS.SessionBookingStartDate
                           
                           {conditionSql}
                           
                           ORDER BY BC.DisplayName ASC;
                           """;

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@SyncGuid", request.SyncGuid.ToString(), DbType.String, size: 200);

        var result = await _cache.GetOrSetAsync<IEnumerable<DisciplineCategoryDto>>(
                                         cacheKey,
                                         async _ => await _readRepository.GetLazyRepository<DisciplineCategoryDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text"),
                                         TimeSpan.FromMinutes(10),
                                         [nameof(CacheTag.Class)],
                                         cancellationToken
                                         );
        return result;
    }
    private async Task<string> GetWebletCategoryFilterAsync(Guid? webletGuid, CancellationToken cancellationToken)
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
            if (webletConfig?.Config?.Filter?.Categories is null or { Count: 0 })
            {
                return string.Empty;
            }
            var categoryConditions = string.Join(", ", webletConfig.Config.Filter.Categories);
            return $" AND BC.CategoryId IN ({categoryConditions}) ";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading weblet configuration: {ex.Message}");
            return string.Empty;
        }
    }
}