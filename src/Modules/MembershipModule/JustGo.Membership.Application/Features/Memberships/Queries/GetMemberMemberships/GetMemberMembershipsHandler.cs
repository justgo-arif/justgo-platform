using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Domain.Entities;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMemberMemberships
{
    public class GetMemberMembershipsHandler : IRequestHandler<GetMemberMembershipsQuery, List<MembersHierarchiesWithMemberships>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;

        public GetMemberMembershipsHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<MembersHierarchiesWithMemberships>> Handle(GetMemberMembershipsQuery request, CancellationToken cancellationToken)
        {
            const string sql = """
                DECLARE @OrganisationPic NVARCHAR(100)=(SELECT TOP 1 '/store/Download?f='+[Value]+'&t=OrganizationLogo' FROM SystemSettings WHERE ItemKey = 'ORGANISATION.LOGO' AND [Value]!='');
                DECLARE @CurrencySymbol NVARCHAR(5)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey = 'SYSTEM.CURRENCY.DEFAULTCURRENCYSYMBOL');
                DECLARE @DownloadPath NVARCHAR(1000)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey = 'MEMBERSHIP.PDF_DOWNLOAD_REPORT_PATH');
                DECLARE @RepoPicurlFromat NVARCHAR(1000)= '/store/download?f=%s&t=repo&p=%d&p1=&p2=%d';
                DECLARE @OrganisationGuid  UniqueIdentifier =(SELECT TOP 1 d.SyncGuid SyncGuid FROM merchantprofile_default mpd 
                INNER JOIN Document d on d.DocId=mpd.DocId WHERE mpd.DocId NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
                AND mpd.Name != 'JustGo' and mpd.Merchanttype = 'NGB');

                With EntityAlias AS (
                SELECT j.Entity,j.Alias FROM dbo.SystemSettings AS ss CROSS APPLY OPENJSON(CAST(ss.[Value] AS nvarchar(max)))
                WITH (Entity nvarchar(200) '$.Entity',Alias  nvarchar(200) '$.Alias') AS j WHERE ss.ItemKey = 'ORGANISATION.E.ENTITY_ALIAS' 
                AND j.Entity NOT IN (SELECT cs.[Value] FROM SystemSettings ss CROSS APPLY STRING_SPLIT([Value],',') cs 
                WHERE ItemKey LIKE 'MEMBERSHIP.CATEGORY.HIDDEN')),

                UserHierarchyLinks AS (
                SELECT h.HierarchyTypeId,ht.HierarchyTypeName,ea.Entity,CASE WHEN h.EntityId=0 THEN 'NGB' ELSE 'Club' END EntityType,h.EntityName,h.EntityId
                ,CASE WHEN h.EntityId=0 THEN ISNULL(@OrganisationPic,'') WHEN ISNULL(d.[Location],'') NOT IN ('','Virtual') THEN FORMATMESSAGE(@RepoPicurlFromat,d.[Location], d.DocId, d.RepositoryId) ELSE '' END OrganisationPicUrl
                ,ISNULL(d.SyncGuid,@OrganisationGuid) OrganisationGuid
                FROM [User] u 
                INNER JOIN HierarchyLinks hl on u.UserId=hl.UserId 
                INNER JOIN Hierarchies h on h.Id=hl.[HierarchyId]
                INNER JOIN HierarchyTypes ht on ht.Id=h.HierarchyTypeId
                INNER JOIN EntityAlias ea on ea.Alias=ht.HierarchyTypeName
                LEFT JOIN Document d on d.DocId=h.EntityId
                LEFT JOIN GomembershipRegistry gm ON gm.EntityId=h.EntityId AND gm.[Status]=1
                WHERE u.UserSyncId=@UserSyncId AND ((h.EntityId!=0 AND gm.Id IS NOT NULL) OR h.EntityId=0)
                AND EXISTS (SELECT TOP 1 1 FROM License_Default WHERE ISNULL(LicenceOwner,0)=h.EntityId))

                SELECT ul.HierarchyTypeId,ul.HierarchyTypeName,ul.[Entity],ul.EntityType,ul.EntityName,ul.EntityId,ul.OrganisationPicUrl,CAST(OrganisationGuid AS uniqueidentifier) OrganisationGuid  
                FROM UserHierarchyLinks ul ORDER BY ul.HierarchyTypeId;


                WITH AllMemberships AS (
                SELECT u.UserSyncId,u.UserId,u.MemberDocId,um.ProductId,ISNULL(um.LicenceOwner,0) LicenceOwner,mld.SyncGuid MembershipSyncGuid
                ,ld.Reference MembershipName,ld.Licencetype,ld.Expirydateendingunit,@CurrencySymbol Currency,p.UnitPrice,s.[Name] [Status]
                ,pr.CurrentStateId StatusId,um.MemberLicenseDocId,um.StartDate,um.EndDate
                ,CASE WHEN ISNULL(pd.[Location],'') NOT IN ('','Virtual') THEN FORMATMESSAGE(@RepoPicurlFromat,pd.[Location], pd.DocId, pd.RepositoryId) ELSE '' END MembershipPicUrl
                ,CASE WHEN DATEDIFF(DAY, GETDATE(), um.EndDate) <= ld.RenewalWindow THEN 1 ELSE 0 END AS IsRenewalWindow 
                ,CASE WHEN EXISTS (SELECT 1 FROM Upgrade WHERE SourceMembership=ld.DocId AND [Status]=1) THEN 1 ELSE 0 END AS IsUpgradeable
                ,CASE WHEN ISNULL(@DownloadPath,'')!='' OR eq.EventId IS NOT NULL THEN 1 ELSE 0 END IsDownloadAvailable
                ,p.IsCommitmentSubscription
                FROM [User] u 
                INNER JOIN UserMemberships um on um.UserId=u.Userid 
                INNER JOIN ProcessInfo pr on pr.PrimaryDocid=um.MemberLicenseDocId
                INNER JOIN [State] s on s.StateId=pr.CurrentStateId
                INNER JOIN License_Links ll on ll.EntityId=um.ProductId
                INNER JOIN License_Default ld on ld.DocId=ll.DocId
                INNER JOIN Document pd on pd.DocId=um.ProductId
                INNER JOIN Document mld on mld.DocId=um.MemberLicenseDocId
                INNER JOIN Products_default p on p.DocId=um.ProductId
                LEFT JOIN EventQueue eq on eq.EntityDocId=um.MemberLicenseDocId AND eq.ProcessIngState=7
                WHERE u.UserSyncId=@UserSyncId-- AND s.[Name]!='Suspended'
                ),

                HistoricalMemberships AS (
                SELECT am.MemberLicenseDocId,CASE WHEN am.StatusId=64 OR (am.IsRenewalWindow=1 AND amd.MemberLicenseDocId IS NOT NULL) THEN 1 ELSE 0 END AS Historical  
                FROM AllMemberships am 
                LEFT JOIN AllMemberships amd ON amd.ProductId=am.ProductId AND amd.MemberLicenseDocId>am.MemberLicenseDocId AND amd.[Status]='Active'),


                RecurringMemberships AS (
                SELECT rpp.PlanGuid,rpp.CustomerId,am.MemberLicenseDocId,
                CASE WHEN rpsh.[Name] LIKE 'Subscription%' 
                THEN CASE WHEN (LEN(rpsh.[Name]) - LEN(REPLACE(rpsh.[Name], '_', ''))) >= 2 THEN 
                REPLACE(LEFT(rpsh.[Name],CHARINDEX('_', rpsh.[Name], CHARINDEX('_', rpsh.[Name]) + 1) - 1),'_', ' ')
                ELSE REPLACE(rpsh.[Name], '_', ' ') END
                WHEN rpsh.[Name] LIKE 'Installment%' AND am.IsCommitmentSubscription=1 THEN 'Subscription Monthly'
                WHEN rpsh.[Name] LIKE 'Installment%' THEN 'Installment' END AS DisplayName
                ,rps.Id,rps.PlanId,rps.PaymentDate,rps.[Status] [ScheduleStatus],rpp.[Status] [PlanStatus]
                ,rpsh.RecurringType
                ,ISNULL((SELECT TOP 1 1 FROM PaymentReceipts_Items pri INNER JOIN ProcessInfo pr on pr.PrimaryDocId=pri.DocId
                INNER JOIN [State] s on s.StateId=pr.CurrentStateId WHERE (pri.[Group]='Subscription,'+CAST(rps.Id AS VARCHAR) OR pri.[Group]='SubscriptionPayment,'+CAST(rps.Id AS VARCHAR)) AND s.[Name]='Failed') ,0) PaymentFailed
                ,ROW_NUMBER() OVER (
                  PARTITION BY rps.PlanId
                  ORDER BY 
                    CASE WHEN rpsh.RecurringType = 1 THEN rps.Id END DESC,
                    CASE WHEN rpsh.RecurringType <> 1 THEN rps.Id END ASC
                ) AS OCC
                ,CASE WHEN rpp.PurchaseItemTag LIKE '%commitment%' THEN 1 ELSE 0 END isCommitmentPlan
                , (case when rpsh.PaymentTrigger=2 then 'week'  
                when rpsh.PaymentTrigger=4 then 'month'  
                when rpsh.PaymentTrigger=7 then 'year'  
                when rpsh.PaymentTrigger=5 then 'quarter'   
                when rpsh.PaymentTrigger=8 then 'year'   
                when rpsh.PaymentTrigger=9 then 'month'  
                when rpsh.PaymentTrigger=10 then 'week'  
                when rpsh.PaymentTrigger=11 then 'one-off payment'  
                when rpsh.PaymentTrigger=1 then ''  
                end ) 'PaymentSchedule'
                ,Case When rpp.PricingMode = 1 AND prd.Iscommitmentsubscription = 1 AND (rpsh.Name = 'Subscription_Yearly' or rpsh.PaymentTrigger = 8)  Then prd.Unitprice*12 
                        When rpp.PricingMode = 1 Then prd.Unitprice 
                Else rpp.Amount End  as RecurringPaymentAmount
                FROM AllMemberships am
                INNER JOIN RecurringPaymentPlan rpp ON rpp.ForEntityId=am.MemberDocId AND (rpp.ProductId=am.ProductId OR 
                rpp.ProductId =(SELECT TOP 1 pd.DocId FROM Products_Links pl INNER JOIN Products_Links pl2 on pl.EntityId=pl2.EntityId AND pl.Docid !=pl2.DocId
                INNER JOIN Products_Default pd on pd.DocId =pl2.DocId WHERE pl.DociD=am.ProductId AND pd.Category='installment'))
                INNER JOIN Products_Default prd on rpp.ProductId = prd.DocId 
                INNER JOIN RecurringPaymentSchedule rps ON rps.PlanId=rpp.Id
                INNER JOIN RecurringPaymentScheme rpsh ON rpsh.Id=rpp.SchemeId),

                RecurringMembershipsCount AS (
                SELECT 
                  PlanGuid,
                  CAST (SUM(CASE WHEN ScheduleStatus = 3 THEN 1 ELSE 0 END) AS VARCHAR) +' of '+
                  CAST(COUNT(*) AS VARCHAR) +' completed' AS Progress,
                  (SELECT TOP 1 PaymentDate FROM RecurringMemberships rm2 
                    WHERE rm2.PlanGuid = rm.PlanGuid AND rm2.ScheduleStatus = 1 ORDER BY PaymentDate ASC
                  ) AS TopPaymentDateStatus1
                FROM RecurringMemberships rm
                GROUP BY PlanGuid)


                SELECT DISTINCT am.UserSyncId,am.UserId,am.MemberDocId,am.ProductId,am.LicenceOwner,am.MembershipPicUrl,am.MembershipSyncGuid,am.MembershipName
                ,am.Licencetype,Case When isCommitmentPlan = 1 then rm.PaymentSchedule Else am.Expirydateendingunit END Expirydateendingunit,am.Currency
                ,Case When isCommitmentPlan = 1 then rm.RecurringPaymentAmount 
                When IsCommitmentSubscription = 1 AND am.LicenceOwner = 0 Then am.UnitPrice*12 
                Else  am.UnitPrice END  UnitPrice
                ,am.[Status],am.StatusId,am.MemberLicenseDocId
                ,am.StartDate,am.EndDate
                ,CASE WHEN hm.[Historical]=1 OR am.IsUpgradeable=1 OR rm.RecurringType=1 THEN 0 ELSE am.IsRenewalWindow END IsRenewalWindow
                ,CASE WHEN hm.[Historical]=1 THEN 0 ELSE am.IsUpgradeable END IsUpgradeable
                ,CASE WHEN hm.[Historical]=1 THEN 0 ELSE am.IsDownloadAvailable END IsDownloadAvailable
                --,CASE WHEN am.StatusId=64 THEN 'Expired On ' + CONVERT(varchar(11), am.EndDate, 106) 
                --      WHEN rm.isCommitmentPlan=1 THEN 'Next Payment Date '+  CONVERT(varchar(11), rmc.TopPaymentDateStatus1, 106) 
                --      WHEN am.StatusId=62 AND am.IsRenewalWindow=1 AND rm.RecurringType=1 THEN 'Expires On ' + CONVERT(varchar(11), am.EndDate, 106) 
                --      WHEN am.IsRenewalWindow=0 AND rm.RecurringType=1 THEN 'Renews On '  + CONVERT(varchar(11), rm.PaymentDate, 106) 
                --      ELSE 'Renewal Due ' + CONVERT(varchar(11), am.EndDate, 106) END RenewalStatus
                ,CASE WHEN rm.RecurringType=1 THEN 'Renewal Due '  + CONVERT(varchar(11), rm.PaymentDate, 106) 
                      ELSE 'Renewal Due ' + CONVERT(varchar(11), am.EndDate, 106) END RenewalStatus
                ,CASE WHEN rm.RecurringType=2 THEN 'Next Payment '+  CONVERT(varchar(11), rmc.TopPaymentDateStatus1, 106) 
                      ELSE NULL END NextPaymentDate
                ,CASE WHEN rm.isCommitmentPlan=1 THEN 'Annual Subscription' ELSE ISNULL(rm.DisplayName,'Manual') END [Frequency]
                ,ISNULL(rm.PaymentFailed,0) PaymentFailed
                ,hm.[Historical],rm.PlanGuid,CASE WHEN rm.RecurringType=2 THEN rmc.Progress ELSE NULL END Progress
                ,rm.CustomerId
                FROM AllMemberships am 
                LEFT JOIN RecurringMemberships rm on rm.MemberLicenseDocId=am.MemberLicenseDocId AND rm.occ=1
                LEFT JOIN HistoricalMemberships hm on hm.MemberLicenseDocId=am.MemberLicenseDocId
                LEFT JOIN RecurringMembershipsCount rmc on rmc.PlanGuid=rm.PlanGuid
                WHERE NOT (ISNULL(rm.isCommitmentPlan,0)!=1 AND am.[Status]='Suspended')
                ORDER BY am.LicenceOwner,am.MemberDocId DESC;

                """;

            var parameters = new DynamicParameters();
            parameters.Add("@UserSyncId", request.SyncGuid, DbType.Guid);

            await using var reader = await _readRepository.Value
                .GetMultipleQueryAsync(sql, cancellationToken, parameters, null, "text");

            var hierarchies = new List<MembersHierarchiesWithMemberships>();
            var byEntityId = new Dictionary<int, MembersHierarchiesWithMemberships>();

            foreach (var h in await reader.ReadAsync<MembersHierarchiesWithMemberships>())
            {
                hierarchies.Add(h);
                byEntityId[h.EntityId] = h;
                // Each hierarchy starts with an empty list by design
            }

            foreach (var m in await reader.ReadAsync<MemberMemberships>())
            {
                if (byEntityId.TryGetValue(m.LicenceOwner, out var group))
                {
                    group.MemberMemberships.Add(m);
                }
                // If no matching hierarchy, ignore the orphan membership
            }

            return hierarchies;
        }
    }
}