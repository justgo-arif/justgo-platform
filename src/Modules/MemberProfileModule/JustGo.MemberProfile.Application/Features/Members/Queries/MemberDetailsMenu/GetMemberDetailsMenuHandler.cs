using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUi;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.MemberDetailsMenu
{
    public class GetMemberDetailsMenuHandler : IRequestHandler<GetMemberDetailsMenuQuery, List<EntityExtensionUI>>
    {
        private readonly IMediator _mediator;

        public GetMemberDetailsMenuHandler(
            LazyService<IReadRepository<dynamic>> readRepository,
            IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<List<EntityExtensionUI>> Handle(GetMemberDetailsMenuQuery request, CancellationToken cancellationToken)
        {
            var entityExtensionUiQuery = new GetEntityExtensionUiQuery(
                request.OwnerType,
                request.OwnerId,
                request.ExtensionArea,
                request.ExtensionEntityId
            );

            var entityExtensionUiList = await _mediator.Send(entityExtensionUiQuery, cancellationToken);
            return entityExtensionUiList;
        }
    }
}
