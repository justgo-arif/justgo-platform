using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Policies.Queries.GetPolicyByName
{
    public class GetPolicyByNameQuery:IRequest<Policy>
    {
        public GetPolicyByNameQuery(string policyName)
        {
            PolicyName = policyName;
        }

        public string PolicyName { get; set;}
    }
}
