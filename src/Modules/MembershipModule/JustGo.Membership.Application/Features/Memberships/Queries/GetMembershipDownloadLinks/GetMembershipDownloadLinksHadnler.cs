using System.Data;
using Azure.Core;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Domain.Entities;
using Microsoft.AspNetCore.Http;
using YamlDotNet.Core.Tokens;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMembershipDownloadLinks
{
    public class GetMembershipDownloadLinksHadnler : IRequestHandler<GetMembershipDownloadLinksQuery, MembershipDownloadLinks?>
    {
        private readonly LazyService<IReadRepository<MembershipDownloadLinks>> _readRepository;
        private readonly LazyService<IReadRepository<string>> _readOptRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public GetMembershipDownloadLinksHadnler(LazyService<IReadRepository<MembershipDownloadLinks>> readRepository, LazyService<IReadRepository<string>> readOptRepository, IHttpContextAccessor httpContextAccessor)
        {
            _readRepository = readRepository;
            _readOptRepository = readOptRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<MembershipDownloadLinks?> Handle(GetMembershipDownloadLinksQuery request, CancellationToken cancellationToken)
        {
            const string sql = """
                DECLARE @DownloadPath NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey = 'MEMBERSHIP.PDF_DOWNLOAD_REPORT_PATH')
                DECLARE @siteaddress NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey ='SYSTEM.SITEADDRESS')

                DECLARE @PdfUrl NVARCHAR(MAX)
                DECLARE @GoogleWalletURL NVARCHAR(MAX)
                DECLARE @AppleWalletURL NVARCHAR(MAX)

                SELECT @GoogleWalletURL=(CASE WHEN g.PayLoad IS NULL THEN NULL ELSE 'https://pay.google.com/gp/v/save/' + g.PayLoad END) 
                ,@AppleWalletURL=replace(replace('#WebsiteAddress/ApplePass/DownloadPKPass?Id=#AppleWalletGuid','#AppleWalletGuid',cast(a.AppleWalletGuid as nvarchar(max))),'#WebsiteAddress',@siteaddress)
                FROM EventQueue eq 
                INNER JOIN Document d on d.DocId=eq.EntityDocId
                LEFT JOIN [GoogleWalletInfo] g on g.EventQueueId=eq.EventId
                LEFT JOIN [AppleWalletInfo] a on a.EventQueueId=eq.EventId
                WHERE d.SyncGuid=@syncguid

                SELECT @PdfUrl=CASE WHEN ISNULL(@DownloadPath,'')='' THEN '' ELSE @siteaddress+'/repository.mvc/printdocument?path='+@DownloadPath+'&format=PDF&docId='+CAST(d.DocId AS VARCHAR)+'&repoName=Members%20Licence' END 
                FROM Document d WHERE d.SyncGuid=@syncguid

                SELECT @PdfUrl PdfUrl,@GoogleWalletURL GoogleWalletURL,@AppleWalletURL AppleWalletURL
                """;

            var parameters = new DynamicParameters();
            parameters.Add("@syncguid", request.SyncGuid, DbType.Guid);

            // Single row result; use GetAsync with commandType text
            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, parameters, null, "text");

            //implemented qrCoreUrl business for scb mobile app
            var appType = _httpContextAccessor.HttpContext.Request.Headers["X-App-Type"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(appType) && appType.ToLower().Equals("mobile"))
            {
                try
                {
                    string qrLinkSql = GetWalletQrUrl();
                    if (result != null)result.qrCodeUrl = await _readOptRepository.Value.GetAsync(qrLinkSql, cancellationToken, parameters, null, "text");
                }catch {}
               
            }


            return result;
        }

        private Func<string> GetWalletQrUrl = () => @"
            SELECT 
                CONCAT(
                    (SELECT [Value] 
                     FROM SystemSettings 
                     WHERE ItemKey = 'SYSTEM.AZURESTOREROOT'),
                    '/002/',
                    ss.Value,
                    '/',
                    REPLACE(uq.QrImageUrl, '\', '/')
                ) AS qrCodeUrl
            FROM SystemSettings ss
            INNER JOIN EventQueue eq ON 1 = 1
            INNER JOIN Document d ON d.DocId = eq.EntityDocId
            LEFT JOIN UserQrCodes uq ON uq.EventQueueId = eq.EventId
            WHERE 
                ss.ItemKey = 'CLUBPLUS.HOSTSYSTEMID'
                AND d.SyncGuid = @syncguid;";

    }
}