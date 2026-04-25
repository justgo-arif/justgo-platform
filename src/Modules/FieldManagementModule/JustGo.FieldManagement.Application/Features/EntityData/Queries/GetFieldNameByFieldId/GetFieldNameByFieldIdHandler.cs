using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetFieldNameByFieldId
{
    public class GetFieldNameByFieldIdHandler: IRequestHandler<GetFieldNameByFieldIdQuery, string>
    {
        private readonly IReadRepositoryFactory _readRepository;
        public GetFieldNameByFieldIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<string> Handle(GetFieldNameByFieldIdQuery request, CancellationToken cancellationToken)
        {
            var sql = "SELECT Caption FROM [dbo].[EntityExtensionField] WHERE Id=@Id";
            var queryParameters = new { Id = request.FieldId };
            return await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync<string>(sql, queryParameters, null, cancellationToken, "text");
        }
    }
}
