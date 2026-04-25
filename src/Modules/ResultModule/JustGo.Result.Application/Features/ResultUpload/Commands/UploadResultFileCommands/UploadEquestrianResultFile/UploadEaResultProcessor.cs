using System.Data.Common;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;
using JustGo.Result.Domain.Entities;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands.UploadEquestrianResultFile;

public class UploadEaResultProcessor : IUploadResultFileProcessor
{
    private readonly IWriteRepositoryFactory _writeRepoFactory;
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IAzureBlobFileService _azureBlobFileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public UploadEaResultProcessor(IWriteRepositoryFactory writeRepoFactory, IReadRepositoryFactory readRepository,
        IAzureBlobFileService azureBlobFileService, IUnitOfWork unitOfWork, IUtilityService utilityService)
    {
        _writeRepoFactory = writeRepoFactory;
        _readRepository = readRepository;
        _azureBlobFileService = azureBlobFileService;
        _unitOfWork = unitOfWork;
        _utilityService = utilityService;
    }

    public async Task<Result<FileHeaderResponseDto>> ProcessAsync(UploadResultFileCommand request,
        CancellationToken cancellationToken)
    {
        var formFile = request.FileDto.File;
        var fileType = Path.GetExtension(formFile.FileName).ToLower() ?? string.Empty;

        var currentUser = await _utilityService.GetCurrentUser(cancellationToken);

        var repo = _writeRepoFactory.GetLazyRepository<object>().Value;
        var readRepo = _readRepository.GetLazyRepository<object>().Value;

        var path =
            $"{Path.GetFileNameWithoutExtension(formFile.FileName)}_{currentUser.MemberId}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(formFile.FileName).ToLower()}";

        if (!IsValidFileType(fileType))
        {
           return Result<FileHeaderResponseDto>.Failure(
                    "The uploaded file format is not supported. Please upload a file with one of the following extensions: .xlsx, .xls, or .json.", 
                    ErrorType.BadRequest);
        }

        var destBlobPath = await _azureBlobFileService.MapPath($"~/store/result_attachments/{path}");

        await _azureBlobFileService.UploadFileAsync(destBlobPath, request.FileDto.File, cancellationToken);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // if (request.FileDto.PreviousUploadedFileId is null)
            // {
            //     await UploadResultFileHelper.CheckIsResultFileAlreadyUploaded(_readRepository,
            //         request.FileDto.DisciplineId, request.FileDto.EventId, transaction,
            //         cancellationToken);
            // }
            
            var discipline = await GetDisciplineAsync(readRepo, request.FileDto.DisciplineId, transaction,
                cancellationToken);

            var compatibilityResult = ValidateFileTypeAndDisciplineCompatibility(fileType, discipline.Name);

            if (!compatibilityResult.IsSuccess)
            {
                return Result<FileHeaderResponseDto>.Failure(
                    compatibilityResult.Error ?? "The file type and discipline combination is invalid. Ensure the file format matches the discipline requirements.",
                    compatibilityResult.ErrorType ?? ErrorType.BadGateway);
            }

            var fileId = await UploadResultFileHelper.SaveUploadedFileAsync(request, repo, currentUser.UserId,
                currentUser.MemberId,
                destBlobPath,
                transaction,
                fileType,
                cancellationToken);

            if (fileType.Equals(".json"))
            {
                await transaction.CommitAsync(cancellationToken);
                return new FileHeaderResponseDto
                {
                    FileId = fileId
                };
            }

            var (headers, secondSheetHeaders) = await UploadResultFileHelper.GetFileHeadersAsync(
                fileType, request.FileDto.File, cancellationToken);

            var preDefinedHeaders = await GetFieldsByScopeId(request.FileDto.DisciplineId, 
                1, cancellationToken);

            List<ResultUploadFieldMapping> preDefinedSecondSheetHeaders = [];
            if (secondSheetHeaders.Count > 0)
            {
                preDefinedSecondSheetHeaders =
                    await GetFieldsByScopeId(request.FileDto.DisciplineId, 2, cancellationToken);   
            }

            await transaction.CommitAsync(cancellationToken);

            return new FileHeaderResponseDto
            {
                FileId = fileId,
                FileHeaders = headers,
                SecondSheetHeaders = secondSheetHeaders,
                PredefinedHeaders = preDefinedHeaders,
                SecondSheetPredefinedHeaders = preDefinedSecondSheetHeaders
            };
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            if (string.IsNullOrEmpty(path))
                return Result<FileHeaderResponseDto>.Failure(
                    $" {e.Message}", ErrorType.InternalServerError);
            try
            {
                await _azureBlobFileService.DeleteFileAsync(path, cancellationToken);
            }
            catch (Exception ex)
            {
                return Result<FileHeaderResponseDto>.Failure(
                    $"Failed to delete uploaded file during rollback: {ex.Message}", ErrorType.InternalServerError);
            }

            return Result<FileHeaderResponseDto>.Failure(
                $" {e.Message}", ErrorType.InternalServerError);
        }
    }

    private static bool IsValidFileType(string fileType)
    {
        var baseTypes = new[] { ".xlsx", ".xls", ".json" };
        return baseTypes.Contains(fileType);
    }

    private static readonly HashSet<string> EventingDisciplines = new(StringComparer.OrdinalIgnoreCase)
    {
        "Eventing Competitive",
        "Eventing"
    };

    private static Result<bool> ValidateFileTypeAndDisciplineCompatibility(string fileType, string disciplineName)
    {
        if (string.IsNullOrWhiteSpace(fileType) || string.IsNullOrWhiteSpace(disciplineName))
        {
            return Result<bool>.Failure("File type and discipline name are required for validation.",
                ErrorType.BadGateway);
        }

        var isJsonFile = string.Equals(fileType, ".json", StringComparison.OrdinalIgnoreCase);
        var isEventingDiscipline = EventingDisciplines.Contains(disciplineName);

        return (isJsonFile, isEventingDiscipline) switch
        {
            (true, true) => true,
            (false, false) => true,

            (true, false) => Result<bool>.Failure(
                $"JSON file format is only supported for Eventing disciplines. " +
                $"Current discipline '{disciplineName}' requires Excel format (.xlsx/.xls).", ErrorType.BadGateway),

            (false, true) => Result<bool>.Failure(
                $"Eventing disciplines require JSON file format. " +
                $"Please upload a JSON file instead of '{fileType}' for discipline '{disciplineName}'.",
                ErrorType.BadGateway)
        };
    }

    private async Task<List<ResultUploadFieldMapping>> GetFieldsByScopeId(int validationScopeId, int sheetNumber,
        CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                select f.ColumnName,f.IsOptional,f.SampleData, f.ColumnIdentifier
                                from [ResultUploadFieldMapping] fm
                                inner join [ResultUploadFields] f on f.ResultUploadFieldId = fm.ResultUploadFieldId
                                where fm.ValidationScopeId = @validationScopeId AND SheetNumber = @sheetNumber
                                """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("validationScopeId", validationScopeId);
        queryParameters.Add("sheetNumber", sheetNumber);

        var repo = _readRepository.GetRepository<ResultUploadFieldMapping>();
        var item =
            (await repo.GetListAsync(sqlQuery, cancellationToken, queryParameters, null, QueryType.Text)).ToList();
        return item;
    }

    private static async Task<(string Name, int Id)> GetDisciplineAsync(IReadRepository<object> readRepository,
        int disciplineId,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        var discipline = await readRepository.QueryFirstAsync<(string Name, int Id)>(
            ImportResultHelper.GET_DISCIPLINE_SQL,
            new { DisciplineId = disciplineId },
            transaction, QueryType.Text, cancellationToken);

        if (string.IsNullOrEmpty(discipline.Name))
        {
            throw new CustomValidationException($"Discipline with ID {disciplineId} was not found. Please ensure the provided ID is correct and exists in the system.");
        }

        return discipline;
    }
}