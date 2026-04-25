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
    public class GetSourceUpgradeLicensesHandlerV2 : IRequestHandler<GetSourceUpgradeLicensesQueryV2, List<SourceUpgradeLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<SourceUpgradeLicenseDTO>> _readRepository;
        private IMediator _mediator;

        public GetSourceUpgradeLicensesHandlerV2(LazyService<IReadRepository<SourceUpgradeLicenseDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<SourceUpgradeLicenseDTO>> Handle(GetSourceUpgradeLicensesQueryV2 request, CancellationToken cancellationToken = default)
        {
            string SQL = @"
                            SELECT 
                                PD.DocId AS ProductDocId,
                                LD.DocId AS LicenseDocId,
                                PD.UnitPrice,
                                COALESCE(fee.Value, 0) AS FeeValue,
                                'Source' AS Type,
                                0 AS SourceProductDocId,
                                0 AS SourceLicenseDocId,
                                PD.Ownerid
                            FROM 
                                License_Default LD
                                INNER JOIN License_Links LL ON LD.DocId = LL.DocId
                                INNER JOIN Products_Default PD ON LL.EntityId = PD.DocId
                                LEFT JOIN AssetLicenseAdditionalFees fee ON fee.FeeLinkId = LD.DocId
                            WHERE 
                                LD.DocId IN (
                                    SELECT DISTINCT LicenseDocId 
                                    FROM AssetTypesLicenseLink 
                                    WHERE IsUpgradable = 1
                                )
                            
                            UNION
                            
                            -- Section 2: Upgrade Licenses
                            SELECT 
                                PD.DocId AS ProductDocId,
                                AST.LicenseDocId AS LicenseDocId,
                                PD.UnitPrice,
                                fee.Value AS FeeValue,
                                'Upgrade' AS Type,
                                PD2.DocId AS SourceProductDocId,
                                ALU.LicenseDocId AS SourceLicenseDocId,
                                PD.Ownerid
                            FROM 
                                License_Links LL --ON LD.DocId = LL.DocId
                                INNER JOIN Products_Default PD ON LL.EntityId = PD.DocId
                                INNER JOIN AssetTypesLicenseLink AST ON AST.LicenseDocId = LL.DocId
                                LEFT JOIN AssetLicenseAdditionalFees fee ON fee.FeeLinkId = AST.LicenseDocId
                                INNER JOIN AssetLicenseUpgrade ALU on ALU.UpgradeLicenseDocId = AST.LicenseDocId
                            	INNER JOIN   License_Links LL2 ON LL2.DocId = ALU.LicenseDocId
                            	INNER JOIN Products_Default PD2 on PD2.DocId = LL2.Entityid
                            	WHERE AST.LicenseDocId = @LicenseDocId
                            
";
            var queryParameters = new DynamicParameters();

            queryParameters.Add("@LicenseDocId", request.LicenseDocId, dbType: DbType.Int32);
            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
