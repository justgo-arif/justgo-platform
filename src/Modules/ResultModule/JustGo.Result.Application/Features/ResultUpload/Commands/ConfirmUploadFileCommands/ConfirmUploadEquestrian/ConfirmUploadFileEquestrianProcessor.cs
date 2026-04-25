using System.Data.Common;
using System.Globalization;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;
using Microsoft.Data.SqlClient;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadEquestrian;

public class ConfirmUploadFileEquestrianProcessor : IConfirmUploadFileProcessor
{
    private readonly IWriteRepositoryFactory _writeRepository;
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    private const int DefaultBatchSize = 1000;

    public ConfirmUploadFileEquestrianProcessor(IWriteRepositoryFactory writeRepository,
        IReadRepositoryFactory readRepository, IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<Result<int>> ProcessAsync(ConfirmUploadFileCommand request, CancellationToken cancellationToken)
    {
        var coordinatorTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        bool isCoordinatorTransactionCommitted = false;
        try
        {
            var readRepository = _readRepository.GetRepository<object>();
            var writeRepository = _writeRepository.GetRepository<object>();

            await ConfirmUploadFileHelper.ValidateFileEligibilityAsync(request.UploadFileId, _utilityService,
                readRepository, writeRepository,
                coordinatorTransaction, cancellationToken);

            var fileMetadata = await ConfirmUploadFileHelper.GetFileMetadataAsync(request.UploadFileId, readRepository,
                coordinatorTransaction,
                cancellationToken);

            await coordinatorTransaction.CommitAsync(cancellationToken);
            isCoordinatorTransactionCommitted = true;

            if (!string.Equals(fileMetadata.Discipline.Name, DisciplineConstants.Eventing,
                    StringComparison.OrdinalIgnoreCase))
                return await ProcessStandardDisciplineWithStreamingAsync(fileMetadata, cancellationToken);
            
            var eventingHandler = new EventingDisciplineHandler(_writeRepository, _readRepository, _unitOfWork);
            return await eventingHandler.Handle(request, cancellationToken);
        }
        catch (CustomValidationException ex)
        {
            if (!isCoordinatorTransactionCommitted)
            {
                await coordinatorTransaction.RollbackAsync();
            }

            return Result<int>.Failure(
                ex.Message,
                ErrorType.BadRequest);
        }
        catch (Exception ex)
        {
            if (!isCoordinatorTransactionCommitted)
            {
                await coordinatorTransaction.RollbackAsync();
            }

            // CustomLog.Exception(AuditScheme.ResultManagement.Value,
            //     AuditScheme.ResultManagement.ResultUpload.Value,
            //     AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred.Value,
            //     0, 0, EntityType.Result, 0,
            //     nameof(AuditLogSink.ActionType.Created), ex.Message);

            return Result<int>.Failure(
                "An unexpected error occurred while processing the upload file. Please try again later or contact support if the issue persists.",
                ErrorType.InternalServerError);
        }
    }

    private async Task<Result<int>> ProcessStandardDisciplineWithStreamingAsync(
        FileMetadata fileMetadata,
        CancellationToken cancellationToken)
    {
        var competitionAdditionalFields = await ConfirmUploadFileHelper.GetDisciplineFieldMappingsAsync(
            fileMetadata.Discipline.Id, "ResultCompetition", _readRepository,
            cancellationToken);

        var uniqueClassInfos = await ConfirmUploadFileHelper.GetUniqueClassInfoFromDatabaseAsync(
            _readRepository,
            fileMetadata.UploadedFileId, fileMetadata.Discipline,
            competitionAdditionalFields.Select(x => x.SourceFieldName).ToList(),
            cancellationToken);

        if (uniqueClassInfos.Count == 0)
        {
            return Result<int>.Failure(
                $"No valid {fileMetadata.Discipline.ClassKey} were found in the uploaded file. Please ensure the file contains valid {fileMetadata.Discipline.ClassKey} data and try again.",
                ErrorType.BadRequest);
        }

        await using var coordinatorTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var processedCompetitions = 0;
        foreach (var classInfo in uniqueClassInfos)
        {
            try
            {
                await ProcessSingleClassWithStreamingAsync(
                    classInfo, fileMetadata, fileMetadata.Discipline.ClassKey, coordinatorTransaction,
                    cancellationToken);
                processedCompetitions++;
            }
            catch (Exception)
            {
                await coordinatorTransaction.RollbackAsync();
                throw;
            }
        }

        await coordinatorTransaction.CommitAsync();
        return processedCompetitions;
    }

    private async Task ProcessSingleClassWithStreamingAsync(
        ClassInfo classInfo,
        FileMetadata fileMetadata,
        string? classKey,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var readRepo = _readRepository.GetRepository<object>();
        var writeRepo = _writeRepository.GetRepository<object>();

        string[] acceptedFormats = ["dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy"];

        DateTime.TryParseExact(classInfo.CompStartDate, acceptedFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate);
        DateTime.TryParseExact(classInfo.CompEndDate, acceptedFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate);

        var competitionInfo = new CompetitionInfo(
            classInfo.ClassName, classInfo.ClassName, fileMetadata.Discipline.Id,
            fileMetadata.EventId, fileMetadata.UploadedFileId, startDate, endDate, classInfo.AdditionalData);

        var repositoryContext = new RepositoryContext(readRepo, writeRepo, transaction);
        var competitionId =
            await ConfirmUploadFileHelper.InsertResultCompetitionAsync(competitionInfo, repositoryContext,
                cancellationToken);

        var competitionRoundId = await ConfirmUploadFileHelper.InsertResultCompetitionRound(
            new RoundInfo
            {
                RoundName = "Final",
                RoundStartDate = startDate.Date,
                RoundEndDate = endDate.Date
            },
            competitionId, repositoryContext, cancellationToken);

        await InsertStandardDisciplineResultCompetitionOfficial(
            fileMetadata.UploadedFileId, competitionId, classInfo.ClassName,
            writeRepo, transaction, cancellationToken);

        await ProcessClassDataInStreamingBatchesAsync(
            fileMetadata.UploadedFileId, competitionRoundId, classInfo.ClassName,
            classKey, fileMetadata.Discipline.Id, writeRepo, readRepo,
            transaction, cancellationToken);
    }

    private static async Task InsertStandardDisciplineResultCompetitionOfficial(int uploadedFileId, int competitionId,
        string? className, IWriteRepository<object> writeRepository,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await writeRepository.ExecuteAsync("ExtractResultCompetitionOfficialData",
                cancellationToken,
                new { FileId = uploadedFileId, ClassName = className, CompetitionId = competitionId },
                transaction, QueryType.StoredProcedure);
        }
        catch (SqlException sqlEx) when (sqlEx.Number is 50001 or 50002)
        {
            throw new CustomValidationException(sqlEx.Message);
        }
        catch (Exception ex)
        {
            throw new CustomValidationException(
                $"Failed to insert competition official for competition ID {competitionId}: {ex.Message}");
        }
    }

    private async Task ProcessClassDataInStreamingBatchesAsync(
        int uploadedFileId,
        int competitionRoundId,
        string? className,
        string? classKey,
        int disciplineId,
        IWriteRepository<object> writeRepository,
        IReadRepository<object> readRepository,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        var hasMoreData = true;

        while (hasMoreData && !cancellationToken.IsCancellationRequested)
        {
            var participantBatch = await ConfirmUploadFileHelper.GetParticipantBatchForClassAsync(
                uploadedFileId, className, classKey, offset, DefaultBatchSize,
                readRepository, transaction, cancellationToken);

            hasMoreData = participantBatch.Count == DefaultBatchSize;

            if (participantBatch.Count > 0)
            {
                // Process this batch: participants -> assets -> results -> result data
                await ProcessParticipantBatchPipeline(
                    participantBatch, competitionRoundId,
                    disciplineId, writeRepository, transaction, cancellationToken);
            }

            offset += DefaultBatchSize;
        }
    }

    private async Task ProcessParticipantBatchPipeline(
        List<ParticipantBatchData> participantBatch,
        int competitionRoundId,
        int disciplineId,
        IWriteRepository<object> writeRepository,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (participantBatch.Count == 0) return;

        // Step 1: Insert participants
        var participantIds = await ConfirmUploadFileHelper.InsertParticipantBatchAsync(
            participantBatch.Select(p => p.UserId).ToArray(),
            competitionRoundId, writeRepository, transaction, cancellationToken);

        // Step 2: Insert assets
        await InsertAssetBatchAsync(participantBatch, participantIds,
            writeRepository, transaction, cancellationToken);

        foreach (var batch in participantBatch)
        {
            batch.ResetProcessingMarker();
        }

        // Step 3: Insert results
        var resultIds = await ConfirmUploadFileHelper.InsertResultsBatchAsync(_readRepository, participantBatch,
            participantIds, disciplineId, writeRepository,
            transaction, cancellationToken);

        foreach (var batch in participantBatch)
        {
            batch.ResetProcessingMarker();
        }

        // Step 4: Insert result data
        await ConfirmUploadFileHelper.InsertResultDataBatchAsync(_readRepository, participantBatch, resultIds,
            disciplineId,
            writeRepository, transaction, cancellationToken);
    }

    private static async Task InsertAssetBatchAsync(
        List<ParticipantBatchData> participantBatch,
        List<(int CompetitionParticipantId, int UserId)> participantIds,
        IWriteRepository<object> writeRepository,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var assetData = new List<(int CompetitionParticipantId, string AssetReference)>();

        foreach (var (participantId, userId) in participantIds)
        {
            var userBatch = participantBatch
                .FirstOrDefault(p => p.UserId == userId && !p.IsProcessed);

            if (userBatch == null) continue;
            userBatch.MarkAsProcessed();

            var horseId = userBatch.GetHorseId();
            if (string.IsNullOrWhiteSpace(horseId)) continue;

            userBatch.ParticipantId =
                participantId; //This is very important for identifying participant in further steps - fahim
            assetData.Add((participantId, horseId));
        }

        if (assetData.Count == 0) return;

        var participantAssetValues = assetData.Select((_, index) =>
            $"(@CompetitionParticipantId{index}, @AssetReference{index})");

        var bulkInsertWithResolutionSql = $"""
                                           WITH ParticipantAssetInput(CompetitionParticipantId, AssetReference) AS (
                                               SELECT * FROM (VALUES 
                                                   {string.Join(",\n ", participantAssetValues)}
                                               ) AS InputData(CompetitionParticipantId, AssetReference)
                                           ),
                                           ResolvedAssets AS (
                                               SELECT 
                                                   pai.CompetitionParticipantId,
                                                   ar.AssetId,
                                                   ar.AssetTypeId,
                                                   pai.AssetReference
                                               FROM ParticipantAssetInput pai
                                               INNER JOIN AssetRegisters ar ON ar.AssetReference = pai.AssetReference
                                           )
                                           INSERT INTO ResultCompetitionAssets (CompetitionParticipantId, AssetId, AssetType)
                                           SELECT 
                                               ra.CompetitionParticipantId,
                                               ra.AssetId,
                                               ra.AssetTypeId
                                           FROM ResolvedAssets ra
                                           LEFT JOIN ResultCompetitionAssets existing ON 
                                               existing.CompetitionParticipantId = ra.CompetitionParticipantId 
                                               AND existing.AssetId = ra.AssetId
                                           WHERE existing.CompetitionAssetId IS NULL
                                           """;

        var insertParams = new DynamicParameters();
        for (var i = 0; i < assetData.Count; i++)
        {
            insertParams.Add($"CompetitionParticipantId{i}", assetData[i].CompetitionParticipantId);
            insertParams.Add($"AssetReference{i}", assetData[i].AssetReference);
        }

        try
        {
            await writeRepository.ExecuteAsync(bulkInsertWithResolutionSql, cancellationToken, insertParams,
                transaction, QueryType.Text);
        }
        catch (Exception ex)
        {
            throw new CustomValidationException($"Failed to insert competition assets: {ex.Message}");
        }
    }
}