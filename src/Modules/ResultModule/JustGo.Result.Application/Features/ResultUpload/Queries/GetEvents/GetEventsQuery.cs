using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetEvents;

public class GetEventsQuery : IRequest<Result<KeysetPagedResult<EventDto>>>
{
    [Required] public int PageNumber { get; set; } = 1;
    [Required] public int PageSize { get; set; } = 10;
    [Required] public required string FilterBy { get; set; } = "All";
    
    public required string OwnerGuid { get; set; } 
    public int? OwnerId { get; set; } 
    public string Search { get; set; } = string.Empty;
    
    public string? SortBy { get; set; }
    
    public string? OrderBy { get; set; }
}