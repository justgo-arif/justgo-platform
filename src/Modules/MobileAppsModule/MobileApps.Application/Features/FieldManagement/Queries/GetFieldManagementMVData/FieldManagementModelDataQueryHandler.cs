using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementMVData
{
    class FieldManagementModelDataQueryHandler : IRequestHandler<FieldManagementModelDataQuery, SchemaDataViewModel>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        public FieldManagementModelDataQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<SchemaDataViewModel> Handle(FieldManagementModelDataQuery request, CancellationToken cancellationToken)
        {
            string sqlCommon = "";

            if (request.SchemaCore.OwnerType.ToLower() == "ngb") sqlCommon = GetNgbLoadExtensionDataQuery();
            else sqlCommon = GetClubLoadExtensionDataQuery();

            // Initialize data dictionary for debug visibility
            var data = new List<Dictionary<string, object>>();

            List<string> tables = new List<string>{
                        "Currency", "Date", "Decimal", "Int", "LargeText", "SmallText", "Text", "Currency",
                        "FieldSet_LargeText", "FieldSet_Currency", "FieldSet_Date", "FieldSet_Decimal",
                        "FieldSet_Int", "FieldSet_SmallText", "FieldSet_Text"
                };

            foreach (var table in tables)
            {
                string mainTable = $"Ex{request.SchemaCore.OwnerType}{request.SchemaCore.ExtensionArea}_{table}";
                string sqlFinal = string.Format(sqlCommon, mainTable);
                data.AddRange(await LoadDataAsync(sqlFinal, request.SchemaCore.ExId, request.MemberDocId, request.SchemaCore.ItemId));
            }


            // Process more fields as required by the schema
            

            return await GenerateFormDataFormat(data);

        }
        private string GetClubLoadExtensionDataQuery()
        {
            return @"SELECT parent.SyncGuid,
                        COALESCE(JSON_VALUE(parent.Config, '$.tabName'), '') AS FormName,
                        COALESCE(
                            (
                                SELECT 
                                    ef.Id,
                                    ef.Caption AS FieldName,
                                    ein.Value AS Value
                                FROM 
                                    EntityExtensionUI child
                                LEFT JOIN 
                                    {0} ein ON child.FieldId = ein.FieldId
                                LEFT JOIN 
                                    EntityExtensionField ef ON ein.FieldId = ef.Id
                                WHERE 
                                    child.ParentId = parent.ItemId 
                                    AND ein.ExId = @ExId 
                                    AND ein.DocId = @DocId
                                    AND (JSON_VALUE(Config, '$.fieldSec_clubsCanView') IS NULL OR JSON_VALUE(Config, '$.fieldSec_clubsCanView')<> 'false')
                                    AND (JSON_VALUE(Config, '$.equality_anonymised') IS NULL OR JSON_VALUE(Config, '$.equality_anonymised')<> 'true')
                                FOR JSON PATH
                            ), ''
                        ) AS FormData
                    FROM 
                        EntityExtensionUI parent
                    WHERE 
                        parent.ItemId IS NOT NULL AND parent.ItemId=@ItemId AND parent.ExId = @ExId
                        AND (JSON_VALUE(parent.Config, '$.security') IS NULL 
                              OR JSON_VALUE(parent.Config, '$.security') NOT LIKE '%hideFromClubs%')
                        AND COALESCE(JSON_VALUE(parent.Config, '$.tabName'), '') <> '';";
        }

        private string GetNgbLoadExtensionDataQuery()
        {
            return @"SELECT parent.SyncGuid,
                        COALESCE(JSON_VALUE(parent.Config, '$.tabName'), '') AS FormName,
                        COALESCE(
                            (
                                SELECT 
                                    ef.Id,
                                    ef.Caption AS FieldName,
                                    ein.Value AS Value
                                FROM 
                                    EntityExtensionUI child
                                LEFT JOIN 
                                    {0} ein ON child.FieldId = ein.FieldId
                                LEFT JOIN 
                                    EntityExtensionField ef ON ein.FieldId = ef.Id
                                WHERE 
                                    child.ParentId = parent.ItemId 
                                    AND ein.ExId = @ExId 
                                    AND ein.DocId = @DocId
                                FOR JSON PATH
                            ), ''
                        ) AS FormData
                    FROM 
                        EntityExtensionUI parent
                    WHERE 
                        parent.ItemId IS NOT NULL AND parent.ItemId=@ItemId AND parent.ExId = @ExId
                        AND COALESCE(JSON_VALUE(parent.Config, '$.tabName'), '') <> '';";
        }

        private async Task<List<Dictionary<string, object>>> LoadDataAsync(string query, int exId, int docId,int ItemId)
        {
            var resultData = new List<Dictionary<string, object>>();
            var param = new DynamicParameters();
            param.Add("@ExId", exId);
            param.Add("@DocId", docId);
            param.Add("@ItemId", ItemId);

            var result = await _readRepository.Value.GetListAsync(query, param, null, "text");
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(result));
            resultData.AddRange(data);

            return resultData;
        }

        private async Task<SchemaDataViewModel> GenerateFormDataFormat(List<Dictionary<string, object>> data)
        {

            var result = new SchemaDataViewModel();
            var groupedData = data.GroupBy(d => d.ContainsKey("FormName") ? d["FormName"].ToString() : "Unknown").ToList();  // Convert the result to a list
            foreach (var groupItem in groupedData)
            {
                try
                {

                    var modelOutList = new List<AdditionalValueDto>();
                    var formName = groupItem.Key.ToString();
                    var syncGuid = groupItem.FirstOrDefault()["SyncGuid"].ToString();
                    var groupData = groupItem.Select(m => m["FormData"]).ToList();
                    bool allDataIsEmpty = groupData.All(item => string.IsNullOrWhiteSpace(item?.ToString()));
                    if (allDataIsEmpty)
                    {
                        result = new SchemaDataViewModel
                        {
                            SyncGuid= syncGuid,
                            FormName = formName,
                            Items = modelOutList
                        };
                    }
                    else
                    {
                        var bindDic = new List<object>();
                        string joinOutPut = string.Empty;
                        foreach (var item in groupItem.Select(m => m["FormData"]).ToList())
                        {
                            if (string.IsNullOrWhiteSpace(item.ToString())) continue;
                            var output = JsonConvert.DeserializeObject<List<AdditionalValueDto>>(item.ToString());
                          

                            modelOutList.AddRange(output);
                        }

                        result = new SchemaDataViewModel
                        {
                            SyncGuid= syncGuid,
                            FormName = formName,
                            Items = modelOutList
                        };
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return await Task.FromResult(result);
        }
    }
}
