using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionSchemaById;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntitySchema
{
    public class GetEntitySchemaHandler : IRequestHandler<GetEntitySchemaQuery, EntityExtensionSchema>
    {
        private readonly IMediator _mediator;

        const string SELECT_ENTITY_EXTENSION_SCHEMA = @"SELECT ExId FROM EntityExtensionSchema 
                                                        WHERE OwnerType=@OwnerType and OwnerId=@OwnerId and 
                                                                ExtensionArea=@ExtensionArea and ExtensionEntityId=@ExtensionEntityId";       
        private readonly string[] AllowedOwner = { "Ngb", "Club" };
        private readonly string[] AllowedArea = { "Profile", "Qualification", "Membership", "Credential", "Event", "Club", "EventMaster", "Asset" };
        private readonly IReadRepositoryFactory _readRepository;

        public GetEntitySchemaHandler(IReadRepositoryFactory readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<EntityExtensionSchema> Handle(GetEntitySchemaQuery request, CancellationToken cancellationToken)
        {
            if (!AllowedOwner.Contains(request.OwnerType))
                throw new Exception("Invalid Owner Type");
            if (!AllowedArea.Contains(request.ExtensionArea))
                throw new Exception("Invalid Extension Area");
            if (request.OwnerType.Equals("Ngb"))
                request.OwnerId = 0;
            else
            {
                if (request.OwnerId <= 0)
                    throw new Exception("Invalid OwnerId");
            }
            if (request.ExtensionArea.Equals("Profile") || request.ExtensionArea.Equals("Event"))
                request.ExtensionEntityId = 0;

            var exId = await GetExId(request.OwnerType, request.OwnerId, request.ExtensionArea, request.ExtensionEntityId, cancellationToken);
            var schema = await _mediator.Send(new GetEntityExtensionSchemaByIdQuery(exId, request.IsArena));
            return schema;
        }

        private async Task<int> GetExId(string ownerType, int ownerId, string extensionArea, int extensionEntityId, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OwnerType", ownerType);
            queryParameters.Add("@OwnerId", ownerId);
            queryParameters.Add("@ExtensionArea", extensionArea);
            queryParameters.Add("@ExtensionEntityId", extensionEntityId);
            var result = (int)await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(SELECT_ENTITY_EXTENSION_SCHEMA, cancellationToken, queryParameters, null, "text");
            return result;
        }



    }
}
