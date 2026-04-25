using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.Common.Queries.GetDisciplineByFileId
{
    public class GetDisciplineByFileIdQuery : IRequest<int>
    {
        public int FileId { get; set; }
    }
}
