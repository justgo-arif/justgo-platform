using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JustGo.Authentication.Infrastructure.AbacAuthorization;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization
{
    public interface IAbacPolicyService
    {
        Task<AbacPolicy?> GetPolicyByName(string policyName, CancellationToken cancellationToken);
    }
}
