using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingTransferRequestDTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.BookingTransferRequest.Queries.CheckMemberPlanStatus
{
    public class CheckMemberPlanStatusHandler : IRequestHandler<CheckMemberPlanStatusQuery, MemberPlanStatusResultDto>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public CheckMemberPlanStatusHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<MemberPlanStatusResultDto> Handle(CheckMemberPlanStatusQuery request, CancellationToken cancellationToken)
        {
            var membersWithIssues = await GetMembersWithPlanIssuesAsync(request.sessionGuid, request.MemberDocIds, cancellationToken);

            return new MemberPlanStatusResultDto
            {
                AllMembersEligible = membersWithIssues.Count == 0,
                MembersWithIssues = membersWithIssues
            };
        }

        private async Task<List<MemberPlanStatusDto>> GetMembersWithPlanIssuesAsync(
            string sessionGuid,
            List<int> memberDocIds,
            CancellationToken cancellationToken)
        {
            if (memberDocIds == null || memberDocIds.Count == 0)
            {
                return new List<MemberPlanStatusDto>();
            }
            {




                var queryParameters = new DynamicParameters();
                queryParameters.Add("@SessionGuid", sessionGuid, size: 100);
                queryParameters.Add("@MemberDocIds", string.Join(",", memberDocIds), DbType.String);

                var sql = """
            DECLARE @PlanActiveStatus INT = 2;
            DECLARE @PlanCompleteStatus INT = 6;
            DECLARE @ScheduleCancelStatus INT = 6;
            DECLARE @ScheduleTroubleShootStatus INT = 8;
            DECLARE @SchedulePendingStatus INT = 1;

            DECLARE @ProductDocIds TABLE (ProductId INT);
            -- Get ProductId from SessionId

            declare @SessionId int	
            select @SessionId =SessionId from JustGoBookingClassSession where ClassSessionGuid=@SessionGuid

            insert into @ProductDocIds (ProductId)
            SELECT sp.ProductId
            FROM JustGoBookingClassSessionProduct sp
            WHERE sp.SessionId = @SessionId 
                AND ISNULL(sp.IsDeleted, 0) = 0
                --AND sp.ProductType = 4;

            -- Parse MemberDocIds into a table
            DECLARE @MemberList TABLE (MemberDocId INT);
            INSERT INTO @MemberList (MemberDocId)
            SELECT CAST(value AS INT)
            FROM STRING_SPLIT(@MemberDocIds, ',')
            WHERE RTRIM(value) <> '';

            -- Check members with problematic plan/schedule status
            SELECT DISTINCT
                u.MemberId as MemberId,
                CONCAT(ISNULL(u.FirstName, ''), ' ', ISNULL(u.LastName, '')) AS MemberName,
                CASE 
                    WHEN rps.Status = @SchedulePendingStatus THEN 1 
                    ELSE 0 
                END AS HasPendingSchedule,
                CASE 
                    WHEN rps.Status = @ScheduleTroubleShootStatus THEN 1 
                    ELSE 0 
                END AS HasTroubleshootSchedule,
                pp.Id AS PlanId,
                CASE pp.Status
                    WHEN 1 THEN 'Draft'
                    WHEN 2 THEN 'Active'
                    WHEN 3 THEN 'Suspended'
                    WHEN 4 THEN 'Cancelled'
                    WHEN 5 THEN 'Failed'
                    WHEN 6 THEN 'Complete'
                    ELSE 'Unknown'
                END AS PlanStatus
            FROM @MemberList ml
            INNER JOIN [User] u ON u.MemberDocId = ml.MemberDocId
            INNER JOIN RecurringPaymentPlan pp ON pp.ForEntityid = u.MemberDocId
            INNER JOIN RecurringPaymentCustomer rpc ON rpc.Id = pp.CustomerId
            INNER JOIN RecurringPaymentScheme rps ON rps.Id = pp.SchemeId
            LEFT JOIN Products_Default pd ON pd.DocId = pp.ProductId
            WHERE pp.Status = @PlanActiveStatus
                AND rps.Status IN (@SchedulePendingStatus, @ScheduleTroubleShootStatus)
                AND  pp.ProductId in (select ProductId from @ProductDocIds)
            """;

                var result = await _readRepository
                    .GetLazyRepository<MemberPlanStatusDto>()
                    .Value
                    .GetListAsync(sql, cancellationToken, queryParameters, null, "text");

                return result?.AsList() ?? new List<MemberPlanStatusDto>();
            }
        }
    }
}
