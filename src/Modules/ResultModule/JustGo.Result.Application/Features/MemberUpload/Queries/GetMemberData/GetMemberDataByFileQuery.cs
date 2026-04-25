using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberData
{

    public class GetMemberDataByFileQuery : IRequest<KeysetPagedResult<MemberDataDto>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? OwnerGuid { get; set; }
        public int? OwnerId { get; set; }
        public int FileId { get; set; }
        public bool ErrorsOnly { get; set; }
        public string? Search { get; set; } = string.Empty;
        public string? SortBy { get; set; } = string.Empty;
        public string? OrderBy { get; set; } = "ASC";
    }
}
