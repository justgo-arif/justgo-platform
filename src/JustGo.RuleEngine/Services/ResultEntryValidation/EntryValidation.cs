using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.RuleEngine.Interfaces.ResultEntryValidation;

namespace JustGo.RuleEngine.Services.ResultEntryValidation
{
    public class EntryValidation : IEntryValidation
    {
        private readonly IReadRepositoryFactory _readRepository;

        public EntryValidation(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<EntryValidationDto>> ValidateEntryAsync(int scopeReferenceId, string validationItemJson, CancellationToken cancellationToken = default)
        {
            const string procedureName = "RuleEngineExecuteValidationRules";
            var queryParam = new DynamicParameters();
            queryParam.Add("scopeReferenceId", scopeReferenceId);
            queryParam.Add("ValidationItemJson", validationItemJson);

            var result = (await _readRepository.GetRepository<EntryValidationDto>().GetListAsync(
                procedureName,
                cancellationToken,
                queryParam)).ToList();

            return result;
        }
    }
}
