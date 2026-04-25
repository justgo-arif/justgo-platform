using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    class CategoryListQueryHandler : IRequestHandler<CategoryListQuery, IEnumerable<object>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public CategoryListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<IEnumerable<object>> Handle(CategoryListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select  bc.CategoryId,MAX(bc.Name) as CategoryName  from JustGoBookingCategory bc
            inner join JustGoBookingClassCategory cc on bc.CategoryId=cc.CategoryId
            where bc.OwnerId=@ClubDocId AND cc.IsDeleted<>1
            Group By bc.CategoryId;";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId",request.ClubDocId);

            return await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
        }
    }
}
