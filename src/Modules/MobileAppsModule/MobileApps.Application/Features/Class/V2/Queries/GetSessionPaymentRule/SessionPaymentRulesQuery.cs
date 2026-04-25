using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Classes;

namespace MobileApps.Application.Features.Class.V2.Queries.GetSessionPaymentRule    
{
    public class SessionPaymentRulesQuery : IRequest<PaymentStatusModel> 
    {
        public int AttendeeId { get; set; }
        public int OccurrenceId { get; set; }
        public int ProductId { get; set; }
        
    }
}
