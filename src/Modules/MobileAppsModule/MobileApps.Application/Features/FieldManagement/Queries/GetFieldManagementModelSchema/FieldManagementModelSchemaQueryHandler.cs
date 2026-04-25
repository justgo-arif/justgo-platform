using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X9;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementModelSchema
{
    class FieldManagementModelSchemaQueryHandler : IRequestHandler<FieldManagementModelSchemaQuery, List<ParentClassDto>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public FieldManagementModelSchemaQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<ParentClassDto>> Handle(FieldManagementModelSchemaQuery request, CancellationToken cancellationToken)
        {
            string sqlSchema = "";
            var entityTypeId = request.EntityType.ToLower() == "ngb" ? 0 : 1;
            if (entityTypeId>0)
                sqlSchema = GetClubSchemaQuery();
            else sqlSchema = GetNgbSchemaQuery();

            var param = new { ExId = request.ExId, ItemId=request.ItemId };

            var parents = await _readRepository.Value.GetListAsync(sqlSchema, param, null, "text");

            if (!parents.Any())
            {
                return new List<ParentClassDto>();
            }
            var data = JsonConvert.DeserializeObject<List<ParentClass>>(JsonConvert.SerializeObject(parents));
            
            return await GetFieldSetsAndFieldsAsync(data, request.ExId, entityTypeId);

        }

        private string GetNgbSchemaQuery()
        {
            return @"SELECT COALESCE(JSON_VALUE(Config, '$.tabName'), '') AS FormName,ItemId,SyncGuid,*  FROM EntityExtensionUI WHERE EXID=@ExId AND ItemId=@ItemId AND Class='MA_TabItem';";
        }
        private string GetClubSchemaQuery()
        {
            return @"SELECT COALESCE(JSON_VALUE(Config, '$.tabName'), '') AS FormName,ItemId,SyncGuid,*  FROM EntityExtensionUI WHERE EXID=@ExId AND ItemId=@ItemId AND Class='MA_TabItem' AND (JSON_VALUE(Config, '$.security') IS NULL OR JSON_VALUE(Config, '$.security') NOT LIKE '%hideFromClubs%');";
        }

        private async Task<List<ParentClassDto>> GetFieldSetsAndFieldsAsync(List<ParentClass> parents, int exId,int entityId)
        {
            try
            {
                var parentIds = parents.Select(p => p.ItemId).ToList();

                // Get Children for All Parents
                string sqlChild = "";
                if (entityId > 0) sqlChild = GetClubFieldSetsAndFieldsQuery();
                else sqlChild = GetNgbFieldSetsAndFieldsQuery();

                var paramChild = new { ParentIds = parentIds, ExId = exId };
                var children = await _readRepository.Value.GetListAsync(sqlChild, paramChild, null, "text");

                string jsonChildData = JsonConvert.SerializeObject(children);
                var childrenData=JsonConvert.DeserializeObject<List<ChildItem>>(jsonChildData);
                //uncomment id grid item is need to show)
                //var childGrid = GetGridFieldSetsAndFieldsAsync(parentIds, exId, entityId);
                //if (childGrid.Result!=null && childGrid.Result.Any())
                //{
                //    var mergedReadOnlyList = childrenData.Concat(childGrid.Result).ToList();
                //    childrenData = mergedReadOnlyList;
                //}
                // Get Child Values for All Children
                var fieldIds = childrenData.ToList().Where(c => c.FieldId != -1).Select(c => c.FieldId).ToList();

                string sqlChildValue = @"SELECT FieldId,[Key], Caption as [Value],[Sequence]
                        FROM EntityExtensionFieldValues 
                        WHERE FieldId IN @FieldIds 
                        ORDER BY FieldValueId ASC;";

                var paramChildValue = new { FieldIds = fieldIds };
                var childValues = await _readRepository.Value.GetListAsync(sqlChildValue, paramChildValue, null, "text");

                var jsonData = JsonConvert.SerializeObject(childValues);
                var childValuesData = JsonConvert.DeserializeObject<List<ChildValue>>(jsonData);
                // Build Hierarchy
                var childLookup = childrenData.ToList().ToLookup(c => c.ParentId);
                var valueLookup = childValuesData.ToLookup(v => v.FieldId);

                var result = new List<ParentClassDto>();
                foreach (var parent in parents)
                {
                    var parentDto = new ParentClassDto {
                        FormName = parent.FormName,
                        SyncGuid=parent.SyncGuid
                    };
                    parentDto.Fields = new List<ChildItemDto>();
                    foreach (var child in childLookup[parent.ItemId])
                    {
                        if (child.FieldId == -1) continue;
                        var childDto = new ChildItemDto
                        {
                            FieldName = child.FieldName,
                            FieldId=child.FieldId,
                            IsRequired = child.IsRequired,
                            IsMultiSelect = child.IsRequired,
                            Class = child.Class,
                            Type= string.IsNullOrEmpty(child.Type) ? child.ClassShort : child.Type,
                            Config=JsonConvert.DeserializeObject<Dictionary<string, object>> (child.Config),
                            Rules= JsonConvert.DeserializeObject<Dictionary<string, object>>(child.Rules)

                        };

                        if (valueLookup.Contains(child.FieldId))
                        {
                            child.Values = valueLookup[child.FieldId].ToList(); // Convert IGrouping<int, ChildValue> to List<ChildValue>
                        }
                        if (child.Values.Count > 0)
                        {
                            var modelOutDic = new List<ChildValueDto>();
                            foreach (var item in child.Values)
                            {
                                var valueDto=new ChildValueDto
                                {
                                    FieldId= item.FieldId,
                                    Value=item.Value,
                                    Sequence=item.Sequence
                                };
                                modelOutDic.Add(valueDto);
                            }
                            childDto.Values = modelOutDic;
                        }
                        parentDto.Fields.Add(childDto);
                       
                    }

                    result.Add(parentDto);
                }

                return result;


            }
            catch (Exception ex)
            {
                // Log or handle exception
                throw new Exception($"Error loading field sets and fields: {ex.Message}");
            }
        }
        private async Task<List<ChildItem>> GetGridFieldSetsAndFieldsAsync(List<int> parentIds, int exId, int entityId)
        {
            try
            {
                string sqlChildGrid = @"";
                if (entityId > 0) sqlChildGrid = GetClubGridFieldSetsAndFieldsSql();
                else sqlChildGrid = GetNgbGridFieldSetsAndFieldsSql();

                var paramChild = new { ParentIds = parentIds, ExId = exId };

                var childGrid=await _readRepository.Value.GetAsync(sqlChildGrid, paramChild, null, "text");
                string jsonString = JsonConvert.SerializeObject(childGrid);
                return JsonConvert.DeserializeObject<List<ChildItem>>(jsonString);

            }
            catch (Exception ex)
            {
                // Log or handle exception
                throw new Exception($"Error loading grid field sets and fields: {ex.Message}");
            }
        }
       
        private string GetNgbFieldSetsAndFieldsQuery()
        {
            return @"SELECT 
                    COALESCE(JSON_VALUE(ui.Config, '$.label'), '') AS FieldName, 
                    COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') AS [Type], 
                    COALESCE(JSON_VALUE(ui.Config, '$.isRequired'), 'false') AS IsRequired,ui.Config,
                    CASE 
                    WHEN ui.Class = 'MA_CheckboxGroup' 
                    AND COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') <> 'radio' 
                    THEN 'true' 
                    ELSE COALESCE(
                    NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].isMultiSelect'), ''), 
                    NULLIF(JSON_VALUE(ui.Config, '$.isMultiSelect'), ''), 
                    'false'
                    ) 
                    END AS isMultiSelect,  
                    ui.FieldId, 
                    ui.ParentId,
                    f.DataType,
                    f.FieldSetId,
                    ui.Class,
					ui.Config, 
                    
                    CASE WHEN ui.Class LIKE 'MA_%Field' 
                        THEN SUBSTRING(ui.Class, 4, LEN(ui.Class) - 8)  -- Remove 'MA_' (3 chars) and 'Field' (5 chars)
                    WHEN ui.Class LIKE 'MA_%' 
                        THEN SUBSTRING(ui.Class, 4, LEN(ui.Class) - 3)  -- Remove 'MA_' (3 chars)
                    ELSE ui.Class
                    END AS ClassShort,
	                --COALESCE(JSON_QUERY(ui.Config, '$.rules'),JSON_VALUE(ui.Config, '$.type'),'') AS Rules

                    COALESCE(
                            JSON_QUERY(ui.Config, '$.rules'),
                            JSON_QUERY('{""type"":""' + JSON_VALUE(ui.Config, '$.type') + '""}'),
                            JSON_QUERY('{}')
                        ) AS Rules

                    FROM EntityExtensionUI ui
                    left join EntityExtensionField f on ui.FieldId=f.Id
                    WHERE ui.EXID=@ExId 
                    AND ui.ParentId IN @ParentIds;";
        }
        private string GetClubFieldSetsAndFieldsQuery()
        {
            return @"SELECT 
                    COALESCE(JSON_VALUE(ui.Config, '$.label'), '') AS FieldName, 
                    COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') AS [Type], 
                    COALESCE(JSON_VALUE(ui.Config, '$.isRequired'), 'false') AS IsRequired,ui.Config,
                    CASE 
                    WHEN ui.Class = 'MA_CheckboxGroup' 
                    AND COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') <> 'radio' 
                    THEN 'true' 
                    ELSE COALESCE(
                    NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].isMultiSelect'), ''), 
                    NULLIF(JSON_VALUE(ui.Config, '$.isMultiSelect'), ''), 
                    'false'
                    ) 
                    END AS isMultiSelect,  
                    ui.FieldId, 
                    ui.ParentId,
                    f.DataType,
                    f.FieldSetId,
                    ui.Class,
                    ui.Config,      
                    CASE WHEN ui.Class LIKE 'MA_%Field' 
                        THEN SUBSTRING(ui.Class, 4, LEN(ui.Class) - 8)  -- Remove 'MA_' (3 chars) and 'Field' (5 chars)
                    WHEN ui.Class LIKE 'MA_%' 
                        THEN SUBSTRING(ui.Class, 4, LEN(ui.Class) - 3)  -- Remove 'MA_' (3 chars)
                    ELSE ui.Class
                    END AS ClassShort,
	                --COALESCE(JSON_QUERY(ui.Config, '$.rules'),JSON_VALUE(ui.Config, '$.type'),'') AS Rules
                    COALESCE(
                            JSON_QUERY(ui.Config, '$.rules'),
                            JSON_QUERY('{""type"":""' + JSON_VALUE(ui.Config, '$.type') + '""}'),
                            JSON_QUERY('{}')
                        ) AS Rules

                    FROM EntityExtensionUI ui
                    left join EntityExtensionField f on ui.FieldId=f.Id
                    WHERE ui.EXID=@ExId 
                    AND ui.ParentId IN @ParentIds
                    AND (JSON_VALUE(ui.Config, '$.fieldSec_clubsCanView') IS NULL OR JSON_VALUE(ui.Config, '$.fieldSec_clubsCanView')<> 'false');";
        }
        private string GetNgbGridFieldSetsAndFieldsSql()
        {
            return @"SELECT f.Name AS FieldName, 
                    COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') AS [Type],
                    COALESCE(JSON_VALUE(ui.Config, '$.isRequired'), 'false') AS IsRequired,ui.Config,
                    CASE 
                    WHEN ui.Class = 'MA_CheckboxGroup' 
                    AND COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') <> 'radio' 
                    THEN 'true' 
                    ELSE COALESCE(
                    NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].isMultiSelect'), ''), 
                    NULLIF(JSON_VALUE(ui.Config, '$.isMultiSelect'), ''), 'false') 
                    END AS isMultiSelect,  
                    f.id as FieldId, 
                    ui.ParentId,
                    f.DataType,
                    f.FieldSetId
                    FROM EntityExtensionUI ui
                    left join EntityExtensionFieldSet fs on JSON_VALUE(REPLACE(ui.Config, '$fs', 'fs'), '$.fs.name')=fs.[Name]
                    left join EntityExtensionField f on fs.Id=f.FieldSetId
                    WHERE ui.ExId=@ExId and ui.ParentId in @ParentIds
                    and Class='MA_Grid';";
        }
        private string GetClubGridFieldSetsAndFieldsSql()
        {
            return @"SELECT f.Name AS FieldName, 
                    COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') AS [Type],
                    COALESCE(JSON_VALUE(ui.Config, '$.isRequired'), 'false') AS IsRequired,ui.Config,
                    CASE 
                    WHEN ui.Class = 'MA_CheckboxGroup' 
                    AND COALESCE(NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].type'), ''), JSON_VALUE(ui.Config, '$.type'), '') <> 'radio' 
                    THEN 'true' 
                    ELSE COALESCE(
                    NULLIF(JSON_VALUE(ui.Config, '$.rules.visible.rules[0].isMultiSelect'), ''), 
                    NULLIF(JSON_VALUE(ui.Config, '$.isMultiSelect'), ''), 'false') 
                    END AS isMultiSelect,  
                    f.id as FieldId, 
                    ui.ParentId,
                    f.DataType,
                    f.FieldSetId
                    FROM EntityExtensionUI ui
                    left join EntityExtensionFieldSet fs on JSON_VALUE(REPLACE(ui.Config, '$fs', 'fs'), '$.fs.name')=fs.[Name]
                    left join EntityExtensionField f on fs.Id=f.FieldSetId
                    WHERE ui.ExId=@ExId and ui.ParentId in @ParentIds
                    and Class='MA_Grid'
                    AND (JSON_VALUE(ui.Config, '$.security') IS NULL 
                    OR JSON_VALUE(ui.Config, '$.security') NOT LIKE '%hideFromClubs%');";
        }
    }
}
