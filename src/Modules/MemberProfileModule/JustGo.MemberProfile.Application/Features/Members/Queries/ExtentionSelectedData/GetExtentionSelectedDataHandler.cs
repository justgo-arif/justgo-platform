using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.Features.EntityData.Queries.GetEntityData;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.ExtentionSelectedData
{
    public class GetExtentionSelectedDataHandler : IRequestHandler<GetExtentionSelectedDataQuery, Dictionary<string, object>>
    {
        private readonly IMediator _mediator;

        public GetExtentionSelectedDataHandler(
            LazyService<IReadRepository<dynamic>> readRepository,
            IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Dictionary<string, object>> Handle(GetExtentionSelectedDataQuery request, CancellationToken cancellationToken)
        {
            var entityDataQuery = new GetEntityDataQuery(
                request.ExId,
                request.DocId
            );

            var entityExtensionSelectedData = await _mediator.Send(entityDataQuery, cancellationToken);
            return entityExtensionSelectedData;
        }
    }
}
