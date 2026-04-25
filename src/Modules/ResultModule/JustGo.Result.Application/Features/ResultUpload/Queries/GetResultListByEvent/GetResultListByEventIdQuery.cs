using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetResultListByEvent;

public class GetResultListByEventIdQuery : IRequest<Result<KeysetPagedResult<ResultListDto>>>
{
    [Required] public int EventId { get; set; }
    [Required] public int PageNumber { get; set; } = 1;
    [Required] public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    
    public string SearchTerm { get; set; } = string.Empty;

    public string? OrderBy { get; set; }
    public required SportType SportType { get; set; }
}