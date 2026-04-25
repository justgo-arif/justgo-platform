using System.Data;
using System.Data.Common;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadEquestrian;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadTt.DTOs;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadTt;

public class ConfirmUploadFileTtProcessor : IConfirmUploadFileProcessor
{
    private readonly IWriteRepositoryFactory _writeRepository;
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    private const string InsertResultCompetitionSql = """
                                                      INSERT INTO ResultCompetition (EventId, CompetitionName, DisciplineId, ClassId, CompetitionStatusId, UploadedFileId, StartDate, EndDate, CompetitionType)
                                                      VALUES (@EventId, @CompetitionName, @DisciplineId, @ClassId, @CompetitionStatusId, @UploadedFileId, @StartDate, @EndDate, @CompetitionType);
                                                      SELECT CAST(SCOPE_IDENTITY() AS INT);
                                                      """;

    public ConfirmUploadFileTtProcessor(IWriteRepositoryFactory writeRepository,
        IReadRepositoryFactory readRepository, IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }
    
    public async Task<Result<int>> ProcessAsync(ConfirmUploadFileCommand request, CancellationToken cancellationToken)
    {
        var readRepository = _readRepository.GetRepository<object>();
        var writeRepository = _writeRepository.GetRepository<object>();

        var sessionUserId = await _utilityService.GetCurrentUserId(cancellationToken);

        var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await EventingDisciplineHandler.CheckFileAlreadyConfirmed(request.UploadFileId, readRepository,
                transaction, cancellationToken);

            await CanConfirmFileUploadAsync(request.UploadFileId, readRepository, transaction,
                cancellationToken);

            var eventData = await GetEventDataAsync(request.UploadFileId, cancellationToken);
            
            var discipline = await GetDisciplineAsync(readRepository, eventData.DisciplineId,
                transaction, cancellationToken);
            
            eventData.DisciplineId = discipline.Id;
            
            var competitionId = await PopulateResultData(eventData, writeRepository, transaction, request,
                cancellationToken);
            
            await HandlePreviousFileAsync(readRepository, writeRepository, request.UploadFileId, sessionUserId,
                transaction, cancellationToken);
            
            CustomLog.Event(AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultUpload.Value,
                AuditScheme.ResultManagement.ResultUpload.Confirmed.Value,
                sessionUserId,
                eventData.EventId,
                EntityType.Result,
                competitionId,
                nameof(AuditLogSink.ActionType.Created),
                string.Empty
            );

            await transaction.CommitAsync();
            return 1;
        }
        catch (CustomValidationException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<int>.Failure(exception.Message, ErrorType.BadRequest);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            
            CustomLog.Event(AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultUpload.Value,
                AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred.Value,
                0,
                0,
                EntityType.Result,
                0,
                nameof(AuditLogSink.ActionType.Created),
                ex.Message
            );
            
            return Result<int>.Failure("An unexpected error occurred while processing the file upload. Please try again later or contact support if the issue persists.",
                    ErrorType.InternalServerError);
        }
    }
    
     private async Task<int> PopulateResultData(ResultEventData eventData,
        IWriteRepository<object> writeRepository, DbTransaction transaction,
        ConfirmUploadFileCommand request, CancellationToken cancellationToken)
    {
        var competitionId = await InsertResultCompetitionAsync(eventData, request.UploadFileId,
            writeRepository, transaction, cancellationToken);

        var instanceId = await InsertResultCompetitionInstanceAsync(competitionId, eventData,
            writeRepository, transaction, cancellationToken);
  
        var roundId = await InsertResultCompetitionRoundsAsync(instanceId, eventData, transaction,
            cancellationToken);

         await InsertResultCompetitionParticipantsAsync(request.UploadFileId, roundId, transaction,
            cancellationToken);

        return competitionId;

    }

    private async Task HandlePreviousFileAsync(IReadRepository<object> readRepository,
        IWriteRepository<object> writeRepository, int uploadFileId, int userId,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        var previousFileIdExists = await ImportResultHelper.CheckPreviousFileExistsAsync(readRepository,
            uploadFileId, transaction, cancellationToken);

        if (previousFileIdExists > 0)
        {
            await ImportResultHelper.HandlePreviousFileAsync(writeRepository, previousFileIdExists,
                userId, transaction,
                cancellationToken);
        }
    }

    private static async Task CanConfirmFileUploadAsync(int requestUploadFileId, IReadRepository<object> readRepository,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT COUNT(1) AS ErrorCount
                           FROM ResultUploadedFile UF
                           INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                           WHERE UF.UploadedFileId = @UploadedFileId
                               AND UF.IsDeleted = 0
                               AND UM.IsDeleted = 0
                               AND UM.ErrorMessage <> ''
                           """;

        var parameters = new DynamicParameters();
        parameters.Add("UploadedFileId", requestUploadFileId);

        var count = await readRepository.GetSingleAsync<int>(sql, parameters, transaction,
            cancellationToken, QueryType.Text);

        if (count > 0)
        {
          throw new CustomValidationException(
              "The uploaded file cannot be confirmed because it may have already been processed or contains validation errors. Please review the file and try again.");
        }
    }

    private async Task InsertResultCompetitionParticipantsAsync(int requestUploadFileId, int roundId,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@FileId", requestUploadFileId);
        parameters.Add("@RoundId", roundId);

        await _writeRepository.GetRepository<object>().ExecuteUnboundedAsync("InsertResultParticipant_Matches_Metadata",
            cancellationToken, parameters, transaction);
    }

    private async Task<int> InsertResultCompetitionInstanceAsync(int competitionId, ResultEventData eventData,
        IWriteRepository<object> writeRepository, DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string insertSql = """
                                 INSERT INTO ResultCompetitionInstance (CompetitionId, Name, StartDate, EndDate, Status)
                                 VALUES (@CompetitionId, @InstanceName, @StartDate, @EndDate, 1);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);
                                 """;

        var parameters = new DynamicParameters();
        parameters.Add("@CompetitionId", competitionId);
        parameters.Add("@InstanceName", $"Instance Of {eventData.EventName}");
        parameters.Add("@StartDate", eventData.StartDate);
        parameters.Add("@EndDate", eventData.EndDate);

        var instanceId = await writeRepository.ExecuteScalarAsync<int>(insertSql, cancellationToken, parameters,
            transaction, QueryType.Text);
        return instanceId;
    }

    private async Task<ResultEventData> GetEventDataAsync(int fileId, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT F.DisciplineId, F.EventId, EventName, StartDate, EndDate, F.FileCategory
                           FROM ResultUploadedFile F
                           INNER JOIN ResultEvents E ON F.EventId = E.EventId
                           WHERE UploadedFileId = @FileId
                           """;

        var eventDto = await _readRepository.GetRepository<object>().QueryFirstAsync<ResultEventData>(sql,
            new { FileId = fileId }, null, QueryType.Text, cancellationToken);

        if (eventDto is null)
        {
            throw new CustomValidationException($"No event data found for the uploaded file with ID {fileId}. Please ensure the file is associated with a valid event and try again.");
        }

        return eventDto;
    }

    private static async Task<int> InsertResultCompetitionAsync(ResultEventData eventData,
        int uploadedFileId, IWriteRepository<object> writeRepository, IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var competitionType = 2; 
        if (!string.Equals(eventData.FileCategory, "Result Upload"))
        {
            competitionType = 1;
        }
        
        var parameters = new DynamicParameters();
        parameters.Add("@EventId", eventData.EventId);
        parameters.Add("@CompetitionName", eventData.EventName);
        parameters.Add("@DisciplineId", eventData.DisciplineId);
        parameters.Add("@ClassId", 0);
        parameters.Add("@CompetitionStatusId", (int)ResultCompetitionStatus.Draft);
        parameters.Add("@UploadedFileId", uploadedFileId);
        parameters.Add("@StartDate", eventData.StartDate);
        parameters.Add("@EndDate", eventData.EndDate);
        parameters.Add("@CompetitionType", competitionType);

        var resultCompetitionId = await writeRepository.ExecuteScalarAsync<int>(InsertResultCompetitionSql,
            cancellationToken, parameters, transaction, QueryType.Text);
        return resultCompetitionId;
    }
    
    private static async Task<(string Name, int Id)> GetDisciplineAsync(IReadRepository<object> readRepository,
        int disciplineId,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string getDisciplineSql = """
                                            SELECT D.[Name], D.DisciplineId AS Id
                                            FROM ValidationScopes S
                                            INNER JOIN ResultDisciplines D ON S.ScopeReferenceId = D.DisciplineId
                                            WHERE ValidationScopeId = @DisciplineId
                                            """;
        var discipline = await readRepository.QueryFirstAsync<(string Name, int Id)>(getDisciplineSql,
            new { DisciplineId = disciplineId },
            transaction, QueryType.Text, cancellationToken);

        if (string.IsNullOrEmpty(discipline.Name))
        {
            throw new CustomValidationException($"Discipline not found with ID {disciplineId}");
        }

        return discipline;
    }

    private async Task<int> InsertResultCompetitionRoundsAsync(int instanceId, ResultEventData eventData,
        IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string insertRoundSql = """
                                      INSERT INTO ResultCompetitionRounds (RoundName, CompetitionCategoryId, StartDate, EndDate, InstanceId)
                                      VALUES (@RoundName, @CompetitionCategoryId, @StartDate, @EndDate, @InstanceId);
                                      SELECT CAST(SCOPE_IDENTITY() AS INT);
                                      """;

        var parameters = new DynamicParameters();
        parameters.Add("@RoundName", $"Round Of {eventData.EventName}");
        parameters.Add("@CompetitionCategoryId", 1);
        parameters.Add("@StartDate", eventData.StartDate);
        parameters.Add("@EndDate", eventData.EndDate);
        parameters.Add("@InstanceId", instanceId);

        var roundId = await _writeRepository.GetRepository<object>().ExecuteScalarAsync<int>(insertRoundSql,
            cancellationToken, parameters, transaction, QueryType.Text);

        return roundId;
    }
}