using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities.V2.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace MobileApps.Application.Features.Class.V2.Queries.GetSessionPaymentRule
{
    class SessionPaymentRulesQueryHandler : IRequestHandler<SessionPaymentRulesQuery, PaymentStatusModel>
    {
        private readonly LazyService<IReadRepository<PaymentStatusModel>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public SessionPaymentRulesQueryHandler(LazyService<IReadRepository<PaymentStatusModel>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<PaymentStatusModel> Handle(SessionPaymentRulesQuery request, CancellationToken cancellationToken)
        {
            string sql = GetPaymentCheckingSql();


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AttendeeId", request.AttendeeId);
            queryParameters.Add("@OccurrenceId", request.OccurrenceId);
            queryParameters.Add("@ProductId", request.ProductId);
            var result= await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
            return result;


        }
        private string GetPaymentCheckingSql()
        {
            return @"SELECT  
            pl.ProductId,
            CASE 
                WHEN pl.Id IS NULL THEN 'Paid' -- No payment plan
                WHEN ps.PaymentDate < oc.StartDate THEN 'Due'
                ELSE 'Paid'
            END AS PaymentStatus
            

            FROM JustGoBookingAttendee att
            INNER JOIN JustGoBookingClassSessionSchedule ss
            ON att.SessionId = ss.SessionId
            INNER JOIN JustGoBookingScheduleOccurrence oc
            ON ss.SessionScheduleId = oc.ScheduleId
            LEFT JOIN RecurringPaymentPlan pl ON att.EntityDocId = pl.ForEntityId AND pl.[Status] IN (2, 3)

            OUTER APPLY (
            SELECT TOP 1 psx.[Status], psx.PaymentDate
            FROM RecurringPaymentSchedule psx
            WHERE psx.PlanId = pl.Id
                AND psx.[Status] IN (1,3,8)
            ORDER BY psx.PaymentDate DESC
            ) ps
            WHERE att.AttendeeId = @AttendeeId
              AND oc.OccurrenceId = @OccurrenceId
              AND pl.ProductId =@ProductId;";
        }

       
    }
}
