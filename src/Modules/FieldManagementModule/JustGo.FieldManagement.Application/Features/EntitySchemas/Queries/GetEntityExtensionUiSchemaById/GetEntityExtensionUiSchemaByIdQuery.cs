using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Domain.Entities;
using Pipelines.Sockets.Unofficial.Arenas;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiSchemaById
{
    public class GetEntityExtensionUiSchemaByIdQuery : IRequest<EntityExtensionSchema>
    {
        public string Id { get; set; }
        public bool IsArena { get; set; }
        public GetEntityExtensionUiSchemaByIdQuery(string id, bool isArena = false)
        {
            Id = id;
            IsArena = isArena;
        }
    }
}
