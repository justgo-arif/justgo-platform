using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.PreviewResultFileQuery;

public class GetPreviewResultFileQueryHandler : IRequestHandler<GetPreviewResultFileQuery,
    Result<KeysetPagedResult<FilePreviewDto>>>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetPreviewResultFileQueryHandler(IReadRepositoryFactory readRepository, IUnitOfWork unitOfWork)
    {
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<KeysetPagedResult<FilePreviewDto>>> Handle(GetPreviewResultFileQuery request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;

            var orderBy = string.Equals(request.OrderBy, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

            string? jsonPath = null;
            if (!string.IsNullOrWhiteSpace(request.SortBy) &&
                !string.Equals(request.SortBy, "FileName", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.SortBy, "MemberId", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.SortBy, "MemberName", StringComparison.OrdinalIgnoreCase))
            {
                // Escape quotes in the property name to prevent JSON injection
                var escapedKey = request.SortBy.Replace("\"", "\"\"");
                jsonPath = $"$.\"{escapedKey}\"";
            }

            request.SortBy = !string.IsNullOrEmpty(request.SortBy)
                ? char.ToUpper(request.SortBy[0]) + request.SortBy[1..]
                : request.SortBy;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("UploadedFileId", request.UploadFileId);
            queryParameters.Add("PageNumber", pageNumber);
            queryParameters.Add("PageSize", pageSize);
            queryParameters.Add("Search", request.Search ?? string.Empty);
            queryParameters.Add("SortBy", request.SortBy ?? "MemberId");
            queryParameters.Add("OrderBy", orderBy);
            queryParameters.Add("ShowErrorOnly", request.ShowErrorsOnly ? 1 : 0);
            queryParameters.Add("JsonPath", jsonPath ?? string.Empty);

            var repo = _readRepository.GetRepository<ResultMemberDataDto>();

            var items = (await repo.GetListAsync(GetQuery(request.SportType), cancellationToken, queryParameters, transaction,
                commandType: QueryType.Text)).ToList();

            await transaction.CommitAsync(cancellationToken);

            foreach (var item in items)
            {
                item.PopulateDynamicProperties("Official", "Estimated Ratings");
            }

            var editableFields = await GetEditableFieldsAsync(request.SportType, cancellationToken);

            var result = new KeysetPagedResult<FilePreviewDto>
            {
                Items =
                [
                    new FilePreviewDto
                    {
                        PreviewData = items,
                        EditableHeaders = editableFields
                    }
                ],
                TotalCount = items.FirstOrDefault()?.TotalCount ?? 0,
                HasMore = items.Count == pageSize,
                LastSeenId = items.LastOrDefault()?.Id
            };

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<KeysetPagedResult<FilePreviewDto>>.Failure(
                $"Failed to retrieve result file preview: {ex.Message}", ErrorType.InternalServerError);
        }
    }

    private async Task<IEnumerable<string>> GetEditableFieldsAsync(SportType sportType,
        CancellationToken cancellationToken)
    {
        var repo = _readRepository.GetRepository<string>();
        const string eaSqlQuery =
            "SELECT DISTINCT ColumnName FROM ResultUploadFields WHERE ColumnIdentifier IN (1, 4, 10, 11, 15);";
        const string usaTtSqlQuery =
            "SELECT DISTINCT ColumnName FROM ResultUploadFields WHERE ColumnIdentifier IN (12, 13, 14);";

        var sqlQuery = sportType switch
        {
            SportType.Equestrian => eaSqlQuery,
            SportType.TableTennis => usaTtSqlQuery,
            _ => eaSqlQuery
        };

        var fields = await repo.GetListAsync(sqlQuery, cancellationToken,
            null, null, commandType: QueryType.Text);

        return fields;
    }

    private string GetQuery(SportType sportType)
    {
        const string usaTtSearchQuery = """
                                                  @Search = '' OR
                                                  JSON_VALUE(MD.MemberData, '$.Winner Name') LIKE '%' + @Search + '%' OR
                                                  JSON_VALUE(MD.MemberData, '$.Loser Name') LIKE '%' + @Search + '%' OR
                                                  JSON_VALUE(MD.MemberData, '$.Winner') LIKE '%' + @Search + '%' OR
                                                  JSON_VALUE(MD.MemberData, '$.Loser') LIKE '%' + @Search + '%' 
                                                                  
                                        """;
        
        const string eaSearchQuery = """
                                           @Search = '' OR
                                           UM.MemberId LIKE '%' + @Search + '%' OR
                                           UM.MemberName LIKE '%' + @Search + '%'
                                                               
                                     """;
        
        
        return $"""
                          ;WITH ResultPreviewCTE AS (
                              SELECT
                                  MD.UploadedMemberDataId as Id,
                                  UF.[FileName],
                                  UM.MemberId,
                                  UM.MemberName,
                                  MD.MemberData,
                                  UM.ErrorType,
                                  UM.ErrorMessage,
                                  COUNT(1) OVER() AS TotalCount,
                                  CASE
                                      WHEN @SortBy = 'FileName' THEN UF.[FileName]
                                      WHEN @SortBy = 'MemberId' THEN UM.MemberId
                                      WHEN @SortBy = 'MemberName' THEN UM.MemberName
                                      ELSE ISNULL(JSON_VALUE(MD.MemberData, @JsonPath), '')
                                  END AS SortValue
                              FROM ResultUploadedFile UF
                              INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                              INNER JOIN ResultUploadedMemberData MD ON UM.UploadedMemberId = MD.UploadedMemberId
                              WHERE UF.UploadedFileId = @UploadedFileId
                                  AND UF.IsDeleted = 0
                                  AND UM.IsDeleted = 0
                                  AND (@ShowErrorOnly = 0 OR UM.ErrorMessage <> '')
                                  AND (
                                        {(sportType == SportType.TableTennis ? usaTtSearchQuery : eaSearchQuery)}
                                  )
                          ),
                          CanConfirm AS (
                              SELECT COUNT(1) AS ErrorCount
                              FROM ResultUploadedFile UF
                              INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                              WHERE UF.UploadedFileId = @UploadedFileId
                                  AND UF.IsDeleted = 0
                                  AND UM.IsDeleted = 0
                                  AND UM.ErrorMessage <> ''
                          )
                          SELECT Id,
                              [FileName],
                              MemberId,
                              MemberName,
                              MemberData,
                              ErrorType AS ValidationStatus,
                              ErrorMessage,
                              TotalCount,
                              (SELECT ErrorCount FROM CanConfirm) AS ErrorCount
                          FROM ResultPreviewCTE
                          ORDER BY 
                              -- Handle numeric sorting
                              CASE 
                                  WHEN ISNUMERIC(SortValue) = 1 AND @OrderBy = 'ASC'
                                      THEN TRY_CAST(SortValue AS BIGINT) 
                              END ASC,
                              CASE 
                                  WHEN ISNUMERIC(SortValue) = 1 AND @OrderBy = 'DESC'
                                      THEN TRY_CAST(SortValue AS BIGINT) 
                              END DESC,
                              -- Handle string sorting
                              CASE 
                                  WHEN ISNUMERIC(SortValue) = 0 AND @OrderBy = 'ASC'
                                      THEN SortValue 
                              END ASC,
                              CASE 
                                  WHEN ISNUMERIC(SortValue) = 0 AND @OrderBy = 'DESC'
                                      THEN SortValue 
                              END DESC
                          OFFSET (@PageNumber - 1) * @PageSize ROWS
                          FETCH NEXT @PageSize ROWS ONLY;
                          """;
    }
}