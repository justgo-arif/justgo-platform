using System.Collections.Generic;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Policies.Queries.GetPolicies
{
    public class GetPoliciesQuery : IRequest<List<Policy>>
    {
        public int Id { get; set; }
        public string PolicyName { get; set; }
        public string PolicyDescription { get; set; }
        public string PolicyRule { get; set; }
        public int ParentPolicyId { get; set; }

    }
}
