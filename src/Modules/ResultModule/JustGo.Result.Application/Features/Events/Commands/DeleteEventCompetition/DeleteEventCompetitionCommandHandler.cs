using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using Microsoft.Extensions.Logging;

namespace JustGo.Result.Application.Features.Events.Commands.DeleteEventCompetition;

public class DeleteEventCompetitionCommandHandler : IRequestHandler<DeleteEventCompetitionCommand, Result<DeleteEventCompetitionResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly ILogger<DeleteEventCompetitionCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    #region Constants
    private const string MatchExistsQuery = "SELECT COUNT(1) FROM ResultCompetitionMatches WHERE MatchId = @MatchId AND IsDeleted = 0";

    private const string GetMatchInfoSql = """
        SELECT rcm.MatchId, rcm.RoundId, rcm.CompetitionParticipantId, rcm.CompetitionParticipantId2
        FROM ResultCompetitionMatches rcm
        WHERE rcm.MatchId = @MatchId AND rcm.IsDeleted = 0
        """;

    private const string SoftDeleteMatchSql = """
        UPDATE ResultCompetitionMatches 
        SET IsDeleted = 1
        WHERE MatchId = @MatchId AND IsDeleted = 0
        """;

    private const string SoftDeleteMatchRatingsSql = """
        UPDATE ResultCompetitionMatchRatings
        SET IsDeleted = 1
        WHERE MatchId = @MatchId AND IsDeleted = 0
        """;

    private const string UpdateFinalRatingAfterDeleteSql = """
        WITH AffectedPlayers AS (
            SELECT DISTINCT CompetitionId, UserId
            FROM dbo.ResultCompetitionMatchRatings
            WHERE MatchId = @MatchId
        )
        UPDATE rcr
        SET rcr.FinalRating =isnull(rcr.BeginRating,0) + (
            SELECT ISNULL(SUM(ISNULL(rcmr.RatingChange, 0)), 0)
            FROM dbo.ResultCompetitionMatchRatings rcmr
            WHERE rcmr.CompetitionId = rcr.CompetitionId
              AND rcmr.UserId = rcr.UserId
              AND rcmr.IsDeleted = 0
        )
        FROM dbo.ResultCompetitionRankings rcr
        INNER JOIN AffectedPlayers ap 
            ON rcr.CompetitionId = ap.CompetitionId
           AND rcr.UserId = ap.UserId;
        """;
    #endregion

    public DeleteEventCompetitionCommandHandler(
        IReadRepositoryFactory readRepositoryFactory,
        IWriteRepositoryFactory writeRepositoryFactory,
        ILogger<DeleteEventCompetitionCommandHandler> logger,
        IUnitOfWork unitOfWork,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _writeRepositoryFactory = writeRepositoryFactory;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<Result<DeleteEventCompetitionResponse>> Handle(DeleteEventCompetitionCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateMatchExistsAsync(request, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var userId = await _utilityService.GetCurrentUserId(cancellationToken);

            var matchInfo = await GetMatchInfoAsync(request.MatchId, transaction, cancellationToken);
            if (matchInfo == null)
            {
                return Result<DeleteEventCompetitionResponse>.Failure("Match not found.", ErrorType.NotFound);
            }

            await SoftDeleteMatchRatingsAsync(request.MatchId, transaction, cancellationToken);
            await UpdateFinalRatingsAfterDeleteAsync(request.MatchId, transaction, cancellationToken);
            await DeleteMatchAsync(request.MatchId, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var response = new DeleteEventCompetitionResponse
            {
                MatchId = request.MatchId,
                IsSuccess = true,
                Message = "Event competition match deleted successfully."
            };

            CustomLog.Event(
                AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultView.Value,
                AuditScheme.ResultManagement.ResultView.Deleted.Value,
                userId,
                request.MatchId,
                EntityType.Result,
                0,
                nameof(AuditLogSink.ActionType.Deleted),
                $"Event competition match deleted successfully. MatchId: {request.MatchId}, RoundId: {matchInfo.RoundId}"
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
               0,
               nameof(AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred),
               $"Exception occurred while deleting event competition match. MatchId: {request.MatchId}. Error: {ex.Message}"
           );

            _logger.LogError(ex, "Error deleting event competition match. MatchId: {MatchId}", request.MatchId);
            return Result<DeleteEventCompetitionResponse>.Failure("Failed to delete event competition match.", ErrorType.BadRequest);
        }
    }

    private async Task<Result<DeleteEventCompetitionResponse>> ValidateMatchExistsAsync(
        DeleteEventCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var readRepo = _readRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", request.MatchId);

        var matchExists = await readRepo.GetSingleAsync<int>(MatchExistsQuery, parameters, null, cancellationToken, QueryType.Text);
        if (matchExists == 0)
        {
            return Result<DeleteEventCompetitionResponse>.Failure("Match not found.", ErrorType.NotFound);
        }

        return new DeleteEventCompetitionResponse();
    }

    private async Task<MatchInfo?> GetMatchInfoAsync(int matchId, System.Data.Common.DbTransaction transaction, CancellationToken cancellationToken)
    {
        var readRepo = _readRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", matchId);

        var results = await readRepo.GetListAsync<MatchInfo>(GetMatchInfoSql, parameters, transaction, QueryType.Text, cancellationToken);
        return results.FirstOrDefault();
    }

    private async Task SoftDeleteMatchRatingsAsync(int matchId, System.Data.Common.DbTransaction transaction, CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", matchId);

        await writeRepo.ExecuteAsync(SoftDeleteMatchRatingsSql, cancellationToken, parameters, transaction, QueryType.Text);
    }

    private async Task DeleteMatchAsync(int matchId, System.Data.Common.DbTransaction transaction, CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", matchId);

        var rowsAffected = await writeRepo.ExecuteAsync(SoftDeleteMatchSql, cancellationToken, parameters, transaction, QueryType.Text);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException("No match was deleted. Match may not exist or is already deleted.");
        }
    }

    private async Task UpdateFinalRatingsAfterDeleteAsync(int matchId, System.Data.Common.DbTransaction transaction, CancellationToken cancellationToken)
    {
        var writeRepo = _writeRepositoryFactory.GetRepository<object>();

        var parameters = new DynamicParameters();
        parameters.Add("@MatchId", matchId, System.Data.DbType.Int32);

        await writeRepo.ExecuteAsync(UpdateFinalRatingAfterDeleteSql, cancellationToken, parameters, transaction, QueryType.Text);
    }
}