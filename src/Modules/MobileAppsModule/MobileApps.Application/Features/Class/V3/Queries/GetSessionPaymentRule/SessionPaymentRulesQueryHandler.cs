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
using MobileApps.Domain.Entities.V3.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335;


namespace MobileApps.Application.Features.Class.V3.Queries.GetSessionPaymentRule
{
    class SessionPaymentRulesQueryHandler : IRequestHandler<SessionPaymentRulesQuery, PaymentStatusModel>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private readonly LazyService<IReadRepository<object>> _readObjRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public SessionPaymentRulesQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService, LazyService<IReadRepository<object>> readObjRepository)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
            _readObjRepository = readObjRepository;
        }
        public async Task<PaymentStatusModel> Handle(SessionPaymentRulesQuery request, CancellationToken cancellationToken)
        {
            string sql = GetSessionIdsSql;


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OccurrenceId", request.OccurrenceId);
            var result= await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
            

            string sqlPayment = @"GetAttendeePaymentDetailsBySessionId";
            int sessionId = result==null?0: result.SessionId;
            var queryParam = new DynamicParameters();
            queryParam.Add("SessionId", sessionId);
            queryParam.Add("AttendeeId", request.AttendeeId);
            queryParam.Add("ProductId", request.ProductId > 0 ? request.ProductId : 0);

            var resultPayment = await _readRepository.Value.GetListAsync(sqlPayment, queryParam, null, "sp");
            var data= JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(JsonConvert.SerializeObject(resultPayment)) ?? new List<IDictionary<string, object>>();

            return data.Count>0?new PaymentStatusModel{ProductId= request.ProductId ,PaymentStatus= data?.FirstOrDefault()["ReceiptStatus"].ToString(),PaymentDate=DateTime.Parse( data?.FirstOrDefault()["Date"].ToString())}: new PaymentStatusModel();
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
        private string GetSessionIdsSql => @"select top 1 ss.SessionId 
        from  JustGoBookingClassSessionSchedule ss 
        INNER JOIN JustGoBookingScheduleOccurrence oc ON ss.SessionScheduleId = oc.ScheduleId
        where oc.OccurrenceId=@OccurrenceId";
       
    }
}
