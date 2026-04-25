using System.Data;
using Dapper;
using ExcelDataReader;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.AspNetCore.Http;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UploadResultFileCommands;

public class UploadResultFileHelper
{
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
                                                      UploadedAt,
                                                      PreviousUploadedFileId
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
                                                      GETUTCDATE(),
                                                      @PreviousUploadedFileId
                                                  );
                                                  SET @InsertedId = SCOPE_IDENTITY();
                                              END
                                              """;

    // public static async Task<(List<string>, List<string>)> GetFileHeadersAsync(string fileType, IFormFile formFile,
    //     CancellationToken cancellationToken)
    // {
    //     await using var memoryStream = new MemoryStream();
    //     await formFile.CopyToAsync(memoryStream, cancellationToken);
    //     memoryStream.Position = 0;
    //
    //     List<string> headers = [];
    //     List<string> secondSheetHeaders = [];
    //     if (fileType is ".xls" or ".xlsx")
    //     {
    //         System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    //
    //         using var excelReader = ExcelReaderFactory.CreateReader(memoryStream);
    //
    //         var result = excelReader.AsDataSet();
    //         var table = result.Tables[0];
    //
    //         for (var i = 0; i < table.Columns.Count; i++)
    //         {
    //             var headerName = table.Rows[0][i].ToString();
    //             if (string.IsNullOrEmpty(headerName) || string.IsNullOrWhiteSpace(headerName))
    //             {
    //                 throw new InvalidOperationException(
    //                     $"Column {i + 1} header is missing. Please ensure all columns have headers.");
    //             }
    //
    //             headers.Add(headerName.Trim() ?? $"Column{i}");
    //         }
    //
    //         if (headers.Count == 0)
    //         {
    //             throw new InvalidOperationException(
    //                 "No headers found in the file. Please ensure the file has a header row.");
    //         }
    //
    //         var secondTable = result.Tables.Count > 1 ? result.Tables[1] : null;
    //         if (secondTable is { Rows.Count: > 1 })
    //         {
    //             for (var i = 0; i < secondTable.Columns.Count; i++)
    //             {
    //                 var header = secondTable.Rows[0][i].ToString();
    //                 if (string.IsNullOrEmpty(header) || string.IsNullOrWhiteSpace(header))
    //                 {
    //                     throw new InvalidOperationException(
    //                         $"Column {i + 1} header is missing in the second sheet. Please ensure all columns have headers.");
    //                 }
    //
    //                 secondSheetHeaders.Add(header.Trim());
    //             }
    //
    //             if (secondSheetHeaders.Count == 0)
    //             {
    //                 throw new InvalidOperationException(
    //                     "No headers found in the second sheet. Please ensure the sheet has a header row.");
    //             }
    //         }
    //     }
    //     else
    //     {
    //         throw new InvalidOperationException("Unsupported file format. Only .xls, and .xlsx are supported.");
    //     }
    //
    //     if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
    //     {
    //         throw new InvalidOperationException(
    //             $"Duplicate headers found in the file: {string.Join(", ", headers)}. Each column header must be unique.");
    //     }
    //
    //     if (secondSheetHeaders.Distinct(StringComparer.OrdinalIgnoreCase).Count() != secondSheetHeaders.Count)
    //     {
    //         throw new InvalidOperationException(
    //             $"Duplicate headers found in the second sheet: {string.Join(", ", secondSheetHeaders)}. Each column header must be unique.");
    //     }
    //
    //     return (headers, secondSheetHeaders);
    // }

    public static async Task<(List<string>, List<string>)> GetFileHeadersAsync(string fileType, IFormFile formFile,
        CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        List<string> headers = [];
        List<string> secondSheetHeaders = [];

        if (fileType is ".xls" or ".xlsx")
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var excelReader = ExcelReaderFactory.CreateReader(memoryStream);

            if (excelReader.Read())
            {
                for (int i = 0; i < excelReader.FieldCount; i++)
                {
                    var headerValue =
                        excelReader.GetValue(i)
                            ?.ToString(); //Fahim - Don't remove this ? operator this is needed here to avoid null reference exception but the compiler is not able to identify that

                    if (string.IsNullOrWhiteSpace(headerValue))
                    {
                        throw new InvalidOperationException(
                            $"Column {i + 1} header is missing. Please ensure all columns have headers.");
                    }

                    var trimmedHeader = headerValue.Trim();
                    headers.Add(trimmedHeader);
                }

                if (headers.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No headers found in the file. Please ensure the file has a header row.");
                }

                if (headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
                {
                    throw new InvalidOperationException(
                        $"Duplicate headers found in the file: {string.Join(", ", headers)}. Each column header must be unique.");
                }

                if (!excelReader.NextResult() || !excelReader.Read()) return (headers, secondSheetHeaders);
                {
                    for (int i = 0; i < excelReader.FieldCount; i++)
                    {
                        var headerValue = excelReader.GetValue(i)?.ToString(); //Fahim - Don't remove this ? operator this is needed here to avoid null reference exception but the compiler is not able to identify that

                        if (string.IsNullOrWhiteSpace(headerValue))
                        {
                            throw new InvalidOperationException(
                                $"Column {i + 1} header is missing in the second sheet. Please ensure all columns have headers.");
                        }

                        var trimmedHeader = headerValue.Trim();
                        secondSheetHeaders.Add(trimmedHeader);
                    }

                    if (secondSheetHeaders.Count > 0)
                    {
                        if (secondSheetHeaders.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                            secondSheetHeaders.Count)
                        {
                            throw new InvalidOperationException(
                                $"Duplicate headers found in the second sheet: {string.Join(", ", secondSheetHeaders)}. Each column header must be unique.");
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("No data found in the file. Please ensure the file contains data.");
            }
        }
        else
        {
            throw new InvalidOperationException("Unsupported file format. Only .xls and .xlsx are supported.");
        }

        return (headers, secondSheetHeaders);
    }


    public static async Task<int> SaveUploadedFileAsync(UploadResultFileCommand request,
        IWriteRepository<object> repo, int userId, string uploadedBy, string location, IDbTransaction transaction,
        string fileType, CancellationToken cancellationToken)
    {
        string notes = uploadedBy + " has uploaded " + request.FileDto.File.FileName + " file at " +
                       DateTime.UtcNow;

        var syncParams = new DynamicParameters();
        syncParams.Add("FileType", fileType.ToLower());
        syncParams.Add("OwnerId", request.FileDto.OwnerId);
        syncParams.Add("DisciplineId", request.FileDto.DisciplineId);
        syncParams.Add("EventId", request.FileDto.EventId);
        syncParams.Add("BlobLocation", location);
        syncParams.Add("FileCategory", "Result Upload");
        syncParams.Add("UpdatedBy", userId);
        syncParams.Add("FileName", Path.GetFileNameWithoutExtension(request.FileDto.File.FileName));
        syncParams.Add("Notes", notes);
        syncParams.Add("IsFinal", false);
        syncParams.Add("IsDeleted", false);
        syncParams.Add("PreviousUploadedFileId", request.FileDto.PreviousUploadedFileId ?? 0);
        syncParams.Add("InsertedId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await repo.ExecuteAsync(InsertUploadedFile, cancellationToken, syncParams, transaction,
            QueryType.Text);
        return syncParams.Get<int>("InsertedId");
    }

    internal static async Task CheckIsResultFileAlreadyUploaded(IReadRepositoryFactory readRepositoryFactory,
        int disciplineId, int eventId,
        IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                SELECT COUNT(1) 
                                FROM ResultUploadedFile F
                                INNER JOIN ResultCompetition RC ON F.UploadedFileId = RC.UploadedFileId
                                WHERE F.EventId = @EventId
                                	AND F.IsDeleted = 0 
                                	AND F.DisciplineId = @DisciplineId
                                """;

        var parameters = new DynamicParameters();
        parameters.Add("@DisciplineId", disciplineId, DbType.Int32);
        parameters.Add("@EventId", eventId, DbType.Int32);

        var existingFileCount = await readRepositoryFactory.GetLazyRepository<object>().Value.GetSingleAsync<int>(
            sqlQuery, parameters,
            transaction, cancellationToken, QueryType.Text);

        if (existingFileCount > 0)
        {
            throw new CustomValidationException(
                "A result file for this discipline and event has already been uploaded and processed. Please verify the uploaded files or contact support for assistance.");
        }
    }
}