using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberData
{
    public class GetMemberDataByFileQueryHandler : IRequestHandler<GetMemberDataByFileQuery, KeysetPagedResult<MemberDataDto>>
    {
        private readonly IReadRepositoryFactory _readRepository;

        private const string SqlQuery = """
                                        DECLARE @FuzzyThreshold INT = 2;
                                        DECLARE @MemberIdKey NVARCHAR(100) = 'Member ID';
                                        DECLARE @MemberFirstNameKey NVARCHAR(100) = 'First Name';
                                        DECLARE @MemberLastNameKey NVARCHAR(100) = 'Last Name';
                                        DECLARE @AssetIdKey NVARCHAR(100) = 'Horse ID';
                                        DECLARE @AssetNameKey NVARCHAR(100) = 'Horse Name';
                                        
                                        ;WITH FilteredBaseData AS (
                                            SELECT 
                                                umd.UploadedMemberDataId,
                                                umd.UploadedMemberId,
                                                umd.MemberData,
                                                um.UserId,
                                                um.IsValidated,
                                                um.ErrorType,
                                                um.ErrorMessage,
                                                uf.FileName,
                                                JSON_VALUE(umd.MemberData, @JsonPath) AS SortValue
                                            FROM ResultUploadedMemberData umd
                                            INNER JOIN ResultUploadedMember um ON umd.UploadedMemberId = um.UploadedMemberId
                                            INNER JOIN ResultUploadedFile uf ON uf.UploadedFileId = um.UploadedFileId
                                            WHERE uf.OwnerId = @OwnerId
                                              AND uf.UploadedFileId = @FileId
                                              AND uf.IsDeleted = 0
                                              AND um.IsDeleted = 0
                                              AND (@ErrorsOnly = 0 OR um.IsValidated = 0)
                                        ),
                                        JsonKeyOrdering AS (
                                            SELECT 
                                                fbd.UploadedMemberDataId,
                                                fbd.UploadedMemberId,
                                                fbd.IsValidated,
                                                fbd.ErrorType,
                                                fbd.ErrorMessage,
                                                fbd.FileName,
                                                fbd.SortValue,
                                                CASE 
                                                    WHEN @MemberIdKey = '' AND @MemberFirstNameKey = '' AND @MemberLastNameKey = '' 
                                                         AND @AssetIdKey = '' AND @AssetNameKey = '' THEN fbd.MemberData
                                                    ELSE
                                                        '{' +
                                                        STUFF(
                                                            ISNULL(CASE WHEN @MemberIdKey <> '' AND JSON_VALUE(fbd.MemberData, '$."' + @MemberIdKey + '"') IS NOT NULL
                                                                THEN ',"' + @MemberIdKey + '":"' + REPLACE(JSON_VALUE(fbd.MemberData, '$."' + @MemberIdKey + '"'), '"', '\"') + '"' ELSE '' END,'') +
                                                            ISNULL(CASE WHEN @MemberFirstNameKey <> '' AND JSON_VALUE(fbd.MemberData, '$."' + @MemberFirstNameKey + '"') IS NOT NULL
                                                                THEN ',"' + @MemberFirstNameKey + '":"' + REPLACE(JSON_VALUE(fbd.MemberData, '$."' + @MemberFirstNameKey + '"'), '"', '\"') + '"' ELSE '' END,'') +
                                                            ISNULL(CASE WHEN @MemberLastNameKey <> '' AND JSON_VALUE(fbd.MemberData, '$."' + @MemberLastNameKey + '"') IS NOT NULL
                                                                THEN ',"' + @MemberLastNameKey + '":"' + REPLACE(JSON_VALUE(fbd.MemberData, '$."' + @MemberLastNameKey + '"'), '"', '\"') + '"' ELSE '' END,'') +
                                                            ISNULL(CASE WHEN @AssetIdKey <> '' AND JSON_VALUE(fbd.MemberData, '$."' + @AssetIdKey + '"') IS NOT NULL
                                                                THEN ',"' + @AssetIdKey + '":"' + REPLACE(JSON_VALUE(fbd.MemberData, '$."' + @AssetIdKey + '"'), '"', '\"') + '"' ELSE '' END,'') +
                                                            ISNULL(CASE WHEN @AssetNameKey <> '' AND JSON_VALUE(fbd.MemberData, '$."' + @AssetNameKey + '"') IS NOT NULL
                                                                THEN ',"' + @AssetNameKey + '":"' + REPLACE(JSON_VALUE(fbd.MemberData, '$."' + @AssetNameKey + '"'), '"', '\"') + '"' ELSE '' END,'') +
                                                            ISNULL((
                                                                SELECT 
                                                                    ',"' + REPLACE(CAST(j2.[key] AS NVARCHAR(100)),'"','\"') + '":"' +
                                                                    REPLACE(ISNULL(CAST(j2.[value] AS NVARCHAR(MAX)),''), '"','\"') + '"'
                                                                FROM OPENJSON(fbd.MemberData) j2
                                                                WHERE CAST(j2.[key] AS NVARCHAR(100)) NOT IN (
                                                                    ISNULL(NULLIF(@MemberIdKey,''),   '##NM1##'),
                                                                    ISNULL(NULLIF(@MemberFirstNameKey,''), '##NM2##'),
                                                                    ISNULL(NULLIF(@MemberLastNameKey,''),  '##NM3##'),
                                                                    ISNULL(NULLIF(@AssetIdKey,''),        '##NM4##'),
                                                                    ISNULL(NULLIF(@AssetNameKey,''),      '##NM5##')
                                                                )
                                                                FOR XML PATH(''), TYPE
                                                            ).value('.','NVARCHAR(MAX)'), '')
                                                        ,1,1,'') + '}'
                                                END AS OrderedMemberData,
                                                CASE 
                                                    WHEN @Search IS NULL OR @Search = '' THEN 0
                                                    ELSE (
                                                        SELECT MAX(
                                                            CASE
                                                                WHEN LOWER(CAST([value] AS NVARCHAR(100))) = LOWER(@Search) THEN 100
                                                                WHEN LOWER(CAST([value] AS NVARCHAR(100))) LIKE LOWER(@Search + '%') THEN 90
                                                                WHEN LOWER(CAST([value] AS NVARCHAR(100))) LIKE LOWER('%' + @Search + '%') THEN 80
                                                                WHEN @Search NOT LIKE '%[0-9]%'
                                                                     AND CAST([value] AS NVARCHAR(100)) NOT LIKE '%[0-9]%'
                                                                     AND DIFFERENCE(CAST([value] AS NVARCHAR(100)), @Search) >= @FuzzyThreshold THEN 70
                                                                ELSE 0
                                                            END
                                                        )
                                                        FROM OPENJSON(fbd.MemberData)
                                                        WHERE CAST([value] AS NVARCHAR(100)) <> ''
                                                    )
                                                END AS FuzzySearchScore
                                            FROM FilteredBaseData fbd
                                            WHERE @Search IS NULL 
                                               OR @Search = ''
                                               OR EXISTS (
                                                    SELECT 1
                                                    FROM OPENJSON(fbd.MemberData) sj
                                                    WHERE LOWER(CAST(sj.[value] AS NVARCHAR(100))) = LOWER(@Search)
                                                       OR LOWER(CAST(sj.[value] AS NVARCHAR(100))) LIKE LOWER('%' + @Search + '%')
                                                )
                                        )
                                        SELECT
                                            COUNT(1) OVER () AS TotalCount,
                                            jko.UploadedMemberDataId AS Id,
                                            jko.UploadedMemberId,
                                            @FileId AS FileId,
                                            jko.FileName,
                                            REPLACE(REPLACE(REPLACE(jko.OrderedMemberData, CHAR(10), ''), CHAR(13), ''), CHAR(9), '') AS MemberData,
                                            jko.IsValidated,
                                            jko.ErrorType,
                                            jko.ErrorMessage,
                                            ISNULL(jko.SortValue,'') AS SortValue,
                                            ISNULL(jko.FuzzySearchScore,0) AS FuzzySearchScore
                                        FROM JsonKeyOrdering jko
                                        ORDER BY 
                                            CASE WHEN ISNULL(@Search,'') <> '' THEN jko.FuzzySearchScore END DESC,
                                            CASE WHEN @OrderBy = 'ASC'  THEN LOWER(LTRIM(RTRIM(jko.SortValue))) END ASC,
                                            CASE WHEN @OrderBy = 'DESC' THEN LOWER(LTRIM(RTRIM(jko.SortValue))) END DESC,
                                            jko.UploadedMemberId DESC
                                        OFFSET (@PageNumber - 1) * @PageSize ROWS
                                        FETCH NEXT @PageSize ROWS ONLY;
                                        """;


        public GetMemberDataByFileQueryHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<KeysetPagedResult<MemberDataDto>> Handle(GetMemberDataByFileQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;

            string jsonPath;

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                var key = request.SortBy.Replace("\"", "\"\"");
                jsonPath = $"$.\"{key}\"";
            }
            else
            {
                jsonPath = "$.\"\"";
            }

            var queryParameters = new DynamicParameters();
            queryParameters.Add("PageNumber", pageNumber);
            queryParameters.Add("PageSize", pageSize);
            queryParameters.Add("OwnerId", request.OwnerId);
            queryParameters.Add("FileId", request.FileId);
            queryParameters.Add("ErrorsOnly", request.ErrorsOnly ? 1 : 0);
            queryParameters.Add("Search", request.Search ?? string.Empty);
            queryParameters.Add("SortBy", request.SortBy);
            queryParameters.Add("OrderBy", request.OrderBy ?? "DESC");
            queryParameters.Add("JsonPath", jsonPath);

            var repo = _readRepository.GetRepository<MemberDataDto>();
            var items = (await repo.GetListAsync(SqlQuery, cancellationToken, queryParameters, commandType: QueryType.Text)).ToList();

            return new KeysetPagedResult<MemberDataDto>
            {
                Items = items,
                TotalCount = items.Count == 0 ? 0: items.FirstOrDefault()!.TotalCount,
                HasMore = items.Count == pageSize,
                LastSeenId = items.LastOrDefault()?.Id
            };
        }
    }
}
