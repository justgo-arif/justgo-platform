using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetFileInformation
{
    public class GetFileInformationQueryHandler : IRequestHandler<GetFileInformationQuery, KeysetPagedResult<FileInformationDto>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        
        public GetFileInformationQueryHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<KeysetPagedResult<FileInformationDto>> Handle(GetFileInformationQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;
            
            var archiveFilter = request.IsArchivedIncluded 
                ? "AND FS.ResultUploadedFileStatusId != 2 AND FS.ResultUploadedFileStatusId IS NOT NULL" 
                : "AND FS.ResultUploadedFileStatusId NOT IN (2,5) AND FS.ResultUploadedFileStatusId IS NOT NULL";
            
            var sqlQuery = $"""
                            ;WITH FileData AS (
                            SELECT 
                            uf.[UploadedFileId] as FileId,
                            uf.FileType,
                            CONCAT(u.[FirstName], ' ', u.[LastName]) AS UploadedBy,
                            uf.UploadedAt,
                            uf.DisciplineId,
                            vs.ValidationScopeName as 'DisciplineName',
                            uf.[FileName],
                            uf.Notes,
                            NULL AS UploadedByImage,
                            FS.[Status],
                            COUNT_BIG(um.UploadedMemberId) AS RecordCount,
                            SUM(CASE WHEN um.IsValidated = 0 THEN 1 ELSE 0 END) AS ErrorCount,
                            COUNT(1) OVER() AS TotalCount
                            FROM ResultUploadedFile uf
                            INNER JOIN ValidationScopes vs on vs.ValidationScopeId = uf.DisciplineId
                            LEFT JOIN ResultUploadedFileStatus FS ON uf.FileStatusId = FS.ResultUploadedFileStatusId
                            LEFT JOIN ResultUploadedMember um 
                            ON uf.[UploadedFileId] = um.UploadedFileId  AND um.IsDeleted = 0
                            INNER JOIN [User] u 
                            ON uf.UpdatedBy = u.UserId
                            WHERE uf.IsDeleted = 0 
                            AND uf.OwnerId = @OwnerId
                            AND uf.EventId is null {archiveFilter}
                            AND (
                            @Search = '' 
                            OR EXISTS (
                            SELECT 1
                            FROM (VALUES (uf.FileName)) AS v(val)
                            WHERE v.val LIKE '%' + @Search + '%'
                            )
                            )
                            GROUP BY 
                            uf.[UploadedFileId], uf.FileType, uf.UpdatedBy, uf.UploadedAt, uf.DisciplineId,vs.ValidationScopeName,
                            uf.FileName, uf.Notes, FS.[Status],
                            CONCAT(u.[FirstName], ' ', u.[LastName])
                            )
                            SELECT
                            FileId,
                            FileType,
                            UploadedBy,
                            UploadedAt,
                            DisciplineId,
                            DisciplineName,
                            FileName,
                            Notes,
                            UploadedByImage,
                            [Status],
                            RecordCount AS Records,
                            ErrorCount AS Errors,
                            CASE 
                            WHEN RecordCount > 0 THEN CAST(ErrorCount AS decimal(18,2)) * 100.0 / RecordCount
                            ELSE 0
                            END AS ErrorPercentage,
                            CASE 
                            WHEN RecordCount > 0 THEN (RecordCount - ErrorCount) * 100.0 / RecordCount
                            ELSE 0
                            END AS SuccessPercentage,
                            TotalCount
                            FROM FileData
                            ORDER BY 
                            CASE 
                            WHEN @SortBy = 'file name' AND @OrderBy = 'ASC' THEN [FileName]
                            WHEN @SortBy = 'uploaded on' AND @OrderBy = 'ASC' THEN CAST(UploadedAt AS NVARCHAR(100))
                            WHEN @SortBy = 'updated by' AND @OrderBy = 'ASC' THEN UploadedBy
                            WHEN @SortBy = 'records' AND @OrderBy = 'ASC' THEN CAST(RecordCount AS NVARCHAR(100))
                            END ASC,
                            CASE 
                            WHEN @SortBy = 'file name' AND @OrderBy = 'DESC' THEN [FileName]
                            WHEN @SortBy = 'uploaded on' AND @OrderBy = 'DESC' THEN CAST(UploadedAt AS NVARCHAR(100))
                            WHEN @SortBy = 'updated by' AND @OrderBy = 'DESC' THEN UploadedBy
                            WHEN @SortBy = 'records' AND @OrderBy = 'DESC' THEN CAST(RecordCount AS NVARCHAR(100))
                            END DESC,
                            CASE 
                            WHEN @SortBy = 'uploaded on' AND @OrderBy = 'ASC' THEN UploadedAt
                            WHEN @SortBy = 'records' AND @OrderBy = 'ASC' THEN RecordCount
                            END ASC,
                            CASE 
                            WHEN @SortBy = 'uploaded on' AND @OrderBy = 'DESC' THEN UploadedAt
                            WHEN @SortBy = 'records' AND @OrderBy = 'DESC' THEN RecordCount
                            END DESC,
                            CASE 
                            WHEN @SortBy = '' OR @SortBy IS NULL THEN FileId
                            END DESC
                            OFFSET (@PageNumber - 1) * @PageSize ROWS
                            FETCH NEXT @PageSize ROWS ONLY;
                            """;
            
            var queryParameters = new DynamicParameters();
            queryParameters.Add("PageNumber", pageNumber);
            queryParameters.Add("PageSize", pageSize);
            queryParameters.Add("Search", request.Search ?? string.Empty);
            queryParameters.Add("SortBy", request.SortBy ?? string.Empty);
            queryParameters.Add("OrderBy", request.OrderBy ?? "DESC");
            queryParameters.Add("OwnerId", request.OwnerId);


            var repo = _readRepository.GetRepository<FileInformationDto>();
            var items = (await repo.GetListAsync(sqlQuery, cancellationToken, queryParameters, commandType: QueryType.Text)).ToList();

            return new KeysetPagedResult<FileInformationDto>
            {
                Items = items,
                TotalCount = items.FirstOrDefault()?.TotalCount ?? 0,
                HasMore = items.Count == pageSize,
                LastSeenId = items.LastOrDefault()?.FileId
            };
        }
    }
}
