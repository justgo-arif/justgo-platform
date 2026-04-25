using Dapper;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetBasicClubDetails;

public class GetBasicClubDetailBySyncGuidHandler : IRequestHandler<GetBasicClubDetailBySyncGuidQuery, BasicClubDetailDto?>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IHybridCacheService _cache;
    private readonly IMediator _mediator;
    public GetBasicClubDetailBySyncGuidHandler(IReadRepositoryFactory readRepository, IHybridCacheService cache, IMediator mediator)
    {
        _readRepository = readRepository;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task<BasicClubDetailDto?> Handle(GetBasicClubDetailBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"justgobooking:basic-club-details:{request.SyncGuid}";
   

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@SyncGuid", request.SyncGuid.ToString());
        var result = await _cache.GetOrSetAsync<BasicClubDetailDto?>(
                                         cacheKey,
                                         async _ => await _readRepository.GetLazyRepository<BasicClubDetailDto>().Value.GetAsync(detailsSql, cancellationToken, queryParameters, null, "text"),
                                         TimeSpan.FromMinutes(10),
                                         [nameof(CacheTag.Class_Setting)],
                                         cancellationToken
                                         );
        return result;
    }
    private static readonly string detailsSql = """
        DECLARE 
        @EntityName VARCHAR(100),
        @EntityId INT;

        DECLARE @BaseUrl Varchar(100) = (SELECT [Value] FROM SystemSettings Where ItemKey = 'SYSTEM.SITEADDRESS')
        SET @BaseUrl = IIF(RIGHT(@BaseUrl, 1) = '/', LEFT(@BaseUrl, LEN(@BaseUrl) - 1), @BaseUrl);
        DECLARE @Currency VARCHAR(50) = (SELECT TOP 1 Value FROM SystemSettings WHERE ItemKey = 'SYSTEM.CURRENCY.DEFAULTCURRENCY')
        DECLARE @CurrencySymbol VARCHAR(50) = (SELECT TOP 1 Value FROM SystemSettings WHERE ItemKey = 'SYSTEM.CURRENCY.DEFAULTCURRENCYSYMBOL')

        IF EXISTS (
            SELECT 1 from merchantprofile_default mpd 
            Inner join Document d on d.docid=mpd.docid
            where d.syncguid = @SyncGuid
        )
        BEGIN
            SET @EntityId = 0
        SET @EntityName = (SELECT [Value] FROM SystemSettings Where ItemKey = 'ORGANISATION.NAME');
        END
        ELSE
        BEGIN
            SELECT TOP 1 @EntityId = H.EntityId, @EntityName = H.EntityName
        FROM Hierarchies H 
        INNER JOIN Document D ON D.DocId = H.EntityId
        WHERE D.SyncGuid = @SyncGuid;
        END;

        DECLARE
        @LogoName VARCHAR(100),
        @HeroImageName VARCHAR(100),
        @ClassBrandColor VARCHAR(100),
        @SocialLinks NVARCHAR(MAX);

        IF(@EntityId > 0)
        BEGIN
        SET @LogoName = (SELECT [Value] FROM EntitySetting WHERE EntityId = @EntityId AND ItemId = (Select ItemId From SystemSettings Where ItemKey = 'ORGANISATION.LOGO'))
        SET @HeroImageName = (SELECT [Value] FROM EntitySetting WHERE EntityId = @EntityId AND ItemId = (Select ItemId From SystemSettings Where ItemKey = 'THEME.CLASS.HERO_IMAGE'))
        SET @ClassBrandColor = (SELECT [Value] FROM EntitySetting WHERE EntityId = @EntityId AND ItemId = (Select ItemId From SystemSettings Where ItemKey = 'THEME.CLASS.BRAND_COLOR'))
        SET @SocialLinks = (SELECT [Value] FROM EntitySetting WHERE EntityId = @EntityId AND ItemId = (Select ItemId From SystemSettings Where ItemKey = 'ORGANISATION.SOCIALMEDIA.LINKS'))

        SELECT @EntityName EntityName, 
        IIF(ISNULL(@LogoName, '') = '', '', CONCAT(@BaseUrl, '/store/downloadpublic?f=', @LogoName, '&t=organizationlogo')) LogoUrl, 
        IIF(ISNULL(@HeroImageName, '') = '', '', CONCAT(@BaseUrl, '/store/downloadpublic?f=', @HeroImageName, '&t=organizationHeroImage')) HeroImageUrl, 
        @ClassBrandColor ClassBrandColor, @SocialLinks SocialLinks, @Currency Currency, @CurrencySymbol CurrencySymbol;
        END
        ELSE
        BEGIN
        SET @LogoName = (SELECT [Value] From SystemSettings Where ItemKey = 'ORGANISATION.LOGO')
        SET @HeroImageName = (SELECT [Value] From SystemSettings Where ItemKey = 'THEME.CLASS.HERO_IMAGE')
        SET @ClassBrandColor = (SELECT [Value] FROM SystemSettings Where ItemKey = 'THEME.CLASS.BRAND_COLOR')
        SET @SocialLinks = (SELECT [Value] FROM SystemSettings Where ItemKey = 'ORGANISATION.SOCIALMEDIA.LINKS')

        SELECT @EntityName EntityName, 
        IIF(ISNULL(@LogoName, '') = '', '', CONCAT(@BaseUrl, '/store/downloadpublic?f=', @LogoName, '&t=organizationlogo')) LogoUrl, 
        IIF(ISNULL(@HeroImageName, '') = '', '', CONCAT(@BaseUrl, '/store/downloadpublic?f=', @HeroImageName, '&t=organizationHeroImage')) HeroImageUrl,
        @ClassBrandColor ClassBrandColor, @SocialLinks SocialLinks, @Currency Currency, @CurrencySymbol CurrencySymbol;
        END
        ;
        """;
}

