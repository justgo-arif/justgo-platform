using Dapper;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.DTOs;
using JustGoAPI.Shared.Helper;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetClubs;

public class GetClubsHandler : IRequestHandler<GetClubsQuery, KeysetPagedResult<ClubDto>>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly ISystemSettingsService _systemSettings;
    private readonly UserLocation _userLocation;

    public GetClubsHandler(IReadRepositoryFactory readRepository, ISystemSettingsService systemSettings, UserLocation userLocation)
    {
        _readRepository = readRepository;
        _systemSettings = systemSettings;
        _userLocation = userLocation;
    }

    public async Task<KeysetPagedResult<ClubDto>> Handle(GetClubsQuery request, CancellationToken cancellationToken)
    {
        var data = await GetClubsAsync(request, cancellationToken);

        var hasMore = data.Count > request.NumberOfRow;
        if (hasMore)
            data.RemoveAt(data.Count - 1);

        return new KeysetPagedResult<ClubDto>()
        {
            Items = data,
            TotalCount = (request.TotalRows.HasValue && request.TotalRows.Value > 0) ? request.TotalRows.Value : data.FirstOrDefault()?.TotalRows ?? 0,
            HasMore = hasMore,
            LastSeenId = data.LastOrDefault()?.RowNumber ?? 0
        };
    }

    private async Task<List<ClubDto>> GetClubsAsync(GetClubsQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserSyncId", request.UserSyncId);
        queryParameters.Add("LastSeenId", request.LastSeenId ?? 0);
        queryParameters.Add("NumberOfRows", request.NumberOfRow + 1);

        (string sortSql, string joinSql, string conditionSql) = await AddQueryConditions(request, queryParameters, cancellationToken);

        string totalRowsQuery = (request.TotalRows ?? 0) > 0 ? $"{request.TotalRows}" : "(SELECT COUNT(1) FROM DISTINCT_CLUBS)";

        var sql = $"""
            DECLARE @LevelNo INT = (SELECT MAX(LevelNo) FROM HierarchyTypes);

            WITH ALL_CLUBS AS (
                SELECT H.EntityId DocId, ROW_NUMBER() OVER (ORDER BY {sortSql}) RowNumber
                FROM Hierarchies H 
                INNER JOIN Clubs_Default CD ON CD.DocId = H.EntityId
                INNER JOIN HierarchyTypes T ON T.Id = H.HierarchyTypeId AND T.LevelNo = @LevelNo
                WHERE H.EntityId > 0
                {conditionSql}
            ),
            DISTINCT_CLUBS AS (
                SELECT 
                C.DocId, MIN(C.RowNumber) RowNumber 
                FROM ALL_CLUBS C
                GROUP BY C.DocId
            ),
            CLUBS AS (
                SELECT TOP (@NumberOfRows) 
                C.DocId, C.RowNumber, {totalRowsQuery} TotalRows 
                FROM DISTINCT_CLUBS C  
                WHERE RowNumber > @LastSeenId
                ORDER BY RowNumber
            ),
            JOIND_CLUBS AS (
                SELECT CD.DocId 
                FROM [User] U
            	INNER JOIN Members_links ml on ml.docid = U.MemberDocId AND Entityparentid = 3
            	INNER JOIN clubmembers_default cmd on cmd.docid = ml.entityid
            	INNER JOIN clubmembers_links cml on cml.docid = cmd.docid AND cml.Entityparentid = 2
            	INNER JOIN Clubs_Default cd on cd.Docid = cml.entityId
                INNER JOIN CLUBS ON CLUBS.DocId = CD.DocId
            	WHERE U.UserSyncId = @UserSyncId
            ),
            CLUBS_INFO AS (
                SELECT CD.DocId ClubDocId, D.SyncGuid, CD.ClubName, 
                CD.ClubaddressLine1 Address1, CD.ClubaddressLine2 Address2, CD.ClubaddressLine3 Address3, 
                CD.Clubtown Town, CD.Clubpostcode Postcode, CD.Region County, CD.ClubCountry Country,
                CD.[Location] ClubImage,
                CASE 
                    WHEN LEN(CD.Latlng) - LEN(REPLACE(CD.Latlng, '.', '')) = 2  AND LEN(CD.Latlng) - LEN(REPLACE(CD.Latlng, ',', '')) = 1 THEN CD.Latlng
                    ELSE '' 
                END Latlng,
                CLUBS.RowNumber,
                CLUBS.TotalRows
                FROM CLUBS
                INNER JOIN Clubs_Default CD ON CD.DocId = CLUBS.DocId
                INNER JOIN [Document] D ON D.DocId = CD.DocId 
            )
            SELECT CD.ClubDocId, CD.SyncGuid, CD.ClubName, CD.Address1, CD.Address2, CD.Address3, CD.Town,
            CD.Postcode, CD.County, CD.Country, CD.ClubImage, 
            SUBSTRING(CD.Latlng, 0, CHARINDEX(',', CD.Latlng)) Lat,
            SUBSTRING(CD.Latlng, CHARINDEX(',', CD.Latlng)+1, LEN(CD.Latlng)) Lng,
            SIGN(ISNULL(JC.DOcID, 0)) IsJoined,
            dbo.CalculateDistance(@Lat, @Lng, (ISNULL(SUBSTRING(CD.[Latlng], 0, CHARINDEX(',', CD.[Latlng])), 0)),
            (ISNULL(SUBSTRING(CD.[Latlng], CHARINDEX(',', CD.[Latlng]) + 1, LEN(CD.[Latlng])), 0)), @IsKm) Distance,
            CD.RowNumber, CD.TotalRows
            FROM CLUBS_INFO CD
            LEFT JOIN JOIND_CLUBS JC ON JC.DocId = CD.ClubDocId
            ORDER BY CD.RowNumber
            """;

        return (await _readRepository.GetLazyRepository<ClubDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }

    private async Task<(string, string, string)> AddQueryConditions(GetClubsQuery request, DynamicParameters queryParameters, CancellationToken cancellationToken)
    {
        int iskm = 1;
        string? unit = await _systemSettings.GetSystemSettingsByItemKey("CLUB.CLUBFINDERDEFAULTDISTANCEUNIT", cancellationToken);
        if (unit?.ToLower() == "mile") iskm = 0;
        queryParameters.Add("IsKm", iskm);

        string lat, lng;
        if (!string.IsNullOrWhiteSpace(request.Lat) && !string.IsNullOrWhiteSpace(request.Lng))
        {
            lat = request.Lat;
            lng = request.Lng;
        }
        else
        {
            (lat, lng) = await _userLocation.GetUserLocationAsync();
        }
        queryParameters.Add("Lat", lat);
        queryParameters.Add("Lng", lng);

        #region SORT & ORDER
        string sortSql = "";
        string orderBy = request.OrderBy.ToUpper() == "DESC" ? "DESC" : "ASC";
        if (request.SortBy.ToLower() == "name")
        {
            sortSql = $"CD.ClubName {orderBy}";
        }
        else if (request.SortBy.ToLower() == "distance")
        {
            sortSql = $@"dbo.CalculateDistance(@Lat, @Lng, (ISNULL(SUBSTRING(CD.[Latlng], 0, CHARINDEX(',', CD.[Latlng])), 0)),
                    (ISNULL(SUBSTRING(CD.[Latlng], CHARINDEX(',', CD.[Latlng]) + 1, LEN(CD.[Latlng])), 0)), @IsKm) {orderBy}";
        }
        else
        {
            sortSql = $"CD.ClubName {orderBy}";
        }
        #endregion

        #region FILTERS
        string conditionSql = string.Empty;
        if (request.Regions?.Length > 0)
        {
            queryParameters.Add("Regions", string.Join(",", request.Regions));
            conditionSql += $@" 
                AND EXISTS(
                    SELECT 1 FROM string_split(@Regions,',') R
                    WHERE CD.Region = R.value
				)
                ";
        }

        if (request.ClubTypes?.Length > 0)
        {
            queryParameters.Add("ClubTypes", string.Join(",", request.ClubTypes));
            conditionSql += $@" 
                AND EXISTS(
                    SELECT 1 FROM string_split(@ClubTypes,',') R
                    WHERE CD.ClubType = R.value
				)
                ";
        }

        if (request.Distance > 0)
        {
            conditionSql += $@" 
                AND LEN(CD.Latlng) - LEN(REPLACE(CD.Latlng, '.', '')) = 2
                AND LEN(CD.Latlng) - LEN(REPLACE(CD.Latlng, ',', '')) = 1
                AND dbo.CalculateDistance(@Lat, @Lng, (ISNULL(SUBSTRING(CD.[Latlng], 0, CHARINDEX(',', CD.[Latlng])), 0)),
                (ISNULL(SUBSTRING(CD.[Latlng], CHARINDEX(',', CD.[Latlng]) + 1, LEN(CD.[Latlng])), 0)), @IsKm) <= {request.Distance}
                ";
        }

        if (!string.IsNullOrWhiteSpace(request.KeySearch))
        {
            string keySearch = $"%{request.KeySearch}%";
            if (keySearch.Contains("'")) keySearch = keySearch.Replace("'", "''");
            //keySearch = Regex.Replace(keySearch, @"\s", "");

            conditionSql += $@" 
                AND (
                    coalesce(convert(nvarchar(100), CD.ClubName), '') +
                    coalesce(convert(nvarchar(100), CD.ClubId), '') +
                    coalesce(convert(nvarchar(100), CD.Clubtown), '') +
                    coalesce(convert(nvarchar(100), CD.Clubpostcode), '')
                ) like '%{keySearch}%'";
        }
        #endregion

        string joinSql = string.Empty;

        return (sortSql, joinSql, conditionSql);
    }

}
