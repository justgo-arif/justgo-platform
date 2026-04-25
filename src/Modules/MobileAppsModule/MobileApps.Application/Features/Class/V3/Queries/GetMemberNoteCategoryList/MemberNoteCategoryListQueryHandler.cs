using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using MobileApps.Domain.Entities.V2.Classes;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetMemberNoteCategoryList
{
    class MemberNoteCategoryListQueryHandler : IRequestHandler<MemberNoteCategoryListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public MemberNoteCategoryListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(MemberNoteCategoryListQuery request, CancellationToken cancellationToken)
        {
            string sql = $@"select * from NoteCategories where IsActive=1";
            var result = await _readRepository.Value.GetListAsync(sql, null, null, "text");

            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result)) ?? new List<IDictionary<string, object>>();

        }

    }
}
