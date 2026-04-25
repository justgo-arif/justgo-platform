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
using MobileApps.Domain.Entities;
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection
{
    class FieldManagementMemberFormQueryHandler : IRequestHandler<FieldManagementMemberFormQuery, List<FormSchemaInfo>>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        public FieldManagementMemberFormQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<FormSchemaInfo>> Handle(FieldManagementMemberFormQuery request, CancellationToken cancellationToken)
        {
            string query = @"if(@Entity='NGB' or @Entity='GoMembership')
            select ItemKey,Value from SystemSettings where ItemKey in (select s from dbo.SplitString(@Key,',')) and isnull(SystemSettings.Restricted,0) = 0

            else if(@Entity='User')

             select SystemSettings.ItemKey,UserSettings.Value  from UserSettings join SystemSettings on SystemSettings.ItemId=UserSettings.ItemId
	         where UserId=@EntityId and SystemSettings.ItemKey in (select s from dbo.SplitString(@Key,',')) and isnull(SystemSettings.Restricted,0) = 0

            else

            select SystemSettings.ItemKey,EntitySetting.Value  from    EntitySetting join SystemSettings on SystemSettings.ItemId=EntitySetting.ItemId
	         where Entity=@Entity and EntityId=@EntityId and SystemSettings.ItemKey in (select s from dbo.SplitString(@Key,',')) and isnull(SystemSettings.Restricted,0) = 0;";

            var param = new DynamicParameters();
            param.Add("@Entity", request.Entity);
            param.Add("@EntityId", request.UserId);
            param.Add("@Key", request.ItemKey);

            var result = await _readRepository.Value.GetAsync(query, param, null, "text");
            var data = JsonConvert.DeserializeObject<SchemaSettings>(JsonConvert.SerializeObject(result));

            return JsonConvert.DeserializeObject<List<FormSchemaInfo>>(data.Value);

        }
    }
}
