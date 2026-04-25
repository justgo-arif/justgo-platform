using Dapper;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.FindAssets
{
    public class FindAssetQueryHandler : IRequestHandler<FindAssetQuery, List<FindAssetsDto>>
    {
        private readonly IReadRepositoryFactory _readRepository;

        private const string SqlQuery = """
                                        DECLARE @BaseUrl NVARCHAR(500);
                                        SELECT @BaseUrl = [Value]
                                        FROM SystemSettings
                                        WHERE ItemKey = 'SYSTEM.SITEADDRESS';
                                        
                                        SELECT AssetId, AssetReference, AssetName, ar.RecordGuid,ast.Name as HorseStatus INTO #AssetSearchTempData
                                            FROM AssetRegisters ar
                                        INNER JOIN AssetStatus ast ON ar.StatusId=ast.AssetStatusId AND ast.Type =1
                                        WHERE AssetTypeId = 1
                                            AND (AssetReference LIKE '%' + @search_term + '%'
                                                OR AssetName LIKE '%' + @search_term + '%');
                                        
                                        
                                        SELECT 
                                        	fa.AssetId,
                                        	pd.Name AS CertificateName,
                                        	atl.LicenseType
                                        	INTO #AssetLicensesTempData
                                        FROM #AssetSearchTempData fa
                                        INNER JOIN AssetLicenses AST ON fa.AssetId = AST.AssetId
                                        INNER JOIN Products_Default pd ON pd.DocId = AST.ProductId
                                        INNER JOIN Products_Links pl ON pl.DocId = pd.DocId
                                        INNER JOIN License_Default ld on ld.DocId = pl.Entityid
                                        INNER JOIN AssetTypesLicenseLink atl ON atl.LicenseDocId = ld.DocId
                                        WHERE atl.LicenseType IN (1,2)
                                        
                                        ;WITH AssetPrimaryCertCTE AS (
                                            SELECT 
                                                AssetId, 
                                                MAX(CertificateName) AS AssetPrimaryCertificate
                                            FROM #AssetLicensesTempData
                                            WHERE LicenseType = 1
                                            GROUP BY AssetId
                                        ),
                                        AssetAdditionalCertCTE AS (
                                            SELECT 
                                                AssetId,
                                                STRING_AGG(CertificateName, ', ') AS AssetAdditionalCertificates
                                            FROM #AssetLicensesTempData
                                            WHERE LicenseType = 2
                                            GROUP BY AssetId
                                        )
                                        
                                        SELECT 
                                            fa.HorseStatus,
                                            fa.AssetReference AS HorseId, 
                                            fa.AssetName AS HorseName,
                                            CASE 
                                                WHEN ai.AssetImage IS NULL OR LTRIM(RTRIM(ai.AssetImage)) = '' 
                                                    THEN ''
                                                ELSE @BaseUrl + 'store/downloadPublic?f=' + ai.AssetImage 
                                                        + '&t=assetattachment'
                                                        + '&p='  + CAST(fa.RecordGuid AS NVARCHAR(50))
                                                        + '&p1=' + CAST(ai.RecordGuid AS NVARCHAR(50))
                                                        + '&p2=-1'
                                            END AS Image,
                                            ISNULL(ap.AssetPrimaryCertificate, '') AS PrimaryLicense,
                                            ISNULL(aa.AssetAdditionalCertificates, '') AS AdditionalLicense
                                        FROM #AssetSearchTempData fa
                                        LEFT JOIN AssetImages ai 
                                            ON fa.AssetId = ai.AssetId AND ai.IsPrimary = 1
                                        LEFT JOIN AssetPrimaryCertCTE ap 
                                            ON fa.AssetId = ap.AssetId
                                        LEFT JOIN AssetAdditionalCertCTE aa 
                                            ON fa.AssetId = aa.AssetId
                                        ORDER BY 
                                            fa.AssetName ASC,
                                            fa.AssetReference ASC;
                                        
                                        drop table #AssetSearchTempData;
                                        drop table #AssetLicensesTempData;
                                        """;

        public FindAssetQueryHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<FindAssetsDto>> Handle(FindAssetQuery request,
            CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("search_term", request.SearchTerm);
            var repo = _readRepository.GetRepository<FindAssetsDto>();
            var item = (await repo.GetListAsync(SqlQuery, cancellationToken, queryParameters, null, QueryType.Text)).ToList();
            return item;
        }
    }
}
