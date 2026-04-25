using System.Globalization;
using CsvHelper;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DownloadMemberDataCommands
{
    public class DownloadMemberDataCommandHandler : IRequestHandler<DownloadMemberDataCommand, Result<string>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IAzureBlobFileService _blobFileService;
        private const string SqlQuery = """
                                        DECLARE @MemberIdKey        NVARCHAR(100) = 'Member ID';
                                        DECLARE @MemberFirstNameKey NVARCHAR(100) = 'First Name';
                                        DECLARE @MemberLastNameKey  NVARCHAR(100) = 'Last Name';
                                        DECLARE @AssetIdKey         NVARCHAR(100) = 'Horse ID';
                                        DECLARE @AssetNameKey       NVARCHAR(100) = 'Horse Name';
                                        
                                        DECLARE @priorityCols NVARCHAR(MAX) = N'';
                                        DECLARE @restCols     NVARCHAR(MAX) = N'';
                                        DECLARE @cols         NVARCHAR(MAX);
                                        DECLARE @sql          NVARCHAR(MAX);
                                        
                                        ;WITH FilteredData AS (
                                            SELECT
                                                uf.UploadedFileId,
                                                uf.FileName,
                                                um.ErrorType AS [Validation Status],
                                                um.ErrorMessage AS [Error Reason],
                                                umd.UploadedMemberDataId,
                                                umd.UploadedMemberId,
                                                umd.MemberData
                                            FROM ResultUploadedFile uf WITH (NOLOCK)
                                            INNER JOIN ResultUploadedMember um WITH (NOLOCK) 
                                                ON uf.UploadedFileId = um.UploadedFileId 
                                                AND um.IsDeleted = 0
                                            INNER JOIN ResultUploadedMemberData umd WITH (NOLOCK) 
                                                ON um.UploadedMemberId = umd.UploadedMemberId
                                            WHERE uf.UploadedFileId = @FileId 
                                                AND uf.IsDeleted = 0
                                        ),
                                        Parsed AS (
                                            SELECT
                                                fd.UploadedFileId,
                                                fd.UploadedMemberDataId,
                                                j.[key],
                                                ROW_NUMBER() OVER (
                                                    PARTITION BY fd.UploadedMemberDataId
                                                    ORDER BY (SELECT NULL)
                                                ) AS seq_in_row
                                            FROM FilteredData fd
                                            CROSS APPLY OPENJSON(fd.MemberData) j
                                        ),
                                        FirstOccurrence AS (
                                            SELECT
                                                [key],
                                                MIN(seq_in_row) AS first_seq
                                            FROM Parsed
                                            GROUP BY [key]
                                        )
                                        SELECT @restCols = STRING_AGG(QUOTENAME([key]) + ' NVARCHAR(MAX)', ', ') 
                                            WITHIN GROUP (ORDER BY first_seq)
                                        FROM FirstOccurrence
                                        WHERE [key] NOT IN (
                                            ISNULL(NULLIF(@MemberIdKey,''),        '#NA#'),
                                            ISNULL(NULLIF(@MemberFirstNameKey,''), '#NA#'),
                                            ISNULL(NULLIF(@MemberLastNameKey,''),  '#NA#'),
                                            ISNULL(NULLIF(@AssetIdKey,''),         '#NA#'),
                                            ISNULL(NULLIF(@AssetNameKey,''),       '#NA#')
                                        );
                                        
                                        SET @priorityCols = CONCAT(
                                            CASE WHEN @MemberIdKey <> '' THEN QUOTENAME(@MemberIdKey) + ' NVARCHAR(MAX)' ELSE '' END,
                                            CASE WHEN @MemberFirstNameKey <> '' AND @MemberIdKey <> '' THEN ', ' ELSE '' END,
                                            CASE WHEN @MemberFirstNameKey <> '' THEN QUOTENAME(@MemberFirstNameKey) + ' NVARCHAR(MAX)' ELSE '' END,
                                            CASE WHEN @MemberLastNameKey <> '' AND (@MemberIdKey <> '' OR @MemberFirstNameKey <> '') THEN ', ' ELSE '' END,
                                            CASE WHEN @MemberLastNameKey <> '' THEN QUOTENAME(@MemberLastNameKey) + ' NVARCHAR(MAX)' ELSE '' END,
                                            CASE WHEN @AssetIdKey <> '' AND (@MemberIdKey <> '' OR @MemberFirstNameKey <> '' OR @MemberLastNameKey <> '') THEN ', ' ELSE '' END,
                                            CASE WHEN @AssetIdKey <> '' THEN QUOTENAME(@AssetIdKey) + ' NVARCHAR(MAX)' ELSE '' END,
                                            CASE WHEN @AssetNameKey <> '' AND (@MemberIdKey <> '' OR @MemberFirstNameKey <> '' OR @MemberLastNameKey <> '' OR @AssetIdKey <> '') THEN ', ' ELSE '' END,
                                            CASE WHEN @AssetNameKey <> '' THEN QUOTENAME(@AssetNameKey) + ' NVARCHAR(MAX)' ELSE '' END
                                        );
                                        
                                        SET @cols = CASE 
                                            WHEN @priorityCols <> '' AND @restCols <> '' THEN @priorityCols + ', ' + @restCols
                                            WHEN @priorityCols <> '' THEN @priorityCols
                                            ELSE @restCols
                                        END;
                                        
                                        IF (@cols IS NULL OR LTRIM(RTRIM(@cols)) = '')
                                        BEGIN
                                            RAISERROR('No JSON keys discovered for file %d.', 10, 1, @FileId);
                                            RETURN;
                                        END;
                                        
                                        SET @sql = N'
                                        WITH FilteredData AS (
                                            SELECT
                                                uf.FileName,
                                                um.ErrorType AS [Validation Status],
                                                um.ErrorMessage AS [Error Reason],
                                                umd.UploadedMemberDataId,
                                                umd.UploadedMemberId,
                                                umd.MemberData
                                            FROM ResultUploadedFile uf WITH (NOLOCK)
                                            INNER JOIN ResultUploadedMember um WITH (NOLOCK) 
                                                ON uf.UploadedFileId = um.UploadedFileId 
                                                AND um.IsDeleted = 0
                                            INNER JOIN ResultUploadedMemberData umd WITH (NOLOCK) 
                                                ON um.UploadedMemberId = umd.UploadedMemberId
                                            WHERE uf.UploadedFileId = @FileId 
                                                AND uf.IsDeleted = 0
                                        )
                                        SELECT
                                            fd.FileName,
                                            fd.[Validation Status],
                                            fd.[Error Reason],
                                            fd.UploadedMemberDataId,
                                            fd.UploadedMemberId,
                                            j.*
                                        FROM FilteredData fd
                                        CROSS APPLY OPENJSON(fd.MemberData)
                                        WITH (' + @cols + N') AS j
                                        ORDER BY fd.UploadedMemberDataId DESC;';
                                        
                                        EXEC sp_executesql @sql, N'@FileId INT', @FileId=@FileId;
                                        """;
        public DownloadMemberDataCommandHandler(IReadRepositoryFactory readRepository, IAzureBlobFileService blobFileService)
        {
            _readRepository = readRepository;
            _blobFileService = blobFileService;
        }

        public async Task<Result<string>> Handle(DownloadMemberDataCommand request, CancellationToken cancellationToken = default)
        {
            var data = await GetMemberDataDto(request.FileId, cancellationToken);
            var newFileName = $"{Guid.NewGuid():N}.csv";
            
            var destinationPath = await _blobFileService.MapPath($"~/store/result_attachments/{newFileName}");
            await using var memory = new MemoryStream();
            await using var writer = new StreamWriter(memory);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            var firstRow = data.FirstOrDefault();
            if (firstRow != null)
            {
                foreach (var key in firstRow.Data.Keys)
                    csv.WriteField(key);

                await csv.NextRecordAsync();
            }
            foreach (var row in data)
            {
                foreach (var key in row.Data.Keys)
                    csv.WriteField(row.Data[key]);
                await csv.NextRecordAsync();
            }
            await writer.FlushAsync(cancellationToken);
            memory.Position = 0;
            var fileBytes = memory.ToArray();
            var url = await _blobFileService.UploadFileAsync(destinationPath, fileBytes, FileMode.Create, cancellationToken);
            var downloadUrl = $"/store/download?f={newFileName}&t=resultattachment&p=-1&p1=-1&p2=-1&p3=-1";
            return downloadUrl;
        }
        public async Task<List<DynamicMemberDataDto>> GetMemberDataDto(int fileId, CancellationToken cancellationToken = default)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("FileId", fileId);
            var result = new List<DynamicMemberDataDto>();
            var repo = _readRepository.GetRepository<dynamic>();
            var rows = await repo.GetListAsync(SqlQuery, cancellationToken, queryParameters, null, QueryType.Text);

            foreach (var dict in rows.OfType<IDictionary<string, object>>())
            {
                var fileName = dict.TryGetValue("FileName", out var fileNameValue) ? fileNameValue?.ToString() ?? string.Empty : string.Empty;

                var dto = new DynamicMemberDataDto
                {
                    FileName = fileName,
                    Data = dict
                        .Where(kvp => kvp.Key != "FileName" && kvp.Key != "UploadedMemberDataId" && kvp.Key != "UploadedMemberId")
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value ?? string.Empty
                        )
                };

                result.Add(dto);
            }


            return result;

        }

    }
}
