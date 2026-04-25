using System.Data.Common;
using System.Globalization;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadEquestrian;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadGymnastic;

public class ConfirmUploadFileGymnasticProcessor : IConfirmUploadFileProcessor
{
    private readonly IWriteRepositoryFactory _writeRepository;
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    private const int DefaultBatchSize = 1000;
    private const int ResultDataBatchSize = 500;

    public ConfirmUploadFileGymnasticProcessor(IWriteRepositoryFactory writeRepository,
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

            return await ProcessStandardDisciplineWithStreamingAsync(fileMetadata, cancellationToken);
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

        await ProcessClassDataInStreamingBatchesAsync(
            fileMetadata.UploadedFileId, competitionRoundId, classInfo.ClassName,
            classKey, fileMetadata.Discipline.Id, writeRepo, readRepo,
            transaction, cancellationToken);
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

        foreach (var batch in participantBatch)
        {
            batch.ResetProcessingMarker();
        }

        // Step 3: Insert results
        var resultIds = await ConfirmUploadFileHelper.InsertResultsBatchAsync(_readRepository, participantBatch, participantIds,
            disciplineId, writeRepository,
            transaction, cancellationToken);

        foreach (var batch in participantBatch)
        {
            batch.ResetProcessingMarker();
        }

        // Step 4: Insert result data
        await InsertResultDataBatchAsync(_readRepository, participantBatch, resultIds, disciplineId,
            writeRepository, transaction, cancellationToken);
    }
    
    private static async Task InsertResultDataBatchAsync(IReadRepositoryFactory readRepository,
        List<ParticipantBatchData> participantBatch,
        List<(int UserId, int CompetitionResultId, int CompetitionParticipantId)> resultIds,
        int disciplineId,
        IWriteRepository<object> writeRepository,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var resultFields = (await ConfirmUploadFileHelper.GetDisciplineFieldMappingsAsync(
                disciplineId, "ResultCompetitionResultData", readRepository, cancellationToken))
            .Select(x => x.SourceFieldName).ToList();

        if (resultFields.Count == 0) return;

        var insertDataList = new List<(int CompetitionResultId, string Key, string Value, int DataType)>();

        foreach (var (userId, competitionResultId, _) in resultIds)
        {
            var userBatch = participantBatch
                .FirstOrDefault(p =>
                    p.UserId == userId && !p.IsProcessed);

            if (userBatch?.ParsedMemberData == null) continue;
            userBatch.MarkAsProcessed();

            foreach (var (key, value) in userBatch.ParsedMemberData)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                if (!resultFields.Contains(key, StringComparer.OrdinalIgnoreCase)) continue;

                var val = value.Trim();
                var dType = ConfirmUploadFileHelper.DetermineDataType(val);
                insertDataList.Add((competitionResultId, key, val, dType));
            }
        }

        if (insertDataList.Count == 0) return;

        foreach (var batch in insertDataList.Chunk(ResultDataBatchSize))
        {
            await ConfirmUploadFileHelper.ProcessResultDataBatch(batch.ToList(), writeRepository, transaction, cancellationToken);
        }
    }
}