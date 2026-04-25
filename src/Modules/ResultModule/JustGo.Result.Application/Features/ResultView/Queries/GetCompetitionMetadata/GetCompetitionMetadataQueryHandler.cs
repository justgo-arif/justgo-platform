using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetCompetitionMetadata;

public class GetCompetitionMetadataQueryHandler : IRequestHandler<GetCompetitionMetadataQuery, CompetitionCreateMetadataDto>
{
    private readonly LazyService<IReadRepository<TimeZoneDto>> _timeZoneRepository;
    private readonly LazyService<IReadRepository<CategoryDto>> _categoryRepository;
    private readonly LazyService<IReadRepository<ResultEventTypeDto>> _resultEventTypeRepository;
    private readonly IUtilityService _utilityService;

    public GetCompetitionMetadataQueryHandler(
        LazyService<IReadRepository<TimeZoneDto>> timeZoneRepository,
        LazyService<IReadRepository<CategoryDto>> categoryRepository,
        LazyService<IReadRepository<ResultEventTypeDto>> resultEventTypeRepository,
        IUtilityService utilityService)
    {
        _timeZoneRepository = timeZoneRepository;
        _categoryRepository = categoryRepository;
        _resultEventTypeRepository = resultEventTypeRepository;
        _utilityService = utilityService;
    }

    public async Task<CompetitionCreateMetadataDto> Handle(GetCompetitionMetadataQuery request, CancellationToken cancellationToken)
    {
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
        // TimeZone Query
        var timeZoneSql = @"
                      DECLARE @Country VARCHAR(100)

                        SELECT @Country = [value]
                        FROM SystemSettings
                        WHERE ItemKey = 'Organisation.country'
                        
                        IF (@Country IS NULL OR LTRIM(RTRIM(@Country)) = '')
                        BEGIN
                            SET @Country = 'United States'   
                        END
                        
                        
                        DECLARE @Timezone TABLE
                        (
                            Zone_Id INT,
                            ABBR VARCHAR(10),
                            Zone_Name VARCHAR(150),
                            [Offset] DECIMAL(4,2),
                            DST BIT,
                            TZDBZoneIdentifier NVARCHAR(500)
                        )
                        
                        INSERT INTO @Timezone
                        (
                            Zone_Id,
                            ABBR,
                            Zone_Name,
                            [Offset],
                            DST,
                            TZDBZoneIdentifier
                        )
                        SELECT  
                            tzz.zone_id,Y.abbreviation,
                        
                            REPLACE( REPLACE( REPLACE( REPLACE( REPLACE(tzz.zone_name + ' (UTC' +
                                                CASE  WHEN Y.gm_offset IS NOT NULL  THEN
                                                 CASE  WHEN Y.gm_offset >= 0 THEN '+' ELSE '-' END + FORMAT(ABS(Y.gm_offset/3600.0), '0.##') ELSE '' END + ')' ,'.50',':30'),'.75',':45') ,'.25',':15'),'.00',':00'),'+-','-') AS Zone_Name,
                        
                                                 CASE WHEN Y.gm_offset IS NOT NULL  THEN Y.gm_offset / 3600.0 ELSE NULL END AS [Offset],Y.dst,tzz.TZDBZoneIdentifier FROM Timezone_Zone tzz
                        
                          INNER JOIN Timezone_Country tzc ON tzc.country_code = tzz.Country_code
                        OUTER APPLY
                        (
                            SELECT TOP 1  gm_offset,abbreviation,dst FROM Timezone
                            WHERE zone_id = tzz.zone_id ORDER BY time_start DESC
                        ) AS Y
                        
                        WHERE LOWER(tzc.country_name) = LOWER(@Country)
                        
                        SELECT Zone_Id as ZoneId ,ABBR as Abbreviation, Zone_Name as ZoneName,Offset ,DST as Dst,TZDBZoneIdentifier as TZDBZoneIdentifier FROM @Timezone WHERE abbr IS NOT NULL ORDER BY Zone_Name 
                        ";

        var timeZones = (await _timeZoneRepository.Value.GetListAsync(timeZoneSql, cancellationToken, commandType: "text")).ToList();


        string categorySqlCondition = string.Empty;
        string eventTypeSqlCondition = string.Empty;

        var parameters = new DynamicParameters();
        if (ownerId >= 0)
        {
            parameters.Add("@OwnerId", ownerId);
            categorySqlCondition = " AND OwnerId = @OwnerId ";
            eventTypeSqlCondition = " and ( ( @ownerId > 0 and rc.ShowInMetadata =1 )  or  ( @ownerId = 0 and rc.ShowInMetadata in (1,2) ) ) ";
        }
        // Category Query
        var categorySql = $@"
    SELECT EventCategoryId, CategoryName, RecordGuid, EventTypeId as ResultEventTypeId
    FROM ResultEventCategory
    WHERE IsDeleted = 0 AND OwnerId = -1

    UNION ALL

    SELECT EventCategoryId, CategoryName, RecordGuid, EventTypeId as ResultEventTypeId
    FROM ResultEventCategory
    WHERE IsDeleted = 0  {categorySqlCondition}
    ORDER BY CategoryName";

        

        var categories = (await _categoryRepository.Value.GetListAsync(categorySql, cancellationToken, parameters, commandType: "text")).ToList();

        // ResultEventType Query
        var resultEventTypeSql = $@"
            SELECT rc.ResultEventTypeId,
                rc.RecordGuid,
                rc.TypeName,
                rc.Description AS Caption
            FROM dbo.ResultEventType rc where IsActive=1 
            {eventTypeSqlCondition}
          ";

        
        var resultEventTypes = (await _resultEventTypeRepository.Value.GetListAsync(resultEventTypeSql, cancellationToken, parameters, commandType: "text")).ToList();

        return new CompetitionCreateMetadataDto
        {
            TimeZones = timeZones,
            Categories = categories,
            ResultEventTypes = resultEventTypes
        };
    }
}

