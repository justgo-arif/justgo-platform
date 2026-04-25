using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetAssetLicenseById
{
    public class GetAssetLicenseByIdHandler : IRequestHandler<GetAssetLicenseByIdQuery, List<AssetLicenseResultDTO>>
    {
        private readonly LazyService<IReadRepository<AssetLicenseResultDTO>> _readRepository;
        public GetAssetLicenseByIdHandler(LazyService<IReadRepository<AssetLicenseResultDTO>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<AssetLicenseResultDTO>> Handle(GetAssetLicenseByIdQuery request, CancellationToken cancellationToken)
        {
            string dataSql = @"SELECT 
                            	al.EndDate AS EndDate,    
                            	pd.[Name] AS [Name],
                            	pd.OwnerId AS OwnerId,
                            	ll.DocId AS LicenseId,
                            	pd.DocId AS ProductDocId,
                            	ATL.LicenseType,
                            	al.StatusId,
                            	ast.Name as LicenseStatus
                            FROM AssetLicenses al
                            INNER JOIN AssetRegisters ar on ar.AssetId = al.AssetId
                            INNER JOIN AssetStatus ast ON ast.AssetStatusId = al.StatusId
                            INNER JOIN Products_Default pd ON pd.DocId = al.ProductId
                            INNER JOIN License_Links ll ON ll.EntityId = pd.DocId
                            INNER JOIN AssetTypesLicenseLink ATL on ATL.LicenseDocId= ll.DocId
                            WHERE ar.RecordGuid = @AssetRegisterId";

            var parameters = new DynamicParameters();
            parameters.Add("@AssetRegisterId", request.AssetRegisterId);

            var data = (await _readRepository.Value.GetListAsync(dataSql, cancellationToken, parameters, null, "text")).ToList();
            return data;
        }
    }
}