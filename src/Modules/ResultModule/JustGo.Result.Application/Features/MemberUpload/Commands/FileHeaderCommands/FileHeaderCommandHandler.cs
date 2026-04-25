using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using CsvHelper;
using Dapper;
using ExcelDataReader;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Globalization;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.FileHeaderCommands
{
    public class FileHeaderCommandHandler : IRequestHandler<FileHeaderCommand, Result<FileHeaderResponseDto>>
    {
        private readonly IWriteRepositoryFactory _writeRepoFactory;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IAzureBlobFileService _azureBlobFileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly IMediator _mediator;

        public FileHeaderCommandHandler(IWriteRepositoryFactory writeRepoFactory, IReadRepositoryFactory readRepository,
            IAzureBlobFileService azureBlobFileService, IUnitOfWork unitOfWork, IUtilityService utilityService,
            IMediator mediator)
        {
            _writeRepoFactory = writeRepoFactory;
            _readRepository = readRepository;
            _azureBlobFileService = azureBlobFileService;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _mediator = mediator;
        }

        private const string InsertUploadedFile = """
                                                  BEGIN
                                                      INSERT INTO ResultUploadedFile (
                                                          FileType,
                                                          OwnerId,
                                                          DisciplineId,
                                                          EventId,
                                                          FileCategory,
                                                          UpdatedBy,
                                                          FileName,
                                                          Notes,
                                                          IsFinal,
                                                          IsDeleted,
                                                          BlobLocation,
                                                          UploadedAt
                                                      )
                                                      VALUES (
                                                          @FileType,
                                                          @OwnerId,
                                                          @DisciplineId,
                                                          @EventId,
                                                          @FileCategory,
                                                          @UpdatedBy,
                                                          @FileName,
                                                          @Notes,
                                                          @IsFinal,
                                                          @IsDeleted,
                                                          @BlobLocation,
                                                          GETUTCDATE()
                                                      );
                                                      SET @InsertedId = SCOPE_IDENTITY();
                                                  END
                                                  """;

        public async Task<Result<FileHeaderResponseDto>> Handle(FileHeaderCommand request,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var formFile = request.FileDto.File;
            var fileType = Path.GetExtension(formFile.FileName).ToLower();
            var currentUserGuid = _utilityService.GetCurrentUserGuid();
            var user = await _mediator.Send(new GetUserByUserSyncIdQuery(currentUserGuid), cancellationToken);
            var repo = _writeRepoFactory.GetLazyRepository<object>().Value;
            var path =
                $"{Path.GetFileNameWithoutExtension(formFile.FileName)}_{user.MemberId}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(formFile.FileName).ToLower()}";
            try
            {
                if (!IsValidFileType(fileType))
                {
                    return Result<FileHeaderResponseDto>.Failure(
                        "Unsupported file format.", ErrorType.BadRequest);
                }

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream, cancellationToken);
                    fileBytes = memoryStream.ToArray();
                }

                var destBlobPath =
                    await _azureBlobFileService.MapPath(
                        $"~/store/result_attachments/{path}");
                await _azureBlobFileService.UploadFileAsync(destBlobPath, fileBytes, FileMode.Create,
                    cancellationToken);
                var fileId = await SaveUploadedFileAsync(request, repo, user.Userid, user.MemberId, destBlobPath,
                    transaction,
                    cancellationToken);
                var headers = await GetFileHeadersAsync(request.FileDto.File, cancellationToken);

                var preDefinedHeaders = await GetFieldsByScopeId(request.FileDto.DisciplineId, cancellationToken);
                await _unitOfWork.CommitAsync(transaction);

                return new FileHeaderResponseDto
                {
                    FileId = fileId,
                    FileHeaders = headers,
                    PredefinedHeaders = preDefinedHeaders
                };
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

        private static bool IsValidFileType(string fileType)
        {
            var baseTypes = new[] { ".csv", ".xlsx", ".xls" };
            return baseTypes.Contains(fileType);
        }

        private async Task<int> SaveUploadedFileAsync(FileHeaderCommand request,
            IWriteRepository<object> repo, int userId, string uploadedBy, string location, IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            string notes = uploadedBy + " has uploaded " + request.FileDto.File.FileName + " file at " +
                           DateTime.UtcNow;
            var syncParams = new DynamicParameters();
            syncParams.Add("FileType", Path.GetExtension(request.FileDto.File.FileName).ToLower());
            syncParams.Add("OwnerId", request.OwnerId);
            syncParams.Add("DisciplineId", request.FileDto.DisciplineId);
            syncParams.Add("EventId", null);
            syncParams.Add("BlobLocation", location);
            syncParams.Add("FileCategory", "Member Upload");
            syncParams.Add("UpdatedBy", userId);
            syncParams.Add("FileName", Path.GetFileNameWithoutExtension(request.FileDto.File.FileName));
            syncParams.Add("Notes", notes);
            syncParams.Add("IsFinal", false);
            syncParams.Add("IsDeleted", false);
            syncParams.Add("InsertedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await repo.ExecuteAsync(InsertUploadedFile, cancellationToken, syncParams, transaction, QueryType.Text);
            return syncParams.Get<int>("InsertedId");
        }

        private async Task<List<string>> GetFileHeadersAsync(IFormFile formFile, CancellationToken cancellationToken)
        {
            var fileType = Path.GetExtension(formFile.FileName).ToLower();
            using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            List<string> headers = [];
            switch (fileType)
            {
                case ".csv":
                {
                    using var reader = new StreamReader(memoryStream);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    await csv.ReadAsync();
                    csv.ReadHeader();

                    if (csv.HeaderRecord != null)
                    {
                        int lastValidHeaderIndex = -1;
                        for (int i = csv.HeaderRecord.Length - 1; i >= 0; i--)
                        {
                            if (!string.IsNullOrWhiteSpace(csv.HeaderRecord[i]))
                            {
                                lastValidHeaderIndex = i;
                                break;
                            }
                        }
                        
                        for (int idx = 0; idx <= lastValidHeaderIndex; idx++)
                        {
                            var header = csv.HeaderRecord[idx];
                            if (string.IsNullOrWhiteSpace(header))
                            {
                                throw new InvalidOperationException(
                                    $"Column {idx + 1} header is missing in the CSV file. Please ensure all columns have headers.");
                            }
                        }
        
                        // Only take headers up to the last valid one
                        headers = csv.HeaderRecord.Take(lastValidHeaderIndex + 1).ToList();
                    }

                    break;
                }
                case ".xls" or ".xlsx":
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using var excelReader = ExcelReaderFactory.CreateReader(memoryStream);
                    var result = excelReader.AsDataSet();
                    var table = result.Tables[0];
                    headers = new List<string>();
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var headerValue = table.Rows[0][i].ToString();
                        if (string.IsNullOrWhiteSpace(headerValue) || string.IsNullOrEmpty(headerValue))
                        {
                            throw new InvalidOperationException(
                                $"Column {i + 1} header is missing in the second sheet. Please ensure all columns have headers.");
                        }

                        headers.Add(headerValue);
                    }

                    break;
                }
                default:
                    throw new InvalidOperationException(
                        "The uploaded file format is not supported. Please upload a file with one of the following extensions: .csv, .xls, or .xlsx.");
            }

            if (headers.Count == 0)
            {
                throw new InvalidOperationException(
                    "No headers were found in the uploaded file. Please ensure the file contains a valid header row and try again.");
            }

            if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
            {
                throw new InvalidOperationException(
                    "The file contains duplicate column headers. Please ensure each column header is unique to proceed.");
            }

            return headers;
        }

        private async Task<List<ResultUploadFieldMapping>> GetFieldsByScopeId(int validationScopeId,
            CancellationToken cancellationToken)
        {
            var sqlQuery = """
                               select f.ColumnName,f.IsOptional,f.SampleData, f.ColumnIdentifier
                               from [ResultUploadFieldMapping] fm
                               inner join [ResultUploadFields] f on f.ResultUploadFieldId = fm.ResultUploadFieldId
                               where fm.ValidationScopeId = @validationScopeId
                           """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("validationScopeId", validationScopeId);
            var repo = _readRepository.GetRepository<ResultUploadFieldMapping>();
            var item = (await repo.GetListAsync(sqlQuery, cancellationToken, queryParameters, null, QueryType.Text))
                .ToList();
            return item;
        }

        //public async Task SaveConfirmHeaderMapping(List<ResultUploadFieldMapping> items, int fileId, CancellationToken cancellationToken, IDbTransaction transaction)
        //{
        //    var mappedHeaders = new List<ResultUploadConfirmHeaderMapping>();
        //    var repo = _writeRepoFactory.GetLazyRepository<object>().Value;
        //    foreach (var item in items)
        //    {
        //        var mappedHeader = new ResultUploadConfirmHeaderMapping
        //        {
        //            UploadedFileId = fileId,
        //            SystemHeaderName = item.ColumnName,
        //            MappedHeaderName = "",
        //            IsMapped = false
        //        };

        //        mappedHeaders.Add(mappedHeader);
        //    }
        //    await repo.ExecuteAsync(InsertConfirmHeaderMappingQuery, cancellationToken, mappedHeaders, transaction, QueryType.Text);
        //}
    }
}