using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.MasterLicenses.Queries.GetLicenses;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetCartAssetLicensesQuery
{
    public class GetCartAssetLicenseshandler : IRequestHandler<GetCartAssetLicensesQuery, List<AssetCartLicenseDTO>>
    {
        private readonly LazyService<IReadRepository<AssetCartLicenseDTO>> _readRepository;
        private IMediator _mediator;
        private readonly IUtilityService _utilityService;
        public GetCartAssetLicenseshandler(LazyService<IReadRepository<AssetCartLicenseDTO>> readRepository, IMediator mediator, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
        }
        public async Task<List<AssetCartLicenseDTO>> Handle(GetCartAssetLicensesQuery request, CancellationToken cancellationToken)
        {
            string dataSQL = $@"select pd.DocId as ProductDocId,D.SyncGuid ProductId,DL.SyncGuid as LicenseId ,pd.Name,pd.Unitprice 
                                from AssetLicenses AL inner join AssetStatus S on S.AssetStatusId = AL.StatusId
                                Inner join AssetRegisters AR on AR.AssetId = AL.AssetId
                                inner join Products_Default PD on PD.DocId = AL.ProductId
								inner join Document D on D.DocId = PD.DocId
								inner join Products_Links PL on PL.DocId = PD.DocId
								inner join License_Default LD on LD.DocId = PL.Entityid
								inner join Document DL on DL.DocId = LD.DocId
                                WHERE AR.RecordGuid = @AssetRegisterId  AND S.Name ='Pending Payment'";
            var queryParameters = new DynamicParameters();
           
            queryParameters.Add("@AssetRegisterId", request.AssetRegisterId, dbType: DbType.Guid);
            var result = (await _readRepository.Value.GetListAsync(dataSQL, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
