using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V3.Classes;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    public class SessionPaymentRulesQuery : IRequest<PaymentStatusModel> 
    {
        public int AttendeeId { get; set; }
        public int OccurrenceId { get; set; }
        public int ProductId { get; set; }
        
    }
}
