using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Commands.UpdateCompetition;

public class UpdateCompetitionCommandHandler : IRequestHandler<UpdateCompetitionCommand, Result<UpdateCompetitionResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;
    private readonly IAzureBlobFileService _azureBlobFileService;

    public UpdateCompetitionCommandHandler(
        IReadRepositoryFactory readRepositoryFactory,
        IWriteRepositoryFactory writeRepositoryFactory,
        IUnitOfWork unitOfWork,
        IUtilityService utilityService,
        IAzureBlobFileService azureBlobFileService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _writeRepositoryFactory = writeRepositoryFactory;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
        _azureBlobFileService = azureBlobFileService;
    }

    public async Task<Result<UpdateCompetitionResponse>> Handle(UpdateCompetitionCommand request, CancellationToken cancellationToken = default)
    {
        var existingEvent = await GetExistingEventAsync(request.EventId, cancellationToken);
        if (existingEvent is null)
        {
            return Result<UpdateCompetitionResponse>.Failure(
                $"Competition with EventId {request.EventId} was not found.",
                ErrorType.NotFound);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrEmpty(request.ImagePath) && request.ImagePath != existingEvent.ImagePath)
            {
                var sourcePath = await _azureBlobFileService.MapPath(
                    $"~/store/Temp/competitionattachment/attachments/{request.ImagePath}");
                var destinationDir = await _azureBlobFileService.MapPath(
                    $"~/store/competitionattachment/{existingEvent.RecordGuid}");
                var destinationPath = $"{destinationDir}/{request.ImagePath}";
                await _azureBlobFileService.MoveFileAsync(sourcePath, destinationPath);
            }

            var writeRepo = _writeRepositoryFactory.GetRepository<object>();

            const string updateCompetitionSql = """
                UPDATE ResultEvents
                SET
                    EventName        = @EventName,
                    StartDate        = dbo.GET_LOCAL_UTC_DATE_TIME(@StartDate, @TimeZone),
                    EndDate          = dbo.GET_LOCAL_UTC_DATE_TIME(@EndDate, @TimeZone),
                    CategoryId       = @CategoryId,
                    ResultEventTypeId = @ResultEventTypeId,
                    TimeZone         = @TimeZone,
                    ImagePath        = @ImagePath,
                    Postcode         = @Postcode,
                    County           = @County,
                    Town             = @Town,
                    Address1         = @Address1,
                    Address2         = @Address2
                WHERE EventId = @EventId;
                """;

            var parameters = new DynamicParameters();
            parameters.Add("@EventId", request.EventId);
            parameters.Add("@EventName", request.EventName);
            parameters.Add("@StartDate", request.StartDate);
            parameters.Add("@EndDate", request.EndDate);
            parameters.Add("@CategoryId", request.CategoryId);
            parameters.Add("@ResultEventTypeId", request.ResultEventTypeId);
            parameters.Add("@TimeZone", request.TimeZone);
            parameters.Add("@ImagePath", string.IsNullOrEmpty(request.ImagePath) ? existingEvent.ImagePath : request.ImagePath);
            parameters.Add("@Postcode", request.Postcode);
            parameters.Add("@County", request.County);
            parameters.Add("@Town", request.Town);
            parameters.Add("@Address1", request.Address1);
            parameters.Add("@Address2", request.Address2);

            var rowsAffected = await writeRepo.ExecuteAsync(
                updateCompetitionSql, cancellationToken, parameters, transaction, QueryType.Text);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UpdateCompetitionResponse>.Failure(
                    "No rows were updated. The competition may have been deleted.",
                    ErrorType.BadRequest);
            }

            await transaction.CommitAsync(cancellationToken);

            var userId = await _utilityService.GetCurrentUserId(cancellationToken);

            CustomLog.Event(
                AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultView.Value,
                AuditScheme.ResultManagement.ResultView.Updated.Value,
                userId,
                request.EventId,
                EntityType.Result,
                0,
                nameof(AuditLogSink.ActionType.Updated),
                $"Event competition updated successfully for EventId: {request.EventId}"
            );

            return new UpdateCompetitionResponse
            {
                EventId = request.EventId,
                IsSuccess = true,
                Message = "Event competition updated successfully."
            };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<UpdateCompetitionResponse>.Failure(
                "Failed to update event competition.",
                ErrorType.BadRequest);
        }
    }

    private async Task<ExistingEventDto?> GetExistingEventAsync(int eventId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EventId, RecordGuid, ImagePath
            FROM ResultEvents
            WHERE EventId = @EventId;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("@EventId", eventId);

        var readRepo = _readRepositoryFactory.GetRepository<ExistingEventDto>();
        return await readRepo.GetAsync(sql, cancellationToken, parameters, null, QueryType.Text);
    }

    private sealed class ExistingEventDto
    {
        public int EventId { get; set; }
        public string? RecordGuid { get; set; }
        public string? ImagePath { get; set; }
    }
}