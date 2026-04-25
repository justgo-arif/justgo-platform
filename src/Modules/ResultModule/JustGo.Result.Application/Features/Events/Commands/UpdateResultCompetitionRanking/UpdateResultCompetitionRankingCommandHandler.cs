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

namespace JustGo.Result.Application.Features.Events.Commands.UpdateResultCompetitionRanking;

public class UpdateResultCompetitionRankingCommandHandler : IRequestHandler<UpdateResultCompetitionRankingCommand, Result<UpdateResultCompetitionRankingResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public UpdateResultCompetitionRankingCommandHandler(
        IReadRepositoryFactory readRepositoryFactory,
        IWriteRepositoryFactory writeRepositoryFactory,
        ILogger<UpdateResultCompetitionRankingCommandHandler> logger,
        ISystemSettingsService systemSettingsService,
        IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<Result<UpdateResultCompetitionRankingResponse>> Handle(UpdateResultCompetitionRankingCommand request, CancellationToken cancellationToken)
    {
        var userId = await _utilityService.GetCurrentUserId(cancellationToken);
        var validationResult = await ValidateInputsAsync(request, cancellationToken);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var writeRepo = _writeRepositoryFactory.GetRepository<object>();
            var parameters = new DynamicParameters();

            int rowsAffected;
            Guid recordGuid;
            string operationType;
            int userIdFromGuid = await _utilityService.GetUserIdByUserSyncGuidAsync(request.UserGuid, cancellationToken);

            if (request.RecordGuid.HasValue && request.RecordGuid.Value != Guid.Empty)
            {
                parameters.Add("@RecordGuid", request.RecordGuid.Value);
                parameters.Add("@BeginRating", request.BeginRating);
                parameters.Add("@FinalRating", request.FinalRating);
                parameters.Add("@AdjustmentRating", (request.FinalRating - request.BeginRating));

                const string updateRankingSql = """
                    UPDATE dbo.ResultCompetitionRankings
                    SET BeginRating = @BeginRating,  
                        FinalRating = @FinalRating,
                        AdjustmentRating = @AdjustmentRating
                    WHERE RecordGuid = @RecordGuid;
                """;

                rowsAffected = await writeRepo.ExecuteAsync(updateRankingSql, cancellationToken, parameters, transaction, QueryType.Text);

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<UpdateResultCompetitionRankingResponse>.Failure("No ranking record was updated. Record may not exist.", ErrorType.NotFound);
                }

                recordGuid = request.RecordGuid.Value;
                operationType = "Updated";
            }
            else
            {

                if (request.CompetitionId == 0 || string.IsNullOrEmpty(request.UserGuid))
                {
                    return Result<UpdateResultCompetitionRankingResponse>.Failure("CompetitionId and UserGuid are required when RecordGuid is not provided.", ErrorType.BadRequest);
                }

                var readRepo = _readRepositoryFactory.GetRepository<object>();
                var validationParameters = new DynamicParameters();
                validationParameters.Add("@CompetitionId", request.CompetitionId);

                var competitionExists = await readRepo.GetSingleAsync<int>(
                    "SELECT COUNT(1) FROM ResultCompetition WHERE CompetitionId = @CompetitionId",
                    validationParameters, null, cancellationToken, QueryType.Text);

                if (competitionExists == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<UpdateResultCompetitionRankingResponse>.Failure("Competition not found.", ErrorType.NotFound);
                }


                if (userIdFromGuid == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<UpdateResultCompetitionRankingResponse>.Failure("User not found for the provided UserGuid.", ErrorType.NotFound);
                }

                recordGuid = Guid.NewGuid();
                parameters.Add("@RecordGuid", recordGuid);
                parameters.Add("@CompetitionId", request.CompetitionId);
                parameters.Add("@UserId", userIdFromGuid);
                parameters.Add("@BeginRating", request.BeginRating);
                parameters.Add("@FinalRating", request.FinalRating);
                parameters.Add("@AdjustmentRating", (request.FinalRating - request.BeginRating));

                string? rankingType ; 
               var rankingTypeParams = new DynamicParameters();
               rankingTypeParams.Add("@CompetitionId", request.CompetitionId);

               rankingType = await readRepo.GetSingleAsync<string>(
                   @"select TOP 1 RankingType from 
                          ResultEvents e 
                          INNER join ResultEventType et ON e.ResultEventTypeId=et.ResultEventTypeId
                          left join ResultCompetition cr ON e.EventId=cr.EventId 
                          where cr.CompetitionId=@CompetitionId",
                        rankingTypeParams, null, cancellationToken, QueryType.Text);
                    
                
                
                parameters.Add("@RankingType", rankingType);

                const string insertRankingSql = """
                    INSERT INTO dbo.ResultCompetitionRankings 
                        (CompetitionId, UserId, BeginRating, FinalRating, RankingType, AdjustmentRating, RecordGuid)
                    VALUES 
                        (@CompetitionId, @UserId, @BeginRating, @FinalRating, @RankingType, @AdjustmentRating, @RecordGuid);
                """;

                rowsAffected = await writeRepo.ExecuteAsync(insertRankingSql, cancellationToken, parameters, transaction, QueryType.Text);

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<UpdateResultCompetitionRankingResponse>.Failure("Failed to insert ranking record.", ErrorType.BadRequest);
                }

                operationType = "Created";
            }

            await transaction.CommitAsync(cancellationToken);

            var response = new UpdateResultCompetitionRankingResponse
            {
                RecordGuid = recordGuid,
                IsSuccess = true,
                Message = $"Competition Rankings {operationType.ToLower()} successfully.",
            };

            CustomLog.Event(
                AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.CompetitionRanking.Value,
                operationType == "Created" ? 
                AuditScheme.ResultManagement.CompetitionRanking.Inserted.Value : 
                AuditScheme.ResultManagement.CompetitionRanking.Updated.Value,
                userId,
                userIdFromGuid,
                EntityType.Result,
                request.CompetitionId,
                operationType == "Created" ? 
                    nameof(AuditLogSink.ActionType.Created) : 
                    nameof(AuditLogSink.ActionType.Updated),
                $"Competition Rankings {operationType.ToLower()} successfully for RecordGuid: {recordGuid};"
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
                userId,
                0,
                EntityType.Result,
                0,
                nameof(AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred),
                $"Exception occurred while updating competition ranking for RecordGuid: {request.RecordGuid}. Error: {ex.Message}"
            );

            return Result<UpdateResultCompetitionRankingResponse>.Failure("Failed to update ranking: " + ex.Message, ErrorType.BadRequest);
        }
    }

    private async Task<Result<UpdateResultCompetitionRankingResponse>> ValidateInputsAsync(
        UpdateResultCompetitionRankingCommand request,
        CancellationToken cancellationToken)
    {
        var readRepo = _readRepositoryFactory.GetRepository<object>();

        if (request.RecordGuid.HasValue && request.RecordGuid.Value != Guid.Empty)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@RecordGuid", request.RecordGuid.Value);
            var rankingExists = await readRepo.GetSingleAsync<int>(
                "SELECT COUNT(1) FROM ResultCompetitionRankings WHERE RecordGuid = @RecordGuid",
                parameters, null, cancellationToken, QueryType.Text);

            if (rankingExists == 0)
            {
                return Result<UpdateResultCompetitionRankingResponse>.Failure("Ranking record not found.", ErrorType.NotFound);
            }
        }
        else
        {
            if (request.CompetitionId == 0 || string.IsNullOrEmpty(request.UserGuid))
            {
                return Result<UpdateResultCompetitionRankingResponse>.Failure("CompetitionId and UserGuid are required when RecordGuid is not provided.", ErrorType.BadRequest);
            }

            var parameters = new DynamicParameters();
            parameters.Add("@CompetitionId", request.CompetitionId);

            var competitionExists = await readRepo.GetSingleAsync<int>(
                "SELECT COUNT(1) FROM ResultCompetition WHERE CompetitionId = @CompetitionId",
                parameters, null, cancellationToken, QueryType.Text);

            if (competitionExists == 0)
            {
                return Result<UpdateResultCompetitionRankingResponse>.Failure("Competition not found.", ErrorType.NotFound);
            }


            var userIdFromGuid = await _utilityService.GetUserIdByUserSyncGuidAsync(request.UserGuid, cancellationToken);

            if (userIdFromGuid == 0)
            {
                return Result<UpdateResultCompetitionRankingResponse>.Failure("User not found for the provided UserGuid.", ErrorType.NotFound);
            }
        }

        return new UpdateResultCompetitionRankingResponse();
    }
}