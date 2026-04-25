using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionSchemaById
{

    public class GetEntityExtensionSchemaByIdQuery : IRequest<EntityExtensionSchema>
    {
        public int ExId { get; }
        public bool IsArena { get; }

        public GetEntityExtensionSchemaByIdQuery(int exId, bool isArena = false)
        {
            ExId = exId;
            IsArena = isArena;
        }
    }
}
