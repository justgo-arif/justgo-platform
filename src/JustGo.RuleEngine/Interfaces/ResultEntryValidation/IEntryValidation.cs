using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.RuleEngine.Services.ResultEntryValidation;

namespace JustGo.RuleEngine.Interfaces.ResultEntryValidation
{
    public interface IEntryValidation
    {
        Task<List<EntryValidationDto>> ValidateEntryAsync(int scopeReferenceId, string validationItemJson, CancellationToken cancellationToken = default);
    }
}
