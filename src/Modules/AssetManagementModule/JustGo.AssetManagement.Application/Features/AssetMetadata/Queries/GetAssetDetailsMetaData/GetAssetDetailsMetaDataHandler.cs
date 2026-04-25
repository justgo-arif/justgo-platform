using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AdditionalFieldsDTO;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetStatusMetaData;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetDetailsMetaData
{
    public class GetAssetDetailsMetaDataHandler : IRequestHandler<GetAssetDetailsMetaDataQuery, List<FormModel>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;

        public GetAssetDetailsMetaDataHandler(IReadRepositoryFactory readRepository,
                                              IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService; 
        }

        public async Task<List<FormModel>> Handle(GetAssetDetailsMetaDataQuery request, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetTypeId", request.AssetTypeId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
                    .GetAsync($@"Select * from AssetTypes Where RecordGuid = @AssetTypeId", cancellationToken, queryParameters, null, "text");

            var AssetRegistrationConfig = JsonConvert.DeserializeObject<AssetRegistrationConfig>(assetType.AssetRegistrationConfig);

            if (AssetRegistrationConfig.Steps.AdditionalDetails.Config.AdditionalRegistrationForms.Any())
            {


                var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
                string sql = $@"exec [GetFieldMgtData_AssetGrid] @UserId = {currentUserId}, @FormIds = '"+string.Join(",", AssetRegistrationConfig.Steps.AdditionalDetails.Config.AdditionalRegistrationForms) + "'";
                queryParameters = new DynamicParameters();
                var results =(await _readRepository.GetLazyRepository<FormFieldValue>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
                return results
                       .GroupBy(r => r.FormName ).Select(r => new FormModel()
                {
                    FormName = r.Key,
                    Fields = r.GroupBy(r => r.FieldId)
                             .Select(r => new Field()
                             {
                                 FieldId = r.FirstOrDefault().SyncGuid,
                                 FieldCaption = r.FirstOrDefault().FieldCaption,
                                 DataType = r.FirstOrDefault().DataType,
                                 DisplayType = r.FirstOrDefault().DisplayType,
                                 FieldValues = r.Select(v => new FieldValue()
                                 {
                                     Name = v.Name,
                                     Value = v.Value,
                                 }).ToList()
                             }
                             ).ToList()
                }).ToList();

            }
            else
            {
                return new List<FormModel>();
            }
        }

    }
}
