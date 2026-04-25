using AuthModule.Domain.Entities;
using Azure.Core;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.AssetReportDTO;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.AssetReports.Helper;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;
using System.Reflection;
using System.Threading;

namespace JustGo.AssetManagement.Application.Features.AssetReports.Commands.DownloadReport
{
    public class DownloadAssetReportHandler : IRequestHandler<DownloadAssetReportQuery, DownloadAssetReportResponse>
    {
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IMediator _mediator;
        private readonly IUtilityService _utilityService;
        public DownloadAssetReportHandler(
            ISystemSettingsService systemSettingsService, IReadRepositoryFactory readRepository, IMediator mediator, IUtilityService utilityService, IWriteRepositoryFactory writeRepository)
        {
            _systemSettingsService = systemSettingsService;
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
            _writeRepository = writeRepository;
        }
        public async Task<DownloadAssetReportResponse> Handle(DownloadAssetReportQuery request, CancellationToken cancellationToken)
        {

           var report= await GetReportData(request, cancellationToken);
             var isSuccess = await DownloadReport(request,report, cancellationToken);
            if(isSuccess)
            {
                return new DownloadAssetReportResponse
                {
                    IsDownloaded = true,
                    Message = "Report download initiated successfully."
                };
            }
            else
            {
                return new DownloadAssetReportResponse
                {
                    IsDownloaded = false,
                };
            }

        }

        private async Task<AssetReportResponseDTO> GetReportData(DownloadAssetReportQuery request, CancellationToken cancellationToken)
        {
            string sql = @"
                            SELECT TOP 1
	                           ReportName,
	                           ReportPath,
	                           RecordGuid as ReportId,
                               ReportType,
                               ReportId AS Id,
							   OwnerId
                            FROM
                                 AssetReports where RecordGuid = @ReportId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ReportId", request.ReportId);

            var result = await _readRepository.GetLazyRepository<AssetReportResponseDTO>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            return result;
        }
        private async Task<bool> DownloadReport(DownloadAssetReportQuery request, AssetReportResponseDTO report, CancellationToken cancellationToken)
        {
            var hostsystemid = await _systemSettingsService.GetSystemSettingsByItemKey(
                                 "CLUBPLUS.HOSTSYSTEMID", cancellationToken);

            var busConString = await _systemSettingsService.GetSystemSettingsByItemKey(
                                "SYSTEM.REPORT.SERVICE.BUS", cancellationToken);
            
            int assetLicenseId = _mediator.Send(new GetIdByGuidQuery() {  Entity = AssetTables.AssetLicenses, RecordGuids = new List<string>() { request.EntityId }}).Result[0];
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            ReportBus bus = new ReportBus(busConString);
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("EntityId", assetLicenseId);
            string ReportEmailHistoryId = "";

            ReportEmailHistoryId = await SaveReportEmailHistory(currentUserId, report, cancellationToken);


            var data = bus.SendMessage<object>(
                           "reportexporttopic",
                           new
                           {
                               reportId = request.ReportId,
                               method = "ExportCoreReport",//"ExportCoreReport",
                               hostId = hostsystemid,
                               reportPath = report.ReportPath, //"/Core Reports/EMAILS/Email Usage by Year",
                               reportParams = args,
                               format = "pdf",
                               connectionId = request.ConnectionId,
                               userId = currentUserId,
                               reportEmailHistoryId = ReportEmailHistoryId,
                               ReportType = 2
                           }, true);

            return true;
        }


        private async Task<string> SaveReportEmailHistory(int userId, AssetReportResponseDTO report, CancellationToken cancellationToken)
        {
            var hostId = await _systemSettingsService.GetSystemSettingsByItemKey("CLUBPLUS.HOSTSYSTEMID", cancellationToken);
            string syncGuid = Guid.NewGuid().ToString();
            string token = Guid.NewGuid().ToString().Replace("-", "") + Guid.NewGuid().ToString().Replace("-", "");

            string sql = @"
                INSERT INTO ReportEmailHistory (ReportId, UserId, HostId, Token, SyncGuid, OwningEntityId, StartTime)
                VALUES (@ReportId, @UserId, @HostId, @Token, @SyncGuid, @OwningEntityId, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT) AS ReportEmailHistoryId;";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ReportId", report.Id, DbType.Int32);
            queryParameters.Add("@HostId", hostId, DbType.String);
            queryParameters.Add("@UserId", userId, DbType.Int32);
            queryParameters.Add("@OwningEntityId", report.OwnerId, DbType.Int32);
            queryParameters.Add("@SyncGuid", syncGuid, DbType.String);
            queryParameters.Add("@Token", Guid.NewGuid().ToString().Replace("-", "") + Guid.NewGuid().ToString().Replace("-", ""), DbType.String);

            var reportEmailHistoryId = await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                sql,
                cancellationToken,
                queryParameters,
                null,
                "text");

            return syncGuid;
        }


    }
}