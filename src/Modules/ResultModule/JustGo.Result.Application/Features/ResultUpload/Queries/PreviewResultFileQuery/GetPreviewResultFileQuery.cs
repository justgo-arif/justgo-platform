using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.PreviewResultFileQuery;

public class GetPreviewResultFileQuery : IRequest<Result<KeysetPagedResult<FilePreviewDto>>>
{
    [Required] public int UploadFileId { get; set; }
    [Required] public int PageNumber { get; set; } = 1;
    [Required] public int PageSize { get; set; } = 10;
    
    public bool ShowErrorsOnly { get; set; } = false;
    
    public string? Search { get; set; } = string.Empty;
    
    public string? SortBy { get; set; } = "MemberId";
    
    public string? OrderBy { get; set; } = "ASC";
    public required SportType SportType { get; set; }
}