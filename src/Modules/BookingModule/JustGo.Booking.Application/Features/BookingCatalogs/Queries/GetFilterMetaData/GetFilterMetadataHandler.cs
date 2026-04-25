using System.Collections.Frozen;
using Json.Schema;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration;
using JustGoAPI.Shared.Miscellaneous;
using System.Text;
using System.Text.Json;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetFilterMetaData;

public class GetFilterMetadataHandler : IRequestHandler<GetFilterMetadataQuery, FilterMetadataDto?>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IHybridCacheService _cache;
    private readonly IMediator _mediator;
    public GetFilterMetadataHandler(IReadRepositoryFactory readRepository, IHybridCacheService cache, IMediator mediator)
    {
        _readRepository = readRepository;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task<FilterMetadataDto?> Handle(GetFilterMetadataQuery request, CancellationToken cancellationToken)
    {
        return await GetFilterMetadata(request, cancellationToken);
    }

    private async Task<FilterMetadataDto?> GetFilterMetadata(GetFilterMetadataQuery request, CancellationToken cancellationToken)
    {
        string categoryFilter = string.Empty;
        string ageGroupFilter = string.Empty;
        string classGroupFilter = string.Empty;
        string colorGroupFilter = string.Empty;
        bool hideGender = false;
        bool hidePaymentTypes = false;
        bool hideDuration = false;
        bool hideWeekDays = false;
        bool hideTimeOfDays = false;
        FrozenSet<KeyValue<string, int>> durationList = ReferenceData.DefaultDurations;
        FrozenSet<KeyValue<string, int>> paymentTypes = ReferenceData.DefaultPriceOptions;
        FrozenSet<KeyValue<string, string>> timeOfDays = ReferenceData.DefaultTimeOfDays;

        if (request.WebletGuid.HasValue && request.WebletGuid != Guid.Empty)
        {
            var webletResponse = await GetWebletConfigAsync(request.WebletGuid, cancellationToken);
            if (webletResponse != null)
            {
                categoryFilter = GetWebletCategoryFilterAsync(webletResponse, cancellationToken);
                ageGroupFilter = GetWebletAgeGroupFilterAsync(webletResponse, cancellationToken);
                classGroupFilter = GetWebletClassGroupFilterAsync(webletResponse, cancellationToken);
                colorGroupFilter = GetWebletColorGroupFilterAsync(webletResponse, cancellationToken);
                hideWeekDays = webletResponse.Config?.Filter?.HideWeekdays ?? false;
                if (webletResponse.Config?.Filter?.TimeOfDay is null or { Count: 0 })
                {
                    hideTimeOfDays = true;
                }
                if (webletResponse.Config?.Filter?.TimeOfDay is { Count: > 0 })
                {
                    hideTimeOfDays = false;
                    var webletTimeOfDays = webletResponse.Config.Filter.TimeOfDay;
                    timeOfDays = ReferenceData.DefaultTimeOfDays
                        .Where(kv => webletTimeOfDays.Contains(kv.Value))
                        .ToFrozenSet();
                }
                hideGender = webletResponse.Config?.Filter?.HideGender ?? false;
                if (webletResponse.Config?.Filter?.PaymentTypes is null or { Count: 0 })
                {
                    hidePaymentTypes = true;
                }
                if (webletResponse.Config?.Filter?.PaymentTypes is {Count: > 0})
                {
                    hidePaymentTypes = false;
                    var webletPaymentTypes = webletResponse.Config.Filter.PaymentTypes;
                    paymentTypes = ReferenceData.DefaultPriceOptions
                        .Where(kv => webletPaymentTypes.Contains(kv.Value))
                        .ToFrozenSet();
                }
                if (webletResponse.Config?.Filter?.ClassDuration is null or { Count: 0 })
                {
                    hideDuration = true;
                }
                if (webletResponse.Config?.Filter?.ClassDuration is { Count: > 0 })
                {
                    hideDuration = false;
                    var webletDuration = webletResponse.Config.Filter.ClassDuration;
                    durationList = ReferenceData.DefaultDurations
                        .Where(kv => webletDuration.Contains(kv.Value))
                        .ToFrozenSet();
                }
            }
        }
        string sql = $"""
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


                            -- AgeGroups
                            --SELECT DISTINCT AG.Name [Label], AG.Id [Value]
                            --FROM JustGoBookingAgeGroup AG
                            --INNER JOIN JustGoBookingClassSession SOP ON SOP.AgeGroupId = AG.Id
                            --WHERE AG.OwnerId = @OwnerId AND IsActive = 1
                            --ORDER BY [Label] ASC;

                            SELECT 
                            AG.Name AS [Label], 
                            AG.Id AS [Value]
                            FROM JustGoBookingAgeGroup AG
                            INNER JOIN JustGoBookingClassSession SOP ON SOP.AgeGroupId = AG.Id 
                            AND SOP.IsDeleted = 0
                            INNER JOIN JustGoBookingClass C ON C.ClassId = SOP.ClassId 
                            AND C.IsDeleted != 1 AND C.ClassBookingType = 2
                            WHERE AG.OwnerId = @OwnerId AND AG.IsActive = 1 {ageGroupFilter}
                            GROUP BY AG.Id, AG.Name
                            ORDER BY AG.Name ASC;

                            -- Disciplines
                            --SELECT DISTINCT BC.DisplayName [Label], BC.CategoryId [Value]
                            --FROM JustGoBookingCategory BC
                            --JOIN JustGoBookingClassCategory CC ON BC.CategoryId = CC.CategoryId AND ISNULL(CC.IsDeleted, 0) = 0
                            --WHERE BC.OwnerId = @OwnerId AND CC.CategoryType = 1 AND BC.ParentId = -1
                            --ORDER BY [Label] ASC;
                            SELECT DISTINCT BC.DisplayName AS [Label], BC.CategoryId AS [Value]
                            FROM JustGoBookingCategory BC
                            INNER JOIN JustGoBookingClassCategory CC ON CC.CategoryId = BC.CategoryId 
                            AND ISNULL(CC.IsDeleted, 0) = 0 AND CC.CategoryType = 1
                            INNER JOIN JustGoBookingClass C ON C.ClassId = CC.ClassId 
                            AND ISNULL(C.IsDeleted, 0) = 0 AND C.ClassBookingType = 2 AND C.OwningEntityId = @OwnerId
                            INNER JOIN JustGoBookingClassSession SOP ON SOP.ClassId = C.ClassId 
                            AND SOP.IsDeleted = 0
                            INNER JOIN JustGoBookingAgeGroup AG ON AG.Id = SOP.AgeGroupId 
                            AND AG.IsActive = 1
                            WHERE 
                            BC.OwnerId = @OwnerId AND BC.ParentId = -1 {categoryFilter}
                            ORDER BY 
                            BC.DisplayName ASC;

                            -- ClassGroups
                            --SELECT C.[Name] [Label], C.ClassId [Value]
                            --FROM JustGoBookingClass C
                            --WHERE C.OwningEntityId = @OwnerId AND ISNULL(C.IsDeleted, 0) = 0
                            --ORDER BY [Label] ASC;
                            SELECT C.[Name] AS [Label], C.ClassId AS [Value]
                            FROM JustGoBookingClass C
                            INNER JOIN JustGoBookingClassSession SOP ON SOP.ClassId = C.ClassId 
                            AND SOP.IsDeleted = 0
                            INNER JOIN JustGoBookingAgeGroup AG ON AG.Id = SOP.AgeGroupId 
                            AND AG.IsActive = 1
                            WHERE C.OwningEntityId = @OwnerId AND ISNULL(C.IsDeleted, 0) = 0 AND C.ClassBookingType = 2 {classGroupFilter}
                            GROUP BY C.ClassId, C.[Name]
                            ORDER BY C.[Name] ASC;

                            --ColorGroups
                            SELECT C.ColorGroupId AS Id, c.ColorName as [Name], c.HexCode  
                            FROM JustGoBookingClassColorGroup C {colorGroupFilter};

                            SELECT [Value] AS GenderOption 
                            FROM SystemSettings WHERE ItemKey = 'ORGANISATION.GENDEROPTIONS';
                            """;

        await using var resultMultipleAsync = await _readRepository.GetLazyRepository<FilterMetadataDto>().Value.GetMultipleQueryAsync(sql, cancellationToken, new { SyncGuid = request.SyncGuid.ToString() }, null, "text");

        if (resultMultipleAsync is null)
        {
            return null;
        }

        var ageGroupsList = (await resultMultipleAsync.ReadAsync<KeyValue<string, int>>()).ToList();
        var disciplinesList = (await resultMultipleAsync.ReadAsync<KeyValue<string, int>>()).ToList();
        var classGroupsList = (await resultMultipleAsync.ReadAsync<KeyValue<string, int>>()).ToList();
        var colorGroupsList = (await resultMultipleAsync.ReadAsync<ColorGroup>()).ToList();
        var genderOptionJson = await resultMultipleAsync.ReadSingleOrDefaultAsync<string?>();


        var filterMetadata = new FilterMetadataDto
        {
            AgeGroups = ageGroupsList,
            Disciplines = disciplinesList,
            ClassGroups = classGroupsList,
            ColorGroups = colorGroupsList,
            GenderOptions = hideGender ? null : ExtractGenderOptions(genderOptionJson),

            Weekdays = hideWeekDays ? null : ReferenceData.DefaultWeekdays,
            TimeOfDays = hideTimeOfDays ? null : timeOfDays,
            PriceOptions = hidePaymentTypes ? null :paymentTypes,
            Duration = hideDuration ? null : durationList
        };

        return filterMetadata;
    }

    private static string[] ExtractGenderOptions(string? genderOptionJson)
    {
        if (string.IsNullOrWhiteSpace(genderOptionJson))
            return [];

        var bytes = Encoding.UTF8.GetBytes(genderOptionJson);
        var reader = new Utf8JsonReader(bytes);

        if (!JsonDocument.TryParseValue(ref reader, out var document) || !document.RootElement.TryGetProperty("GenderOptions", out var genderOptionsElement)) return [];

        var genderOptionsStr = genderOptionsElement.GetString();
        return !string.IsNullOrEmpty(genderOptionsStr)
            ? genderOptionsStr.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
    }

    private async Task<WebletConfigurationResponse?> GetWebletConfigAsync(Guid? webletGuid, CancellationToken cancellationToken)
    {
        if (!webletGuid.HasValue || webletGuid.Value == Guid.Empty)
        {
            return null;
        }
        try
        {
            return await _mediator.Send(
                new GetWebletConfigurationQuery(webletGuid.Value),
                cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading weblet configuration: {ex.Message}");
            return null;
        }
    }
    private string GetWebletCategoryFilterAsync(WebletConfigurationResponse webletResponse, CancellationToken cancellationToken)
    {
        try
        {
            if (webletResponse?.Config?.Filter?.Categories is null or { Count: 0 })
            {
                return string.Empty;
            }
            var categoryConditions = string.Join(", ", webletResponse.Config.Filter.Categories);
            return $" AND BC.CategoryId IN ({categoryConditions}) ";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading weblet configuration: {ex.Message}");
            return string.Empty;
        }
    }
    private string GetWebletAgeGroupFilterAsync(WebletConfigurationResponse webletResponse, CancellationToken cancellationToken)
    {
        try
        {
            if (webletResponse?.Config?.Filter?.AgeGroups is null or { Count: 0 })
            {
                return string.Empty;
            }
            var ageGroupConditions = string.Join(", ", webletResponse.Config.Filter.AgeGroups);
            return $" AND AG.Id IN ({ageGroupConditions}) ";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading weblet configuration: {ex.Message}");
            return string.Empty;
        }
    }
    private string GetWebletClassGroupFilterAsync(WebletConfigurationResponse webletResponse, CancellationToken cancellationToken)
    {
        try
        {
            if (webletResponse?.Config?.Filter?.ClassGroups is null or { Count: 0 })
            {
                return string.Empty;
            }
            var classGroupConditions = string.Join(", ", webletResponse.Config.Filter.ClassGroups);
            return $" AND C.ClassId IN ({classGroupConditions}) ";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading weblet configuration: {ex.Message}");
            return string.Empty;
        }
    }
    private string GetWebletColorGroupFilterAsync(WebletConfigurationResponse webletResponse, CancellationToken cancellationToken)
    {
        try
        {
            if (webletResponse?.Config?.Filter?.ColorGroups is null or { Count: 0 })
            {
                return string.Empty;
            }
            var colorGroupConditions = string.Join(", ", webletResponse.Config.Filter.ColorGroups);
            return $" Where C.ColorGroupId IN ({colorGroupConditions}) ";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading weblet configuration: {ex.Message}");
            return string.Empty;
        }
    }

}