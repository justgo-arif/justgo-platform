using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetItemIdBySyncGuid
{
    public class GetItemIdBySyncGuidHandler: IRequestHandler<GetItemIdBySyncGuidQuery, string>
    {
        private readonly IReadRepositoryFactory _readRepository;
        public GetItemIdBySyncGuidHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<string> Handle(GetItemIdBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            var sql = "SELECT [ItemId] FROM [dbo].[EntityExtensionUI] WHERE [SyncGuid]=@SyncGuid";
            var queryParameters = new { SyncGuid = request.SyncGuid };
            return await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync<string>(sql, queryParameters, null, cancellationToken, "text");
        }
    }
}
