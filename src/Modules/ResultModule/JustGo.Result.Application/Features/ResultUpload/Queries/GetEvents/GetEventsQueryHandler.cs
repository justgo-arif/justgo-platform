using System.Data;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetEvents;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, Result<KeysetPagedResult<EventDto>>>
{
    private readonly IReadRepository<EventDto> _readRepository;

    public GetEventsQueryHandler(IReadRepository<EventDto> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<KeysetPagedResult<EventDto>>> Handle(GetEventsQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = PrepareQueryParameters(request);
           
            var events = await _readRepository.GetListAsync(
                "GetResultEvents",
                cancellationToken,
                parameters,
                null,
                QueryType.StoredProcedure
            );
            
            var eventsList = events.ToList();
            var result = new KeysetPagedResult<EventDto>
            {
                Items = eventsList,
                TotalCount = eventsList.FirstOrDefault()?.TotalCount ?? 0,
                HasMore = eventsList.Count == request.PageSize,
                LastSeenId = eventsList.LastOrDefault()?.EventId
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
            
            return Result<KeysetPagedResult<EventDto>>.Failure(
                "An error occurred while retrieving events. Please try again.", 
                ErrorType.InternalServerError);
        }
    }
    
    private static object PrepareQueryParameters(GetEventsQuery request)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Search", 
            string.IsNullOrWhiteSpace(request.Search) ? string.Empty : request.Search.Trim(),
            DbType.String);

        parameters.Add("@FilterBy", 
            string.IsNullOrWhiteSpace(request.FilterBy) ? string.Empty : request.FilterBy.Trim(),
            DbType.String);

        parameters.Add("@SortBy", 
            string.IsNullOrWhiteSpace(request.SortBy) ? "EndDate" : request.SortBy.Trim(),
            DbType.String);

        parameters.Add("@OrderBy", 
            string.IsNullOrWhiteSpace(request.OrderBy) ? "DESC" : request.OrderBy.Trim(),
            DbType.String);

        parameters.Add("@PageNumber", Math.Max(1, request.PageNumber), DbType.Int32);
        parameters.Add("@PageSize", Math.Min(100, Math.Max(1, request.PageSize)), DbType.Int32);

        parameters.Add("@OwnerId", request.OwnerId, DbType.Int32);

        return parameters;
    }
}


#region improvements

//     private static object PrepareQueryParameters(GetEventsQuery request)
//     {
//         var parameters = new DynamicParameters();
//         
//         var searchStrategy = DetermineSearchStrategy(request.Search);
//
//         parameters.Add("@Search",
//             string.IsNullOrWhiteSpace(request.Search) ? string.Empty : request.Search.Trim(),
//             DbType.String);
//
//         parameters.Add("@ContainsQuery", searchStrategy.ContainsQuery, DbType.String);
//         parameters.Add("@UseLikeSearch", searchStrategy.UseLikeSearch, DbType.Boolean);
//         parameters.Add("@UseFullTextSearch", searchStrategy.UseFullTextSearch, DbType.Boolean);
//         parameters.Add("@HasSearch", searchStrategy.HasSearch, DbType.Boolean);
//
//         parameters.Add("@FilterBy",
//             string.IsNullOrWhiteSpace(request.FilterBy) ? string.Empty : request.FilterBy.Trim(),
//             DbType.String);
//
//         parameters.Add("@SortBy",
//             string.IsNullOrWhiteSpace(request.SortBy) ? "EndDate" : request.SortBy.Trim().ToLower(),
//             DbType.String);
//
//         parameters.Add("@OrderBy",
//             string.IsNullOrWhiteSpace(request.OrderBy) ? "DESC" : request.OrderBy.Trim().ToUpper(),
//             DbType.String);
//
//         parameters.Add("@PageNumber", Math.Max(1, request.PageNumber), DbType.Int32);
//         parameters.Add("@PageSize", Math.Min(100, Math.Max(1, request.PageSize)), DbType.Int32);
//         parameters.Add("@OwnerId", request.OwnerId, DbType.Int32);
//
//         return parameters;
//     }
//     
//     private static SearchStrategy DetermineSearchStrategy(string search)
//     {
//         var strategy = new SearchStrategy
//         {
//             ContainsQuery = "\"\"",
//             UseLikeSearch = false,
//             UseFullTextSearch = false,
//             HasSearch = false
//         };
//
//         if (string.IsNullOrWhiteSpace(search))
//         {
//             return strategy;
//         }
//
//         strategy.HasSearch = true;
//         var trimmedSearch = search.Trim();
//         
//         var searchWords = trimmedSearch
//             .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
//             .Where(word => !string.IsNullOrWhiteSpace(word))
//             .ToList();
//         
//         if (searchWords.Count == 1 && trimmedSearch.Length <= 3)
//         {
//             strategy.UseLikeSearch = true;
//             return strategy;
//         }
//         
//         strategy.UseFullTextSearch = true;
//         strategy.UseLikeSearch = true;
//         
//         strategy.ContainsQuery = string.Join(" AND ",
//             searchWords.Select(word => $"\"{word}\""));
//
//         return strategy;
//     }
//     
//     private sealed class SearchStrategy
//     {
//         public string ContainsQuery { get; set; } = string.Empty;
//         public bool UseLikeSearch { get; set; }
//         public bool UseFullTextSearch { get; set; }
//         public bool HasSearch { get; set; }
//     }
// }

#endregion