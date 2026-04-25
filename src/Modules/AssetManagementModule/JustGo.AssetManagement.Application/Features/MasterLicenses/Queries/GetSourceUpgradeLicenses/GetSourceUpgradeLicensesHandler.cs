using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.MasterLicenses.V2.Queries.GetAssetMasterLicenses;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetSourceUpgradeLicenses
{
    public class GetSourceUpgradeLicensesHandler : IRequestHandler<GetSourceUpgradeLicensesQuery, List<SourceUpgradeLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<SourceUpgradeLicenseDTO>> _readRepository;
        private IMediator _mediator;

        public GetSourceUpgradeLicensesHandler(LazyService<IReadRepository<SourceUpgradeLicenseDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<SourceUpgradeLicenseDTO>> Handle(GetSourceUpgradeLicensesQuery request, CancellationToken cancellationToken = default)
        {
            string SQL = @"
                             SELECT PD.DocId as ProductDocId,LD.DocId as LicenseDocId, PD.UnitPrice, COALESCE(fee.Value, 0) AS FeeValue ,'Source' Type,0 SourceProductDocId,0 as SourceLicenseDocId,PD.Ownerid
                            FROM License_Default LD
                            INNER JOIN License_Links LL ON LD.DocId = LL.DocId
                            INNER JOIN Products_Default PD ON LL.EntityId = PD.DocId
                            LEFT JOIN AssetLicenseAdditionalFees fee ON fee.FeeLinkId = LD.DocId
                            WHERE LD.DocId IN (
                                SELECT DISTINCT SourceUpgradeLicense 
                                FROM AssetTypesLicenseLink 
                                WHERE SourceUpgradeLicense>0--  = @LicenseDocId 
								AND LicenseDocId = @LicenseDocId
                                --AND LicenseType = 1
                            )
                            
                            UNION
                            
                            SELECT 
                                PD.DocId AS ProductDocId,LD.DocId as LicenseDocId,
                                PD.UnitPrice,
                                fee.Value AS FeeValue,
                                'Upgrade' AS Type,
                                PD2.DocId AS SourceProductDocId,
                            	AST.SourceUpgradeLicense AS SourceLicenseDocId,PD.Ownerid
                            FROM License_Default LD
                            INNER JOIN License_Links LL ON LD.DocId = LL.DocId
                            INNER JOIN Products_Default PD ON LL.EntityId = PD.DocId
                            INNER JOIN AssetTypesLicenseLink AST ON AST.LicenseDocId = LD.DocId
                            LEFT JOIN AssetLicenseAdditionalFees fee ON fee.FeeLinkId = LD.DocId
                            INNER JOIN Products_Links PL ON PL.EntityId = AST.SourceUpgradeLicense
                            INNER JOIN Products_Default PD2 ON PD2.DocId = PL.DocId
                            WHERE AST.SourceUpgradeLicense > 0 AND LicenseDocId = @LicenseDocId
                              --AND AST.LicenseType = 1


  --select *from AssetTypesLicenseLink";
            var queryParameters = new DynamicParameters();

            queryParameters.Add("@LicenseDocId", request.LicenseDocId, dbType: DbType.Int32);
            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
