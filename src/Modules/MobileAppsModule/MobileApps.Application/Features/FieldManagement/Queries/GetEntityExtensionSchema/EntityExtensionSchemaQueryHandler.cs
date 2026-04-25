using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.FieldManagement.Queries.GetEntityExtensionSchema;
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementSchemaId
{
    class EntityExtensionSchemaQueryHandler : IRequestHandler<EntityExtensionSchemaQuery, List<EntityExtensionSchemaCore>>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        public EntityExtensionSchemaQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<EntityExtensionSchemaCore>> Handle(EntityExtensionSchemaQuery request, CancellationToken cancellationToken)
        {
            string query = @"select es.OwnerType,es.ExtensionArea, ui.*  from EntityExtensionUI ui
		    inner join EntityExtensionSchema es on ui.ExId=es.ExId
		    where ui.ItemId IN @ItemIds";
            var param = new DynamicParameters();
            param.Add("@ItemIds",request.ItemIds);

            var result=  await _readRepository.Value.GetListAsync(query, param, null, "text");
            return JsonConvert.DeserializeObject<List<EntityExtensionSchemaCore>>(JsonConvert.SerializeObject(result));
        }
    }
}
