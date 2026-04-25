using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.Events.Commands.UpdateEventCompetition;

public class UpdateEventCompetitionCommandHandler : IRequestHandler<UpdateEventCompetitionCommand, Result<UpdateEventCompetitionResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly ILogger<UpdateEventCompetitionCommandHandler> _logger;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    #region Constants
    private const int UserParticipantType = 1;
    private const int StringValue = 5;
    private const string MatchScoresKey = "Match Scores";

    private const string MatchExistsQuery = "SELECT COUNT(1) FROM ResultCompetitionMatches WHERE MatchId = @MatchId";
    private const string EventExistsQuery = "SELECT COUNT(1) FROM ResultEvents WHERE EventId = @EventId";
    private const string CompetitionExistsQuery = "SELECT COUNT(1) FROM ResultCompetition WHERE CompetitionId = @CompetitionId AND EventId = @EventId";
    private const string RoundExistsQuery = "SELECT COUNT(1) FROM ResultCompetitionRounds WHERE CompetitionRoundId = @RoundId";
    private const string UserExistsQuery = "SELECT COUNT(1) FROM [User] WHERE UserSyncId = @UserId";

    private const string InsertParticipantSql = """
        declare @PlayerUserId int = (select top 1 userid from [user] where UserSyncId = @UserId)
        IF NOT EXISTS (SELECT 1 FROM ResultCompetitionRoundParticipants 
                      WHERE EntityId = @PlayerUserId AND ParticipantType = @ParticipantType AND RoundId = @RoundId)
        BEGIN
            INSERT INTO ResultCompetitionRoundParticipants (RoundId,EntityId, ParticipantType)
            VALUES (@RoundId, @PlayerUserId, @ParticipantType);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            SELECT CompetitionParticipantId FROM ResultCompetitionRoundParticipants 
            WHERE EntityId = @PlayerUserId AND ParticipantType = @ParticipantType AND RoundId = @RoundId;
        END
        """;

    private const string UpdateMatchSql = """
        UPDATE ResultCompetitionMatches 
        SET RoundId = @RoundId, 
            CompetitionParticipantId = @Player1ParticipantId, 
            CompetitionParticipantId2 = @Player2ParticipantId, 
            WinnerCompetitionParticipantId = @WinnerParticipantId
        WHERE MatchId = @MatchId
        """;

    private const string DeleteMatchMetaDataSql = """
        DELETE FROM ResultCompetitionMatchMetaData 
        WHERE MatchId = @MatchId AND [Key] = @Key
        """;

    private const string InsertMatchMetaDataSql = """
        INSERT INTO ResultCompetitionMatchMetaData (MatchId, [Key], Value,DataType)
        VALUES (@MatchId, @Key, @Value,@DataType);
        """;

    private const string DeleteMatchRatingsSql = """
        DELETE FROM ResultCompetitionMatchRatings
        WHERE MatchId = @MatchId;
        """;

    private const string InsertMatchRatingSql = """
        DECLARE @ResolvedUserId INT = (SELECT TOP 1 UserId FROM [User] WHERE UserSyncId = @UserSyncId);
        INSERT INTO ResultCompetitionMatchRatings (MatchId, CompetitionId, CompetitionParticipantId, UserId, RatingChange, IsWinner, RatingChangeStatus)
        VALUES (@MatchId, @CompetitionId, @ParticipantId, @ResolvedUserId, @RatingChange, @IsWinner, @RatingChangeStatus);
        """;

    private const string UpdateFinalRatingSql = """
        UPDATE dbo.ResultCompetitionRankings
        SET FinalRating = isnull(BeginRating,0) + (
            SELECT ISNULL(SUM(ISNULL(rcmr.RatingChange, 0)), 0)
            FROM dbo.ResultCompetitionMatchRatings rcmr
            WHERE rcmr.CompetitionId = @CompetitionId
              AND rcmr.UserId = @UserId
              AND rcmr.IsDeleted = 0
        )
        WHERE CompetitionId = @CompetitionId
          AND UserId = @UserId;
        """;

    #endregion

    public UpdateEventCompetitionCommandHandler(
        IReadRepositoryFactory readRepositoryFactory,
        IWriteRepositoryFactory writeRepositoryFactory,
        ILogger<UpdateEventCompetitionCommandHandler> logger,
        ISystemSettingsService systemSettingsService,
        IUnitOfWork unitOfWork,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _writeRepositoryFactory = writeRepositoryFactory;
        _logger = logger;
        _systemSettingsService = systemSettingsService;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<Result<UpdateEventCompetitionResponse>> Handle(UpdateEventCompetitionCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateInputsAsync(request, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var userId = await _utilityService.GetCurrentUserId(cancellationToken);
            var player1ParticipantId = await GetOrCreateParticipantAsync(request.Player1UserId, request.RoundId, transaction, cancellationToken);
            var player2ParticipantId = await GetOrCreateParticipantAsync(request.Player2UserId, request.RoundId, transaction, cancellationToken);

            var winnerParticipantId = request.WinnerUserId == request.Player1UserId
                ? player1ParticipantId
                : player2ParticipantId;

            await UpdateMatchAsync(request.MatchId, request.RoundId, player1ParticipantId, player2ParticipantId,
                winnerParticipantId, transaction, cancellationToken);

            await UpdateMatchMetaDataAsync(request.MatchId, request.MatchScores, transaction, cancellationToken);

            await UpdateMatchRatingsAsync(
                request.MatchId,
                request.CompetitionId,
                player1ParticipantId,
                player2ParticipantId,
                request.Player1UserId,
                request.Player2UserId,
                request.Player1RatingChange,
                request.Player2RatingChange,
                isPlayer1Winner: request.WinnerUserId == request.Player1UserId,
                transaction,
                cancellationToken);

            await UpdateFinalRatingsAsync(
                request.CompetitionId,
                request.Player1UserId,
                request.Player2UserId,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var matchDetails = await GetMatchDetailsAsync(request.MatchId, cancellationToken);

            var response = new UpdateEventCompetitionResponse
            {
                MatchId = request.MatchId,
                IsSuccess = true,
                Message = "Event competition match updated successfully.",
                MatchDetails = matchDetails
            };

            CustomLog.Event(
               AuditScheme.ResultManagement.Value,
               AuditScheme.ResultManagement.ResultView.Value,
               AuditScheme.ResultManagement.ResultView.Updated.Value,
               userId,
               request.MatchId,
               EntityType.Result,
               request.EventId,
               nameof(AuditLogSink.ActionType.Updated),
               $"Event competition match updated successfully for EventId: {request.EventId}, CompetitionId: {request.CompetitionId}, MatchId: {request.MatchId}"
           );

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            CustomLog.Event(
                AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultUpload.Value,
                AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred.Value,
                0,
                request.MatchId,
                EntityType.Result,
                request.EventId,
                nameof(AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred),
                $"Exception occurred while updating event competition match for EventId: {request.EventId}, CompetitionId: {request.CompetitionId}, MatchId: {request.MatchId}. Error: {ex.Message}"
            );
            return Result<UpdateEventCompetitionResponse>.Failure("Failed to update event competition match.", ErrorType.BadRequest);
        }
    }

    private async Task<Result<UpdateEventCompetitionResponse>> ValidateInputsAsync(
        UpdateEventCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var readRepo = _readRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", request.MatchId);
        var matchExists = await readRepo.GetSingleAsync<int>(MatchExistsQuery, parameters, null, cancellationToken, QueryType.Text);
        if (matchExists == 0)
        {
            return Result<UpdateEventCompetitionResponse>.Failure("Match not found.", ErrorType.NotFound);
        }

        parameters = new DynamicParameters();
        parameters.Add("@EventId", request.EventId);
        var eventExists = await readRepo.GetSingleAsync<int>(EventExistsQuery, parameters, null, cancellationToken, QueryType.Text);
        if (eventExists == 0)
        {
            return Result<UpdateEventCompetitionResponse>.Failure("Event not found.", ErrorType.NotFound);
        }

        parameters = new DynamicParameters();
        parameters.Add("@EventId", request.EventId);
        parameters.Add("@CompetitionId", request.CompetitionId);
        var competitionExists = await readRepo.GetSingleAsync<int>(CompetitionExistsQuery, parameters, null, cancellationToken, QueryType.Text);
        if (competitionExists == 0)
        {
            return Result<UpdateEventCompetitionResponse>.Failure("Competition not found for the specified event.", ErrorType.NotFound);
        }

        parameters = new DynamicParameters();
        parameters.Add("@RoundId", request.RoundId);
        var roundExists = await readRepo.GetSingleAsync<int>(RoundExistsQuery, parameters, null, cancellationToken, QueryType.Text);
        if (roundExists == 0)
        {
            return Result<UpdateEventCompetitionResponse>.Failure("Round not found.", ErrorType.NotFound);
        }

        parameters = new DynamicParameters();
        parameters.Add("@UserId", request.Player1UserId);
        var player1Exists = await readRepo.GetSingleAsync<int>(UserExistsQuery, parameters, null, cancellationToken, QueryType.Text);
        if (player1Exists == 0)
        {
            return Result<UpdateEventCompetitionResponse>.Failure("Player 1 not found.", ErrorType.NotFound);
        }

        parameters = new DynamicParameters();
        parameters.Add("@UserId", request.Player2UserId);
        var player2Exists = await readRepo.GetSingleAsync<int>(UserExistsQuery, parameters, null, cancellationToken, QueryType.Text);
        if (player2Exists == 0)
        {
            return Result<UpdateEventCompetitionResponse>.Failure("Player 2 not found.", ErrorType.NotFound);
        }

        if (request.WinnerUserId != request.Player1UserId &&
            request.WinnerUserId != request.Player2UserId)
        {
            return Result<UpdateEventCompetitionResponse>.Failure("Winner must be one of the two players.", ErrorType.BadRequest);
        }

       

        return new UpdateEventCompetitionResponse();
    }

    private async Task<int> GetOrCreateParticipantAsync(string userId, int roundId, System.Data.Common.DbTransaction transaction, CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@ParticipantType", UserParticipantType);
        parameters.Add("@RoundId", roundId);

        var participantId = await writeRepo.ExecuteScalarAsync<int>(InsertParticipantSql, cancellationToken, parameters, transaction, QueryType.Text);

        return participantId;
    }

    private async Task UpdateMatchAsync(int matchId, int roundId, int player1ParticipantId, int player2ParticipantId, int winnerParticipantId,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", matchId);
        parameters.Add("@RoundId", roundId);
        parameters.Add("@Player1ParticipantId", player1ParticipantId);
        parameters.Add("@Player2ParticipantId", player2ParticipantId);
        parameters.Add("@WinnerParticipantId", winnerParticipantId);

        var rowsAffected = await writeRepo.ExecuteAsync(UpdateMatchSql, cancellationToken, parameters, transaction, QueryType.Text);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("No match was updated. Match may not exist.");
        }
    }

    private async Task UpdateMatchMetaDataAsync(int matchId, string? matchScores, System.Data.Common.DbTransaction transaction, CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();

        var deleteParameters = new DynamicParameters();
        deleteParameters.Add("@MatchId", matchId);
        deleteParameters.Add("@Key", MatchScoresKey);

        await writeRepo.ExecuteAsync(DeleteMatchMetaDataSql, cancellationToken, deleteParameters, transaction, QueryType.Text);

        if (!string.IsNullOrEmpty(matchScores))
        {
            var insertParameters = new DynamicParameters();
            insertParameters.Add("@MatchId", matchId);
            insertParameters.Add("@Key", MatchScoresKey);
            insertParameters.Add("@Value", matchScores);
            insertParameters.Add("@DataType", StringValue);

            await writeRepo.ExecuteAsync(InsertMatchMetaDataSql, cancellationToken, insertParameters, transaction, QueryType.Text);
        }
    }

    private async Task UpdateMatchRatingsAsync(
        int matchId,
        int competitionId,
        int player1ParticipantId,
        int player2ParticipantId,
        string player1UserSyncId,
        string player2UserSyncId,
        int player1RatingChange,
        int player2RatingChange,
        bool isPlayer1Winner,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();

        // Delete existing ratings
        var deleteParameters = new DynamicParameters();
        deleteParameters.Add("@MatchId", matchId);
        await writeRepo.ExecuteAsync(DeleteMatchRatingsSql, cancellationToken, deleteParameters, transaction, QueryType.Text);

        // Insert Player 1 rating
        var p1 = new DynamicParameters();
        p1.Add("@MatchId", matchId, System.Data.DbType.Int32);
        p1.Add("@CompetitionId", competitionId, System.Data.DbType.Int32);
        p1.Add("@ParticipantId", player1ParticipantId, System.Data.DbType.Int32);
        p1.Add("@UserSyncId", player1UserSyncId, System.Data.DbType.String);
        p1.Add("@RatingChange", player1RatingChange, System.Data.DbType.Int32);
        p1.Add("@IsWinner", isPlayer1Winner, System.Data.DbType.Boolean);
        p1.Add("@RatingChangeStatus", (int)RatingChangeStatus.Overridden, System.Data.DbType.Int32);

        await writeRepo.ExecuteAsync(InsertMatchRatingSql, cancellationToken, p1, transaction, QueryType.Text);

        // Insert Player 2 rating
        var p2 = new DynamicParameters();
        p2.Add("@MatchId", matchId, System.Data.DbType.Int32);
        p2.Add("@CompetitionId", competitionId, System.Data.DbType.Int32);
        p2.Add("@ParticipantId", player2ParticipantId, System.Data.DbType.Int32);
        p2.Add("@UserSyncId", player2UserSyncId, System.Data.DbType.String);
        p2.Add("@RatingChange", player2RatingChange, System.Data.DbType.Int32);
        p2.Add("@IsWinner", !isPlayer1Winner, System.Data.DbType.Boolean);
        p2.Add("@RatingChangeStatus", (int)RatingChangeStatus.Overridden, System.Data.DbType.Int32);

        await writeRepo.ExecuteAsync(InsertMatchRatingSql, cancellationToken, p2, transaction, QueryType.Text);
    }

    private async Task<CompetitionMatchDto?> GetMatchDetailsAsync(int matchId, CancellationToken cancellationToken)
    {
        var readRepo = _readRepositoryFactory.GetRepository<object>();
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? "" : siteAddress.TrimEnd('/');

        var sql = $"""
            SELECT 
                rcm.MatchId,
                cast(u1.UserSyncId as nvarchar(50)) AS WinnerParticipantId,
                u1.MemberId AS WinnerMemberId,
                LTRIM(RTRIM(ISNULL(u1.FirstName, '') + ' ' + ISNULL(u1.LastName, ''))) AS WinnerName,
                ISNULL(rcr1.FinalRating, 0) AS WinnerRating,
                u1.Gender AS WinnerGender,
                CASE 
                    WHEN u1.ProfilePicURL IS NOT NULL AND u1.ProfilePicURL != '' AND u1.UserId IS NOT NULL 
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u1.ProfilePicURL + '&t=user&p=' + CAST(u1.UserId AS VARCHAR)
                    ELSE ''
                END AS WinnerImageUrl,
                ISNULL(wmr.RatingChange, 0) AS WinnerRatingChange,
                ISNULL(wmr.RatingChangeStatus, 0) AS WinnerRatingChangeStatus,
                cast(u2.UserSyncId as nvarchar(50)) AS LoserParticipantId,
                u2.MemberId AS LoserMemberId,
                LTRIM(RTRIM(ISNULL(u2.FirstName, '') + ' ' + ISNULL(u2.LastName, ''))) AS LoserName,
                ISNULL(rcr2.FinalRating, 0) AS LoserRating,
                u2.Gender AS LoserGender,
                CASE 
                    WHEN u2.ProfilePicURL IS NOT NULL AND u2.ProfilePicURL != '' AND u2.UserId IS NOT NULL 
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + u2.ProfilePicURL + '&t=user&p=' + CAST(u2.UserId AS VARCHAR)
                    ELSE ''
                END AS LoserImageUrl,
                ISNULL(lmr.RatingChange, 0) AS LoserRatingChange,
                ISNULL(lmr.RatingChangeStatus, 0) AS LoserRatingChangeStatus,
                rcmmd.Value AS MatchScores,
                1 AS IsCompleted
            FROM ResultCompetitionMatches rcm
            LEFT JOIN ResultCompetitionRoundParticipants rcrp1 ON rcm.WinnerCompetitionParticipantId = rcrp1.CompetitionParticipantId
            LEFT JOIN [User] u1 ON rcrp1.EntityId = u1.UserId AND rcrp1.ParticipantType = {UserParticipantType}
            LEFT JOIN (
                SELECT UserId, FinalRating, 
                       ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY FinalRating DESC) as rn
                FROM ResultCompetitionRankings
            ) rcr1 ON rcrp1.EntityId = rcr1.UserId AND rcr1.rn = 1
            LEFT JOIN ResultCompetitionRoundParticipants rcrp2 ON 
                (CASE WHEN rcm.CompetitionParticipantId = rcm.WinnerCompetitionParticipantId 
                      THEN rcm.CompetitionParticipantId2 
                      ELSE rcm.CompetitionParticipantId END) = rcrp2.CompetitionParticipantId
            LEFT JOIN [User] u2 ON rcrp2.EntityId = u2.UserId AND rcrp2.ParticipantType = {UserParticipantType}
            LEFT JOIN (
                SELECT UserId, FinalRating,
                       ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY FinalRating DESC) as rn
                FROM ResultCompetitionRankings
            ) rcr2 ON rcrp2.EntityId = rcr2.UserId AND rcr2.rn = 1
            LEFT JOIN ResultCompetitionMatchMetaData rcmmd ON rcm.MatchId = rcmmd.MatchId AND rcmmd.[Key] = '{MatchScoresKey}'
            LEFT JOIN ResultCompetitionMatchRatings wmr ON wmr.MatchId = rcm.MatchId AND wmr.CompetitionParticipantId = rcm.WinnerCompetitionParticipantId
            LEFT JOIN ResultCompetitionMatchRatings lmr ON lmr.MatchId = rcm.MatchId AND lmr.CompetitionParticipantId != rcm.WinnerCompetitionParticipantId
            WHERE rcm.MatchId = @MatchId
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", matchId);

        var results = await readRepo.GetListAsync<CompetitionMatchDto>(sql, parameters, null, QueryType.Text, cancellationToken);
        return results.FirstOrDefault();
    }

    private async Task UpdateFinalRatingsAsync(
        int competitionId,
        string player1UserSyncId,
        string player2UserSyncId,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();
        int player1UserId = await _utilityService.GetUserIdByUserSyncGuidAsync(player1UserSyncId, cancellationToken);
        int player2UserId = await _utilityService.GetUserIdByUserSyncGuidAsync(player2UserSyncId, cancellationToken);

        foreach (var playerUserId in new[] { player1UserId, player2UserId })
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CompetitionId", competitionId, System.Data.DbType.Int32);
            parameters.Add("@UserId", playerUserId, System.Data.DbType.String);

            await writeRepo.ExecuteAsync(UpdateFinalRatingSql, cancellationToken, parameters, transaction, QueryType.Text);
        }
    }
}