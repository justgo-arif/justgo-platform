using Dapper;
using JustGo.AssetManagement.Application.DTOs.AssetReportDTO;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.CustomMediator;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.AssetReports.Queries.GetAssetReports
{
    public class GetAssetReportsHandler : IRequestHandler<GetAssetReportsQuery, List<AssetReportResponseDTO>>
    {
        private readonly LazyService<IReadRepository<AssetReportResponseDTO>> _readRepository;
        private readonly IMediator _mediator;


        public GetAssetReportsHandler(LazyService<IReadRepository<AssetReportResponseDTO>> readRepository,
        IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }
        public async Task<List<AssetReportResponseDTO>> Handle(GetAssetReportsQuery request, CancellationToken cancellationToken)
        {
           int assetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() {request.AssetRegisterId.ToString() } }))[0];
            string sql = @"
                            SELECT
                               al.recordguid as EntityId,
	                           ar.ReportName,
	                           ar.ReportPath,
	                           ar.RecordGuid as ReportId
                            FROM
                                AssetLicenses al
                                INNER JOIN Products_Default pd ON pd.docid = al.productid
                            	INNER JOIN AssetReports AR on AR.OwnerId = PD.Ownerid AND ReportType = Al.LicenseType
                            WHERE
                            al.StatusId = 8 and AR.OwnerId = pd.Ownerid
                            and al.AssetId = @AssetId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ReportType", request.EntityType, dbType: DbType.Int32);
            queryParameters.Add("@AssetId", assetId, dbType: DbType.Int32);

            var data = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text"));

            return (List<AssetReportResponseDTO>)data;
        }
    }
}