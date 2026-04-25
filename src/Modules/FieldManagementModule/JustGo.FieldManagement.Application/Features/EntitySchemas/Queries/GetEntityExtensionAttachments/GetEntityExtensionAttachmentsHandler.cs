using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionAttachments
{
    public class GetEntityExtensionAttachmentsHandler : IRequestHandler<GetEntityExtensionAttachmentsQuery, List<IDictionary<string, object>>>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetEntityExtensionAttachmentsHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<IDictionary<string, object>>> Handle(GetEntityExtensionAttachmentsQuery request, CancellationToken cancellationToken)
        {
            var attachments = new List<IDictionary<string, object>>();
            var sql = $"select * from Ex{request.Mode}{request.ExtensionArea}_LargeText where fieldid=@FieldId and docid=@DocId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@FieldId", request.FieldId);
            queryParameters.Add("@DocId", request.DocId);
            var result = (await _readRepository.GetLazyRepository<IDictionary<string, object>>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
