using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.AssetLicenseDefination
{
    public class AssetLicenseDefinationHandler : IRequestHandler<AssetLicenseDefinationQuery, List<AssetMasterLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<AssetMasterLicenseDTO>> _readRepository;
        private IMediator _mediator;
        public AssetLicenseDefinationHandler(LazyService<IReadRepository<AssetMasterLicenseDTO>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<AssetMasterLicenseDTO>> Handle(AssetLicenseDefinationQuery request, CancellationToken cancellationToken)
        {
            string SQL = $@"select Ld.DocId as LicenseDocId,
							                            pd.DocId as ProductDocId,
                                                        pd.Location,
                                                        ld.DocId AS LicenseDocId,
                                                        cast(D.SyncGuid as uniqueidentifier) AS LicenseId,
                                                        pd.DocId AS ProductDocId,
							                            P_Id.ProductId,
                                                        ld.Reference,
                                                        ld.Benefits,
                                                        ld.[Description],
                                                        ld.HidePrice,
                                                        ld.Sequence,
                                                        ISNULL(c_Id.OwnerId,'')  as LicenceOwner,
                                                        ISNULL(pd.Code,'') AS Code,
                                                        pd.Name AS ProductName,
                                                        pd.[Description] AS ProductDescription,
                                                        pd.Category,
                                                        pd.Unitprice + isnull(fee.Value,0) AS Price,
                                                        pd.Currency,
                                                        pd.Availablequantity,
                                                        ISNULL(pd.Color,'') AS ProductColor,
                                                        ld.RenewalWindow AS LicenseType,
                                                        ld.ExpiryDateEndingUnit,
                                                        ld.ExpiryDateEndingValue,
                                                        ld.HideMembershipDuration,
                                                        ld.HideViewMoreAboutMembership,
                                                        ld.Classification,
                                                        ld.PriceOption,
                                                        pd.Unitprice as FromPrice,
                                                        ld.ToPrice,
                                                        ld.MembershipJourney,
                                                        ld.AlternateDisplayCurrency,
                                                        pd.IsSubscriptionEnabled,
                                                        pd.Recurringdescription,
                                                        pd.RecurringMandatory,
                                                        0 AS UpgradeType,
                                                        0 AS UpgradeId,
                                                        Expirydatestartingtype AS Expirydatestartingtype,
                                                        Expirydatestartingvalue AS Expirydatestartingvalue,
                                                        '' AS LicenseConfig
														from AssetTypesLicenseLink AL
														inner join assetTypes ATS on ATS.AssetTypeId = AL.AssetTypeId and ATS.RecordGuid = @AssetTypeId
			inner join License_Default LD on LD.DocId = AL.LicenseDocId
			inner join License_links LL on LL.Docid = LD.Docid
			inner join Products_Default PD on PD.DocId = LL.EntityId 
			inner join Document d ON d.DocId = ld.DocId
			inner join (select DocId, cast(SyncGuid as uniqueidentifier) as ProductId from Document ) P_Id on P_Id.DocId = pd.DocId
            left join (select DocId, SyncGuid as OwnerId from Document ) C_Id on C_Id.DocId = ld.LicenceOwner
            LEFT JOIN AssetLicenseAdditionalFees fee on fee.FeeLinkId = ld.docid
			
                            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", request.AssetTypeId, dbType: DbType.Guid);
            var result = (await _readRepository.Value.GetListAsync(SQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}