using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V3.Classes;

namespace MobileApps.Application.Features.Class.V3.Queries   
{
    public class SessionEligibilityRulesQuery : IRequest<List<RuleModel>>
    {
        public int ProductId { get; set; }
        public int MemberDocId { get; set; }
        public int UserId { get; set; }
    }
}
