using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetResultListByEvent;

public class GetResultListByEventIdQueryHandler : IRequestHandler<GetResultListByEventIdQuery, Result<KeysetPagedResult<ResultListDto>>>
{
    private readonly IReadRepository<ResultListDto> _readRepository;

    public GetResultListByEventIdQueryHandler(IReadRepository<ResultListDto> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<KeysetPagedResult<ResultListDto>>> Handle(GetResultListByEventIdQuery request, CancellationToken cancellationToken = default)
    {
        const string sqlQuery = """
                                SELECT 
                                    Count(1) Over() TotalCount,
                                	F.FileType,
                                    F.UploadedFileId,
                                    F.[FileName],
                                    TRY_CAST(F.UploadedAt AS VARCHAR(20)) AS UploadedAt, 
                                    MAX(CONCAT_WS(' ', U.FirstName, U.LastName)) AS UpdatedBy, 
                                    U.ProfilePicURL, 
                                    D.[Name] AS Disciplines,
                                    MAX(CASE RC.CompetitionStatusId 
                                        WHEN 1 THEN 'Draft'
                                        WHEN 2 THEN 'Published'
                                        WHEN 3 THEN 'In Progress'
                                        WHEN 4 THEN 'Failed'
                                        ELSE NULL
                                    END) AS ResultStatus,
                                    F.EventId,
                                    MAX(ISNULL(ERROR_CNT.ErrorCount, 0)) AS ErrorCount,
                                	MAX(ISNULL(RECORDS_CTE.Records, 0)) AS Records
                                FROM ResultCompetition rc
                                INNER JOIN ResultUploadedFile  f on rc.UploadedFileId = f.UploadedFileId
                                LEFT JOIN (
                                    SELECT 
                                        UploadedFileId,
                                        COUNT(1) AS ErrorCount
                                    FROM ResultUploadedMember 
                                    WHERE ErrorType = 'Validation Failed' AND IsDeleted = 0
                                    GROUP BY UploadedFileId
                                ) ERROR_CNT ON F.UploadedFileId = ERROR_CNT.UploadedFileId
                                LEFT JOIN (
                                     SELECT 
                                        UploadedFileId,
                                        COUNT(1) AS Records
                                    FROM ResultUploadedMember 
                                    WHERE IsDeleted = 0
                                    GROUP BY UploadedFileId
                                ) RECORDS_CTE ON F.UploadedFileId = RECORDS_CTE.UploadedFileId
                                LEFT JOIN ValidationScopes S ON F.DisciplineId = S.ValidationScopeId
                                LEFT JOIN ResultDisciplines D ON S.ScopeReferenceId = D.DisciplineId
                                LEFT JOIN [USER] U ON F.UpdatedBy = U.Userid
                                WHERE rc.eventId = @EventId and f.IsDeleted = 0
                                AND (@Search = ''
                                    OR F.[FileName] LIKE '%' + @Search + '%'
                                    OR D.[Name] LIKE '%' + @Search + '%')
                                GROUP BY 
                                    F.FileType,
                                    F.UploadedFileId,
                                    F.[FileName],
                                	F.EventId,
                                	F.UploadedAt,
                                	D.[Name],
                                	U.ProfilePicURL
                                ORDER BY 
                                    CASE WHEN @SortBy = 'FileName' AND @OrderBy = 'ASC' THEN F.[FileName] END ASC,
                                    CASE WHEN @SortBy = 'FileName' AND @OrderBy = 'DESC' THEN F.[FileName] END DESC,
                
                                	CASE WHEN @SortBy = 'UploadedAt' AND @OrderBy = 'ASC' THEN F.UploadedAt END ASC,
                                    CASE WHEN @SortBy = 'UploadedAt' AND @OrderBy = 'DESC' THEN F.UploadedAt END DESC,
                
                                	CASE WHEN @SortBy = 'Disciplines' AND @OrderBy = 'ASC' THEN D.[Name] END ASC,
                                    CASE WHEN @SortBy = 'Disciplines' AND @OrderBy = 'DESC' THEN D.[Name] END DESC,
                                	F.EventId DESC
                                OFFSET (@PageNumber - 1) * @PageSize ROWS
                                FETCH NEXT @PageSize ROWS ONLY;
                                """;
        
        try
        {
            var parameters = PrepareQueryParameters(request);

            var events = await _readRepository.GetListAsync(
                sqlQuery,
                cancellationToken,
                parameters,
                null,
                QueryType.Text
            );

            var resultListDtos = events.ToList();

            var result = new KeysetPagedResult<ResultListDto>
            {
                Items = resultListDtos,
                TotalCount = resultListDtos.FirstOrDefault()?.TotalCount ?? 0,
                HasMore = resultListDtos.Count == request.PageSize,
                LastSeenId = resultListDtos.LastOrDefault()?.EventId
            };

            return result;
        }
        catch (Exception ex)
        {
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
            
            return Result<KeysetPagedResult<ResultListDto>>.Failure(
                "An error occurred while retrieving events. Please try again.",
                ErrorType.InternalServerError);
        }
    }

    private static object PrepareQueryParameters(GetResultListByEventIdQuery request)
    {
        return new
        {
            SortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "UploadedAt" : request.SortBy.Trim(),
            OrderBy = string.IsNullOrWhiteSpace(request.OrderBy) ? "DESC" : request.OrderBy.Trim(),
            EventId = request.EventId,
            Search = string.IsNullOrWhiteSpace(request.SearchTerm) ? string.Empty : request.SearchTerm.Trim(),
            PageNumber = Math.Max(1, request.PageNumber),
            PageSize = Math.Min(100, Math.Max(1, request.PageSize))
        };
    }
}