using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetPrimaryClubGuid;

public class GetPrimaryClubGuidHandler : IRequestHandler<GetPrimaryClubGuidQuery, string>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUtilityService _utilityService;
    public GetPrimaryClubGuidHandler(IReadRepositoryFactory readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }
    public async Task<string> Handle(GetPrimaryClubGuidQuery request, CancellationToken cancellationToken = default)
    {
        var currentUser = await _utilityService.GetCurrentUser(cancellationToken);
        return await GetPrimaryClubGuidAsync(request, currentUser, cancellationToken);
    }

    private async Task<string> GetPrimaryClubGuidAsync(GetPrimaryClubGuidQuery request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserId", currentUser.UserId);
        queryParameters.Add("MemberDocId", currentUser.MemberDocId);

        var sql = $"""
            DECLARE @ClubGuid VARCHAR(50);
            IF((Select [Value] from SystemSettings Where ItemKey = 'ORGANISATION.ENABLE_MEMBER_GRID_MODULE') = 'true')
            BEGIN
            	Select TOP 1 @ClubGuid = D.SyncGuid
            	FROM ClubMemberRoles CMR
            	INNER JOIN Hierarchies H ON H.EntityId = CMR.ClubDocId 
            	INNER JOIN Document D ON D.DocId = H.EntityId
            	WHERE CMR.UserId = @UserId AND CMR.IsPrimary = 1
                AND H.EntityId IN (SELECT DISTINCT OwningEntityId FROM JustGoBookingClass WHERE OwningEntityId > 0)
            	;

            	IF(@ClubGuid IS NULL)
            	BEGIN
            		Select top 1 @ClubGuid = d.SyncGuid 
            		from merchantprofile_default mpd 
            		Inner join Document d on d.docid=mpd.docid
            		WHERE mpd.docid NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
            		AND mpd.Name != 'JustGo'  and mpd.Merchanttype = 'NGB'
            	END;

            END
            ELSE
            BEGIN
            	SELECT TOP 1 @ClubGuid = D.SyncGuid
            	from Members_Default md
            	inner join Members_links ml on ml.docid = md.docid
            	inner join clubmembers_default cmd on cmd.docid = ml.entityid
            	inner join clubmembers_links cml on cml.docid = cmd.docid
            	INNER join Clubs_Default cd on cd.Docid = cml.entityId
            	INNER JOIN Document D ON D.DocId = cd.Docid
            	WHERE cmd.Isprimary = 1 AND md.docid = @MemberDocId
                AND CD.DocId IN (SELECT DISTINCT OwningEntityId FROM JustGoBookingClass WHERE OwningEntityId > 0)
            	;

            	IF(@ClubGuid IS NULL)
            	BEGIN
            		Select top 1 @ClubGuid = d.SyncGuid 
            		from merchantprofile_default mpd 
            		Inner join Document d on d.docid=mpd.docid
            		WHERE mpd.docid NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
            		AND mpd.Name != 'JustGo'  and mpd.Merchanttype = 'NGB'
            	END;

            END

            SELECT @ClubGuid ClubGuid;
            ;
            """;
        var result = await _readRepository.GetLazyRepository<object>().Value.GetSingleAsync<string>(sql, queryParameters, null, cancellationToken, "text");

        return result ?? string.Empty;
    }
}
