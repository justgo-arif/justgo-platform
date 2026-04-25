using System.Data;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Domain.Entities;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands.UploadTableTennisResultFile;

public class UploadTtResultProcessor : IUploadResultFileProcessor
{
    private readonly IWriteRepositoryFactory _writeRepoFactory;
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IAzureBlobFileService _azureBlobFileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUtilityService _utilityService;

    public UploadTtResultProcessor(IWriteRepositoryFactory writeRepoFactory, IReadRepositoryFactory readRepository,
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
        var fileType = Path.GetExtension(formFile.FileName).ToLower();

        var currentUser = await _utilityService.GetCurrentUser(cancellationToken);

        var repo = _writeRepoFactory.GetLazyRepository<object>().Value;
        var path =
            $"{Path.GetFileNameWithoutExtension(formFile.FileName)}_{currentUser.MemberId}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(formFile.FileName).ToLower()}";

        if (!IsValidFileType(fileType))
        {
            return Result<FileHeaderResponseDto>.Failure(
                "The uploaded file format is not supported. Please upload a file with a valid format, such as .xlsx or .xls.",
                ErrorType.BadRequest);
        }

        var destBlobPath = await _azureBlobFileService.MapPath(
            $"~/store/result_attachments/{path}");

        await _azureBlobFileService.UploadFileAsync(destBlobPath, request.FileDto.File, cancellationToken);
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (request.FileDto.PreviousUploadedFileId is null)
            {
                await UploadResultFileHelper.CheckIsResultFileAlreadyUploaded(_readRepository,
                    request.FileDto.DisciplineId, request.FileDto.EventId, transaction,
                    cancellationToken);
            }

            var fileId = await UploadResultFileHelper.SaveUploadedFileAsync(request, repo, currentUser.UserId,
                currentUser.MemberId,
                destBlobPath,
                transaction,
                fileType,
                cancellationToken);

            var (headers, secondSheetHeaders) = await UploadResultFileHelper.GetFileHeadersAsync(fileType,
                request.FileDto.File,
                cancellationToken);

            var preDefinedHeaders = await GetFieldsByScopeId(request.FileDto.DisciplineId, 1, cancellationToken);

            List<ResultUploadFieldMapping> preDefinedSecondSheetHeaders = [];
            if (secondSheetHeaders.Count > 0)
            {
                if (!secondSheetHeaders.Contains("Membership#", StringComparer.OrdinalIgnoreCase) ||
                    !secondSheetHeaders.Contains("Est Rating", StringComparer.OrdinalIgnoreCase))
                {
                    throw new CustomValidationException(
                        "The second sheet must include the columns 'Membership#' and 'Est Rating' to proceed. Please ensure these columns are present and correctly labeled.");
                }

                preDefinedSecondSheetHeaders = await GetFieldsByScopeId(request.FileDto.DisciplineId, 2,
                    cancellationToken);
            }

            await _unitOfWork.CommitAsync(transaction);

            return new FileHeaderResponseDto
            {
                FileId = fileId,
                FileHeaders = headers,
                SecondSheetHeaders = secondSheetHeaders,
                PredefinedHeaders = preDefinedHeaders,
                SecondSheetPredefinedHeaders = preDefinedSecondSheetHeaders
            };
        }
        catch (CustomValidationException ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<FileHeaderResponseDto>.Failure(
                $" {ex.Message}", ErrorType.BadRequest);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancellationToken);
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

    private static List<string> GetNormalizedHeaders(List<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i].Equals("Winner Membership#", StringComparison.OrdinalIgnoreCase))
            {
                headers[i] = "Winner";
            }
            else if (headers[i].Equals("Loser Membership#", StringComparison.OrdinalIgnoreCase))
            {
                headers[i] = "Loser";
            }
        }

        return headers;
    }

    private static bool IsValidFileType(string fileType)
    {
        var baseTypes = new[] { ".xlsx", ".xls" };
        return baseTypes.Contains(fileType);
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
}