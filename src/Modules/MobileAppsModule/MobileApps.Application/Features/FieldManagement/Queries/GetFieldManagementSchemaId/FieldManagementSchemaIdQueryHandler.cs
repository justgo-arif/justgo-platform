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
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementSchemaId
{
    class FieldManagementSchemaIdQueryHandler:IRequestHandler<FieldManagementSchemaIdQuery, EntityExtensionSchemaCore>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        public FieldManagementSchemaIdQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<EntityExtensionSchemaCore> Handle(FieldManagementSchemaIdQuery request, CancellationToken cancellationToken)
        {
            string query = @"SELECT * FROM EntityExtensionSchema 
                            WHERE OwnerType=@OwnerType and OwnerId=@OwnerId and 
                            ExtensionArea=@ExtensionArea and ExtensionEntityId=0";
            var param = new DynamicParameters();
            param.Add("@OwnerType", request.OwnerType);
            param.Add("@OwnerId", request.OwnerId);
            param.Add("@ExtensionArea", request.ExtensionArea);

            var result=  await _readRepository.Value.GetAsync(query, param, null, "text");
            var data = JsonConvert.DeserializeObject<EntityExtensionSchemaCore>(JsonConvert.SerializeObject(result));
            return data;

        }
    }
}
