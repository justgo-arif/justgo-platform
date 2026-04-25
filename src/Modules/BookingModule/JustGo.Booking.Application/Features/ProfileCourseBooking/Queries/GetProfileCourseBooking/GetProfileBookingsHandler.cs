using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.ProfileBookingDtos;
using System.Data;
using System.Data.SqlTypes;

namespace JustGo.Booking.Application.Features.ProfileCourseBooking.Queries.GetProfileCourseBooking
{
    public class GetProfileBookingHandler : IRequestHandler<GetProfileBookingsQuery, List<ProfileCourseBookingGroupDto>>
    {
        private readonly LazyService<IReadRepository<ProfileCourseBookingDto>> _readRepository;

        public GetProfileBookingHandler(LazyService<IReadRepository<ProfileCourseBookingDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<ProfileCourseBookingGroupDto>> Handle(GetProfileBookingsQuery request, CancellationToken cancellationToken)
        {
            const string sql = """
                DECLARE @NGBSynguid NVARCHAR(36) =(SELECT TOP 1 d.SyncGuid FROM merchantprofile_default mpd INNER JOIN Document d on d.docid=mpd.docid
                WHERE mpd.DocId NOT IN (SELECT mpl.DocId FROM merchantprofile_links mpl) AND mpd.Name != 'JustGo' and mpd.Merchanttype = 'NGB')
                DECLARE @siteaddress NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey ='SYSTEM.SITEADDRESS')
                DECLARE @organisationname NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey ='ORGANISATION.NAME')
                DECLARE @RepoPicurlFromat NVARCHAR(1000)= '/store/download?f=%s&t=repo&p=%d&p1=&p2=%d';
                DECLARE @DownloadPath NVARCHAR(MAX)=(SELECT TOP 1 JSON_VALUE(Config, '$.reportPath') FROM Widget WHERE WorkbenchId=9 AND [Name]='Widgets.CartWidget')
                DECLARE @DefaultLogo NVARCHAR(MAX) = (SELECT TOP 1  Value FROM systemsettings  WHERE itemkey = 'ORGANISATION.LOGO');

                ; With MemberEvents AS(
                SELECT d.Docid,ed.DocId EventDocId,CAST(d.SyncGuid AS uniqueidentifier) AS SyncGuid,ed.EventName,cbd.CourseName
                ,CONVERT(varchar, DATEADD(SECOND, X.gm_offset, ed.StartDate),120) AS StartDateTime
                ,CASE WHEN ISNULL(ed.[Location],'') NOT IN ('','Virtual') THEN FORMATMESSAGE(@RepoPicurlFromat,ed.[Location], ed.DocId, ed.RepositoryId) ELSE '' END EventPicUrl
                ,CASE WHEN ed.Isrecurring=1 THEN ed.Alternatemessagefordate ELSE FORMAT(CONVERT(date, DATEADD(SECOND, X.gm_offset, ed.StartDate)), 'ddd · dd MMM yyyy')+ ' · ' +FORMAT(CONVERT(datetime, StartTime, 108), 'hh:mm tt') + ' ' + X.abbreviation END AS [FormattedDateTime]
                ,(
                    STUFF(
                      (
                        SELECT 
                          ', ' + value
                        FROM (
                          SELECT NULLIF(ed.Address1, '') AS value
                          UNION ALL SELECT NULLIF(ed.Address2, '')
                          UNION ALL SELECT NULLIF(ed.Town, '')
                          UNION ALL SELECT NULLIF(ed.PostCode, '')
                          UNION ALL SELECT NULLIF(ed.County, '')
                          UNION ALL SELECT NULLIF(lkp.Field_6, '')
                        ) AS vals
                        WHERE value IS NOT NULL
                        FOR XML PATH(''), TYPE
                      ).value('.', 'NVARCHAR(MAX)')
                      , 1, 2, '' -- Remove leading separator
                    )
                  ) AS AddressSummary
                ,CASE WHEN g.PayLoad IS NULL THEN NULL ELSE 'https://pay.google.com/gp/v/save/' + g.PayLoad END AS GoogleWalletURL
                ,replace(replace('#WebsiteAddress/ApplePass/DownloadPKPass?Id=#AppleWalletGuid','#AppleWalletGuid',cast(a.AppleWalletGuid as nvarchar(max))),'#WebsiteAddress',@siteaddress) AS AppleWalletURL
                ,@siteaddress+'/Report.mvc/GetStandardOutputReport?reportModule=Finance&format=PDF&reportType=Default&reportParameters=DocId|'+CAST(prd.DocId AS VARCHAR)+';MerchantID|0;'AS ReceiptUrl
                --,CASE WHEN DATEADD(SECOND, X.gm_offset, ed.StartDate) < GETDATE() THEN 1 ELSE 0 END AS PastEvent
                ,0 AttachmentsCount
                ,CASE WHEN CAST(DATEADD(SECOND, X.gm_offset, ed.StartDate) AS date) = CAST(GETDATE() AS date) THEN 'Today'
                WHEN DATEADD(SECOND, X.gm_offset, ed.StartDate) > GETDATE() THEN 'Upcoming' ELSE FORMAT(CAST(DATEADD(SECOND, X.gm_offset, ed.StartDate) AS date), 'MMMM yyyy') END AS EventPeriod
                ,CASE 
                    WHEN ed.OwningEntityid = 0 
                    THEN @organisationname 
                    ELSE cd.ClubName 
                END as ClubName
                ,CASE 
                   WHEN ed.OwningEntityid = 0  THEN CONCAT( 'Store/Download?f=', @DefaultLogo ,'&t=OrganizationLogo' )
                    WHEN cd.Location = 'Virtual' OR ISNULL(cd.Location,'') = '' THEN ''  
                    ELSE Concat('store/download?f=', cd.Location, '&t=repo&p=', cd.DocId,'&p1=&p2=2')  
                END AS ClubImageUrl
                ,ISNULL(dc.SyncGuid,@NGBSynguid) ClubGuid
                FROM [User] u
                INNER JOIN CourseBooking_Links cbl ON cbl.Entityid=u.MemberDocId
                INNER JOIN CourseBooking_Default cbd ON cbd.DocId=cbl.DocId
                INNER JOIN Events_Default ed on ed.DocId=cbd.CourseDocId
                INNER JOIN Document d ON cbd.DocId=d.DocId
                INNER JOIN ProcessInfo pr on pr.PrimaryDocId=d.DocId
                INNER JOIN [State] s on s.StateId=pr.CurrentStateId
                LEFT JOIN Lookup_2 lkp on lkp.Field_5=ed.Country
                OUTER APPLY 
                (select top  1 gm_offset,abbreviation from Timezone where time_start <=  cast(DATEDIFF(HOUR,'1970-01-01 00:00:00', ed.[StartDate]) as bigint)*60*60
                and zone_id=ed.Timezone order by time_start desc) as X
                LEFT JOIN EventQueue eq on eq.EntityDocId=cbd.Docid
                LEFT JOIN [GoogleWalletInfo] g on g.EventQueueId=eq.EventId
                LEFT JOIN [AppleWalletInfo] a on a.EventQueueId=eq.EventId
                LEFT JOIN PaymentReceipts_Default prd on prd.PaymentId=cbd.PaymentReceiptId
                LEFT JOIN Clubs_Default cd ON cd.DocId = ed.OwningEntityid
                LEFT JOIN Document dc on dc.DocId=cd.DocId
                WHERE u.UserSyncId=@UserSyncId AND s.[Name]!='Cancelled' AND ISNULL(ed.LocationType,'')!='shop'
                )


                SELECT me.DocId,me.EventDocId,me.SyncGuid,me.EventName,me.CourseName,me.EventPicUrl,me.FormattedDateTime,me.StartDateTime,me.AddressSummary
                ,me.GoogleWalletURL,me.AppleWalletURL,me.ReceiptUrl,CASE WHEN me.EventPeriod IN ('Upcoming','Today') THEN 0 ELSE 1 END AS PastEvent
                ,me.EventPeriod,ISNULL(me.ClubName, '') AS ClubName,me.ClubImageUrl,me.ClubGuid FROM MemberEvents me
                """;

            const string attachmentsSql = """
                SELECT DISTINCT cbd.DocId,'/store/download?f='+field.[Value]+'&t=fieldmanagementattach&p=-1&p1='+CAST(item.FieldId AS VARCHAR)+'&p2=-1' AttachmentUrl
                FROM [User] u
                INNER JOIN CourseBooking_Links cbl ON cbl.Entityid=u.MemberDocId
                INNER JOIN CourseBooking_Default cbd ON cbd.DocId=cbl.DocId 
                INNER JOIN dbo.Products_DataCaptureItems AS pdc ON pdc.DocId = cbd.ProductDocId
                CROSS APPLY OPENJSON(pdc.Config, '$.items') WITH (Class nvarchar(100),FieldId int) AS item
                INNER JOIN ExNGBEvent_FieldSet_Largetext field on field.FieldId=item.FieldId AND cbd.DocId=field.DocId
                WHERE u.UserSyncId=@UserSyncId AND  item.Class = 'MA_Attachment' 
                AND EXISTS (SELECT 1 FROM ExNGBEvent_FieldSet_Largetext WHERE DocId=cbd.Docid AND FieldId=field.FieldId AND ISNULL([Value],'')!='')
                UNION
                SELECT DISTINCT cbd.DocId,'/store/download?f='+field.[Value]+'&t=fieldmanagementattach&p=-1&p1='+CAST(item.FieldId AS VARCHAR)+'&p2=-1' AttachmentUrl
                FROM [User] u
                INNER JOIN CourseBooking_Links cbl ON cbl.Entityid=u.MemberDocId
                INNER JOIN CourseBooking_Default cbd ON cbd.DocId=cbl.DocId 
                INNER JOIN dbo.Products_DataCaptureItems AS pdc ON pdc.DocId = cbd.ProductDocId
                CROSS APPLY OPENJSON(pdc.Config, '$.items') WITH (Class nvarchar(100),FieldId int) AS item
                INNER JOIN ExClubEvent_FieldSet_Largetext field on field.FieldId=item.FieldId AND cbd.DocId=field.DocId
                WHERE u.UserSyncId=@UserSyncId AND  item.Class = 'MA_Attachment' 
                AND EXISTS (SELECT 1 FROM ExClubEvent_FieldSet_Largetext WHERE DocId=cbd.Docid AND FieldId=field.FieldId AND ISNULL([Value],'')!='') 

                """;


            var parameters = new DynamicParameters();
            parameters.Add("@UserSyncId", request.Id, DbType.Guid);

            var result = await _readRepository.Value.GetListAsync<ProfileCourseBookingDto>(sql, parameters, null, "text", cancellationToken);
            var attachments = await _readRepository.Value.GetListAsync<ProfileCourseBookingAttachmentDto>(attachmentsSql, parameters, null, "text", cancellationToken);

            foreach (var item in result)
            {
                item.Attachments = attachments
                    .Where(a => a.DocId == item.DocId)
                    .Select(a => a.AttachmentUrl)
                    .ToList();
            }

            var grouped = result
                .GroupBy(b => b.EventPeriod)
                .Select(g => new ProfileCourseBookingGroupDto
                {
                    EventPeriod = g.Key,
                    ProfileCourseBookings = g.ToList()
                })
                .ToList();

            return grouped;
        }
    }
}
