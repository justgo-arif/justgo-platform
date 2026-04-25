using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetPlayerProfileMaxScore;

public class GetPlayerProfileMaxScoreQueryHandler : IRequestHandler<GetPlayerProfileMaxScoreQuery,
    Result<PlayerProfileMaxScoreDto>>
{
    private readonly IReadRepository<PlayerProfileMaxScoreDto> _readRepository;
    private readonly ISystemSettingsService _systemSettingsService;
    public GetPlayerProfileMaxScoreQueryHandler(IReadRepository<PlayerProfileMaxScoreDto> readRepository, ISystemSettingsService systemSettingsService)
    {
        _readRepository = readRepository;
        _systemSettingsService = systemSettingsService;
    }

    public async Task<Result<PlayerProfileMaxScoreDto>> Handle(GetPlayerProfileMaxScoreQuery request,
     CancellationToken cancellationToken = default)
    {
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');

         string sql = $"""
                           ;WITH UserCTE AS
                           (
                               SELECT 
                                   u.UserId,
                                   u.MemberId,
                                   u.FirstName,
                                   u.LastName,
                                   u.County,
                                   u.Country,
                                   u.Gender,
                                   u.DOB,
                                   CASE WHEN u.ProfilePicURL <> '' THEN
                                       '{baseImageUrl}/store/downloadPublic?f=' + u.ProfilePicURL + '&t=user&p=' + CAST(u.UserId AS VARCHAR)
                                   ELSE '' END AS PlayerImageUrl
                               FROM dbo.[User] u
                               WHERE u.MemberId = @MemberId
                           ),
                           MaxScorePerDiscipline AS
                           (
                               SELECT 
                                   rcp.UserId,
                                   rc.DisciplineId,
                                   MAX(rcr.Score) AS MaxScore
                               FROM dbo.ResultCompetitionResults rcr
                               INNER JOIN dbo.ResultCompetitionParticipants rcp ON rcr.CompetitionParticipantId = rcp.CompetitionParticipantId
                               INNER JOIN dbo.ResultCompetitionRounds rcrd  ON rcp.CompetitionRoundId = rcrd.CompetitionRoundId
                               INNER JOIN dbo.ResultCompetition rc  ON rcrd.CompetitionId = rc.CompetitionId AND rc.IsDeleted = 0 AND rc.CompetitionStatusId = 2
                               INNER JOIN UserCTE u  ON u.UserId = rcp.UserId
                               GROUP BY rcp.UserId,rc.DisciplineId
                           ),
                           PrimaryClub AS
                           (
                               SELECT TOP 1 
                                   CMR.UserId,
                                   H.EntityName AS ClubName
                               FROM dbo.ClubMemberRoles CMR
                               INNER JOIN dbo.Hierarchies H ON H.EntityId = CMR.ClubDocId
                               WHERE CMR.IsPrimary = 1
                           )

                           SELECT
                               u.MemberId,
                               u.FirstName + ' ' + ISNULL(u.LastName, '') AS UserName,
                               u.County,
                               u.Country,
                               u.Gender,
                               u.DOB,
                               pc.ClubName,
                               PlayerImageUrl,
                               d.Name AS DisciplineName,
                               ISNULL(ms.MaxScore, 0) AS MaxScore
                           FROM UserCTE u
                           LEFT JOIN MaxScorePerDiscipline ms  ON u.UserId = ms.UserId
                           LEFT JOIN dbo.ResultDisciplines d ON ms.DisciplineId = d.DisciplineId
                           LEFT JOIN PrimaryClub pc ON u.UserId = pc.UserId
                           ORDER BY d.Name;
                           """;



                  var results = await _readRepository.GetListAsync(sql, cancellationToken, new { request.MemberId }, null, QueryType.Text);
                  
                  var first = results.FirstOrDefault();
                  if (first == null)
                      return null;
                  
                  var dto = new PlayerProfileMaxScoreDto
                  {
                      MemberId = first.MemberId,
                      UserName = first.UserName,
                      County = first.County,
                      Country = first.Country,
                      Gender = first.Gender,
                      DOB = first.DOB,
                      ClubName = first.ClubName,
                      PlayerImageUrl = first.PlayerImageUrl,
                      Items = results
                          .Where(x => !string.IsNullOrEmpty(x.DisciplineName))
                          .Select(x => new PlayerProfileDisciplineScoreDto
                          {
                              DisciplineName = x.DisciplineName,
                              MaxScore = x.MaxScore
                          })
                          .ToList()
                  };
                  
                  return dto;
                  

    }
}
