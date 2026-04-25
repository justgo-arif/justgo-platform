using System.Data;
using System.Text;
using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.CreateEntitySchema
{
    public class CreateEntitySchemaHandler : IRequestHandler<CreateEntitySchemaCommand, EntityExtensionSchema>
    {
        private readonly string[] AllowedOwner = { "Ngb", "Club" };
        private readonly string[] AllowedArea = { "Profile", "Qualification", "Membership", "Credential", "Event", "Club", "EventMaster", "Asset" };
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        IUnitOfWork _unitOfWork;
        public CreateEntitySchemaHandler(IReadRepositoryFactory readRepository,IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;   
        }

        public async Task<EntityExtensionSchema> Handle(CreateEntitySchemaCommand request, CancellationToken cancellationToken)
        {
            var result = await SaveEntityExtensionSchema(request, (int)request.tabItemId, cancellationToken);
            return result;
        }

        private async Task<EntityExtensionSchema> SaveEntityExtensionSchema(EntityExtensionSchema schema, int tabItemId, CancellationToken cancellationToken)
        {
            var filterEntityExtentionUI = FilterItems(schema.UiComps, tabItemId);
            schema.UiComps.RemoveAll(ui => !filterEntityExtentionUI.Any(fi => fi.ItemId == ui.ItemId));
            schema.Fields.RemoveAll(field => !filterEntityExtentionUI.Any(fi => fi.FieldId == field.Id));

            List<EntityExtensionFieldSet> newEntityExtensioFieldSet = new List<EntityExtensionFieldSet>();
            foreach (var fieldSet in schema.FieldSets)
            {
                EntityExtensionFieldSet entityExtensionFieldSet = fieldSet;
                List<EntityExtensionField> fields = new List<EntityExtensionField>();
                foreach (var item in fieldSet.Fields)
                {
                    if (filterEntityExtentionUI.Any(fi => fi.FieldId == item.Id))
                    {
                        fields.Add(item);

                    }
                }

                if (fields.Count > 0)
                {
                    entityExtensionFieldSet.Fields = fields;
                    newEntityExtensioFieldSet.Add(entityExtensionFieldSet);
                }
            }
            schema.FieldSets = newEntityExtensioFieldSet;

            if (!AllowedOwner.Contains(schema.OwnerType))
                throw new Exception("Invalid Owner Type");
            if (!AllowedArea.Contains(schema.ExtensionArea))
                throw new Exception("Invalid Extension Area");
            if (schema.OwnerType.Equals("Ngb"))
                schema.OwnerId = 0;
            else
            {
                if (schema.OwnerId <= 0)
                    throw new Exception("Invalid OwnerId");
            }
            if (schema.ExtensionArea.Equals("Profile"))
                schema.ExtensionEntityId = 0;

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            schema.ExId = await SaveToExtensionSchemaDB(schema, dbTransaction, cancellationToken);
            var tabItemid = schema.UiComps.Where(uc => uc.Class == "MA_TabItem" && uc.FieldId == -1).FirstOrDefault().ItemId;
            Dictionary<int, int> oldToNewFieldIds = await SaveEntityExtensionField(schema.ExId, 0, schema.Fields, tabItemid, dbTransaction, cancellationToken);
            foreach (var fset in schema.FieldSets)
            {
                await SaveFieldSet(schema.ExId, fset, oldToNewFieldIds, tabItemId, dbTransaction, cancellationToken);
            }
            var entityExtensionFieldSetNames = await GetFieldSetNameByTabId(tabItemId, dbTransaction, cancellationToken);
            foreach (var fset in schema.FieldSets)
            {
                entityExtensionFieldSetNames.RemoveAll(fieldSet => fieldSet.Id == fset.Id);
            }
            if (entityExtensionFieldSetNames.Any())
            {
                var fieldSetIds = entityExtensionFieldSetNames.Select(fs => fs.Id).ToList();
                await DeleteEntityExtensionFieldSetById(fieldSetIds, dbTransaction, cancellationToken);
            }

            await SaveEntityExtensionFieldUI(schema, oldToNewFieldIds, tabItemid, dbTransaction, cancellationToken);

            await CreateEntityExtensionView(schema, dbTransaction, cancellationToken);
            foreach (var fset in schema.FieldSets)
            {
                if (schema.OwnerType == "Ngb" && schema.ExtensionArea == "Event")
                {
                   await CreateEntityExtensionFieldSetView(schema.OwnerType, schema.ExtensionArea, fset.Id, schema.ExId, dbTransaction, cancellationToken);
                }
            }
            await _unitOfWork.CommitAsync(dbTransaction);
            return schema;
        }
        private async Task<int> SaveToExtensionSchemaDB(EntityExtensionSchema schema, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            queryParameters.Add("@ExId", schema.ExId);
            queryParameters.Add("@OwnerType", schema.OwnerType);
            queryParameters.Add("@ExtensionArea", schema.ExtensionArea);
            queryParameters.Add("@OwnerId", schema.OwnerId);
            queryParameters.Add("@ExtensionEntityId", schema.ExtensionEntityId);
            queryParameters.Add("@IsInUse", schema.IsInUse);
            var newId = await _writeRepository.GetLazyRepository<object>().Value.ExecuteQuerySingleAsync<int>(SAVE_ENTITY_EXTENSION_SCHEMA, cancellationToken, queryParameters, dbTransaction, "text");
            return newId > 0 ? newId : schema.ExId;
        }
        private async Task<Dictionary<int, int>> SaveEntityExtensionField(int schemaId, int fieldSetId, List<EntityExtensionField> fields, int tabItemId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var oldToNewFieldIds = new Dictionary<int, int>();           
            const int batchSize = 200;
            var batches = fields.Select((field, index) => new { field, index })
                               .GroupBy(x => x.index / batchSize)
                               .Select(g => g.Select(x => x.field).ToList());
            var newFields = new List<EntityExtensionField>();
            var newFieldIds = new List<int>();
            var listFieldsDelete = new List<int>();
            foreach (var batch in batches)
            {
                var batchFieldParameters = CreateBulkFieldParameters(batch, schemaId, fieldSetId);
                var batchFieldQuery = new StringBuilder();
                for (int i = 0; i < batch.Count; i++)
                {
                    batchFieldQuery.AppendFormat(SAVE_ENTITY_EXTENSION_FIELD, i);
                    batchFieldQuery.AppendLine();
                }
                await SaveSchemaDB(batchFieldQuery.ToString(), dbTransaction, batchFieldParameters, cancellationToken);
                var newIds = batchFieldParameters.ParameterNames
                               .Where(name => name.Contains("NewId"))
                               .Select(name => batchFieldParameters.Get<int>(name))
                               .ToList();
                newFieldIds.AddRange(newIds);
            }

            for (int i = 0; i < fields.Count; i++)
            {
                var oldId = fields[i].Id;
                var newId = newFieldIds[i];
                if (newId > 0)
                {
                    fields[i].Id = newId;
                }
                oldToNewFieldIds[oldId] = fields[i].Id;
                listFieldsDelete.Add(fields[i].Id);
                newFields.Add(fields[i]);
            }
            if (listFieldsDelete.Any())
            {
                if (fieldSetId > 0)
                {
                    await DeleteEntityExtensionFieldValueFieldSet(fieldSetId, listFieldsDelete, dbTransaction, cancellationToken);
                }
                else
                {
                    await DeleteEntityExtensionFieldValue(tabItemId, listFieldsDelete, dbTransaction, cancellationToken);
                }
            }
            if (fieldSetId > 0)
            {
                await DeleteEntityExtensionFieldValueFieldSetInvalid(fieldSetId, dbTransaction, cancellationToken);
            }
            else
            {
                await DeleteEntityExtensionFieldValueInvalid(tabItemId, dbTransaction, cancellationToken);
            }

            await SaveFieldValueDB(newFields, schemaId, tabItemId, fieldSetId, dbTransaction, cancellationToken);

            if (fields.Any())
            {
                var fieldIds = fields.Select(f => f.Id).ToList();
                await DeleteEntityExtensionFieldWithField(schemaId, tabItemId, fieldSetId, fieldIds, dbTransaction, cancellationToken);
            }
            else
            {
                await DeleteEntityExtensionField(schemaId, tabItemId, fieldSetId, dbTransaction, cancellationToken);
            }          

            return oldToNewFieldIds;
        }
        private DynamicParameters CreateBulkFieldParameters(List<EntityExtensionField> batch, int schemaId, int fieldSetId)
        {
            var parameters = new DynamicParameters();
            for (int i = 0; i < batch.Count; i++)
            {
                var field = batch[i];
                parameters.Add($"NewId{i}", 0, DbType.Int32, ParameterDirection.Output);
                parameters.Add($"ExId{i}", schemaId);
                parameters.Add($"FieldSetId{i}", fieldSetId);
                parameters.Add($"Name{i}", field.Name);
                parameters.Add($"Description{i}", field.Description);
                parameters.Add($"Caption{i}", field.Caption);                
                parameters.Add($"DataType{i}", (int)field.Type);
                parameters.Add($"IsInUse{i}", field.IsInUse);
                parameters.Add($"IsMultiValue{i}", field.IsMultiValue);
                parameters.Add($"Id{i}", field.Id);
            }
            return parameters;
        }
        private async Task<int> GetMaxFieldValueId(IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = "select isnull(max(FieldValueId),0) as MaxFieldValueId from EntityExtensionFieldValues";
            var queryParameters = new DynamicParameters();
            var result = await _readRepository.GetLazyRepository<object>().Value.GetSingleAsync<int>(sql, queryParameters, dbTransaction, cancellationToken, "text");
            return result;
        }
        private async Task SaveSchemaDB(string sql, IDbTransaction dbTransaction, DynamicParameters dynamicParameters, CancellationToken cancellationToken)
        {
            await _writeRepository.GetLazyRepository<EntityExtensionField>().Value.ExecuteAsync(sql, cancellationToken, dynamicParameters, dbTransaction, "text");
        }
        private async Task DeleteEntityExtensionFieldValueFieldSet(int fieldSetId, List<int> listFieldsDelete, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"delete EntityExtensionFieldValues where FieldId in @Fields 
                                             and fieldid in (select id from   EntityExtensionField where FieldSetId=@FieldSetId)";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Fields", listFieldsDelete);
            queryParameters.Add("@FieldSetId", fieldSetId);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
        }
        private async Task DeleteEntityExtensionFieldValue(int tabItemId, List<int> listFieldsDelete, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"delete EntityExtensionFieldValues where FieldId in @Fields  
                                            and fieldid in (  select UI.FieldId from   EntityExtensionUI UI 
                                            where UI.ParentId=@TabItemId and UI.FieldId >0)";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Fields", listFieldsDelete);
            queryParameters.Add("@TabItemId", tabItemId);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
        }
        private async Task DeleteEntityExtensionFieldValueFieldSetInvalid(int fieldSetId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"delete from EntityExtensionFieldValues
                                                 where fieldid in (
                                                                         select f.id
                                                                         from EntityExtensionFieldValues fv
                                                                             inner join entityextensionfield f
                                                                                 on fv.FieldId = f.Id
                                                                         where f.DataType != 6
                                                                             and fieldid in (
                                                                                                 select id from EntityExtensionField where FieldSetId =@FieldSetId
                                                                                             )
                                                                     )";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@FieldSetId", fieldSetId);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
        }
        private async Task DeleteEntityExtensionFieldValueInvalid(int tabItemId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"delete EntityExtensionFieldValues
                                         where fieldid in (
                                                              select Id
                                                              from entityextensionfield f
                                                                  inner join EntityExtensionUI ui
                                                                      on f.id = ui.fieldid
                                                              where DataType != 6
                                                                    and UI.ParentId = @TabItemId
                                                          ) ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TabItemId", tabItemId);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
        }
        private async Task SaveFieldValueDB(List<EntityExtensionField> fields, int schemaId, int tabItemId, int fieldSetId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var fieldsWithValues = fields.Where(f => f.AllowedValues.Any()).ToList();
            var fieldValues = fieldsWithValues.SelectMany(f => f.AllowedValues).ToList();
            var maxFieldValueId = await GetMaxFieldValueId(dbTransaction, cancellationToken);
            const int valueBatchSize = 200;
            var batches = fieldValues.Select((fieldValue, index) => new { fieldValue, index })
                                       .GroupBy(x => x.index / valueBatchSize)
                                       .Select(g => g.Select(x => x.fieldValue).ToList());

            foreach (var batch in batches)
            {
                var batchFieldValueParameters = CreateBulkFieldValueParameters(batch, maxFieldValueId);
                var batchFieldValueQuery = new StringBuilder();
                for (int i = 0; i < batch.Count; i++)
                {
                    batchFieldValueQuery.AppendFormat(SAVE_FIELD_VALUE, i);
                    batchFieldValueQuery.AppendLine();
                }
                await _writeRepository.GetLazyRepository<EntityExtensionFieldValue>().Value.ExecuteAsync(batchFieldValueQuery.ToString(), cancellationToken, batchFieldValueParameters, dbTransaction, "text");
            }   
        }
        private DynamicParameters CreateBulkFieldValueParameters(List<EntityExtensionFieldValue> batch, int maxFieldValueId)
        {
            var parameters = new DynamicParameters();
            for (int i = 0; i < batch.Count; i++)
            {
                var fieldValue = batch[i];
                parameters.Add($"FieldId{i}", fieldValue.FieldId);
                parameters.Add($"Key{i}", fieldValue.Key);
                parameters.Add($"Caption{i}", fieldValue.Caption);
                parameters.Add($"Description{i}", fieldValue.Description);
                parameters.Add($"Lang{i}", fieldValue.Lang);
                parameters.Add($"Value{i}", fieldValue.Value);
                parameters.Add($"Sequence{i}", fieldValue.Sequence);
                parameters.Add($"FieldValueId{i}", fieldValue.FieldValueId);
                parameters.Add($"maxFieldValueId{i}", maxFieldValueId);
            }
            return parameters;
        }
        private async Task DeleteEntityExtensionField(int schemaId, int tabItemId, int fieldSetId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"declare @tempFieldIds table (fieldId int)
                                     insert into @tempFieldIds
                                     select id from EntityExtensionField where id in ( select FieldId  from EntityExtensionUI where parentId=@TabItemId)
                                     delete eef from EntityExtensionField eef inner join @tempFieldIds tfi on eef.Id = tfi.fieldId
                                     where exid=@SchemaId and FieldSetId=@FieldSetId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TabItemId", tabItemId);
            queryParameters.Add("@SchemaId", schemaId);
            queryParameters.Add("@FieldSetId", fieldSetId);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
        }
        private async Task DeleteEntityExtensionFieldWithField(int schemaId, int tabItemId, int fieldSetId, List<int> fields, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"declare @tempFieldIds table (fieldId int)
                                     insert into @tempFieldIds
                                     select id from EntityExtensionField where id in ( select FieldId  from EntityExtensionUI where parentId=@TabItemId)
                                     delete eef from EntityExtensionField eef inner join @tempFieldIds tfi on eef.Id = tfi.fieldId
                                     where exid=@SchemaId and FieldSetId=@FieldSetId and eef.Id not in @Fields";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TabItemId", tabItemId);
            queryParameters.Add("@SchemaId", schemaId);
            queryParameters.Add("@FieldSetId", fieldSetId);
            queryParameters.Add("@Fields", fields);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
        }
        private async Task SaveFieldSet(int exId, EntityExtensionFieldSet fset, Dictionary<int, int> oldToNewFieldIds
            , int tabItemId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            queryParameters.Add("@ExId", exId);
            queryParameters.Add("@Name", fset.Name);
            queryParameters.Add("@Description", fset.Description);
            queryParameters.Add("@Caption", fset.Caption);
            queryParameters.Add("@IsInUse", fset.IsInUse);
            queryParameters.Add("@Id", fset.Id);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(SAVE_ENTITY_EXTENSION_FIELDSET, cancellationToken, queryParameters, dbTransaction, "text");
            fset.Id = queryParameters.Get<int>("@NewId");

            var maping = await SaveEntityExtensionField(exId, fset.Id, fset.Fields, tabItemId, dbTransaction, cancellationToken);

            foreach (var item in maping)
                oldToNewFieldIds.Add(item.Key, item.Value);
        }
        private List<EntityExtensionUI> FilterItems(List<EntityExtensionUI> items, int targetItemId)
        {
            var tabitem = items.Where(i => i.Class == "MA_Tabs").FirstOrDefault();
            var itemsToKeep = new List<int>();
            itemsToKeep.Add(tabitem.ItemId);
            itemsToKeep.Add(targetItemId);

            var childItems = items.Where(item => item.ParentId == targetItemId).Select(item => item.ItemId).ToList();
            childItems.ForEach(i => itemsToKeep.Add(i));

            foreach (var child in childItems)
            {
                var grandChilds = items.Where(item => item.ParentId == child).Select(item => item.ItemId).ToList();

                grandChilds.ForEach(i => itemsToKeep.Add(i));

            }
            return items.Where(item => itemsToKeep.Contains(item.ItemId)).ToList();
        }
        private async Task<List<EntityExtensionFieldSet>> GetFieldSetNameByTabId(int tabItemId,IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = "GetFieldSetNameOfTabItems";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TabItemId", tabItemId);
            var results = (await _readRepository.GetLazyRepository<EntityExtensionFieldSet>().Value.GetListAsync(sql, cancellationToken, queryParameters, dbTransaction)).ToList();
            return results;
        }
        private async Task DeleteEntityExtensionFieldSetById(List<int> fieldSetIds, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = "DeleteEntityExtensionfieldSetById";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@IdList", string.Join(", ", fieldSetIds));
            await _writeRepository.GetLazyRepository<EntityExtensionFieldSet>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction);
        }
        private async Task CreateEntityExtensionView(EntityExtensionSchema schema, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            if (schema.OwnerType.Equals("Ngb"))//Create view for only Ngb
            {
                var sql = "CreateEntityExtentionView";
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@OwnerType", schema.OwnerType);
                queryParameters.Add("@ExtensionArea", schema.ExtensionArea);
                await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction);
            }
        }
        private async Task CreateEntityExtensionFieldSetView(string ownerType, string extensionArea, int fieldSetId, int exId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            if (ownerType == "Ngb")
            {
                var sql = "CreateEntityExtentionFieldSetView";
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@OwnerType", ownerType);
                queryParameters.Add("@ExtensionArea", extensionArea);
                queryParameters.Add("@FieldSetId", fieldSetId);
                queryParameters.Add("@ExId", exId);
                await _writeRepository.GetLazyRepository<EntityExtensionFieldSet>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction);
            }
        }
        private async Task SaveEntityExtensionFieldUI(EntityExtensionSchema schema, Dictionary<int, int> oldToNewFieldIds, int tabItemId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var oldToNewParentIds = new Dictionary<int, int>();
            const int batchSize = 200;
            var batches = schema.UiComps.Select((uiComp, index) => new { uiComp, index })
                               .GroupBy(x => x.index / batchSize)
                               .Select(g => g.Select(x => x.uiComp).ToList());
            var newIds = new List<int>();
            foreach (var batch in batches)
            {
                var batchUiParameters = CreateBulkUIParameters(batch, schema.ExId, oldToNewFieldIds);
                var batchUiQuery = new StringBuilder();
                for (int i = 0; i < batch.Count; i++)
                {
                    batchUiQuery.AppendFormat(SAVE_ENTITY_EXTENSION_UI, i);
                    batchUiQuery.AppendLine();
                }
                await SaveExtensionUiDB(batchUiQuery.ToString(), dbTransaction, batchUiParameters, cancellationToken);
                var ids = batchUiParameters.ParameterNames
                               .Where(name => name.Contains("NewId"))
                               .Select(name => batchUiParameters.Get<int>(name))
                               .ToList();
                newIds.AddRange(ids);
            }

            for (var i = 0; i < schema.UiComps.Count; i++)
            {
                var oldParentId = schema.UiComps[i].ItemId;
                var newId = newIds[i];
                if (newId > 0)
                    schema.UiComps[i].ItemId = newId;

                if (oldParentId < 0)
                    oldToNewParentIds[oldParentId] = schema.UiComps[i].ItemId;
            }
                        
            if (schema.UiComps.Any())
                await DeleteEntityExtensionUIByItem(tabItemId, schema.UiComps, dbTransaction, cancellationToken);
            else
                await DeleteEntityExtensionUI(tabItemId, dbTransaction, cancellationToken);


            foreach (var oldToNewParentId in oldToNewParentIds)
            {
                await UpdateEntityExtensionUI(oldToNewParentId.Value, oldToNewParentId.Key, schema.ExId, dbTransaction, cancellationToken);
            }
        }
        private DynamicParameters CreateBulkUIParameters(List<EntityExtensionUI> batch, int exId, Dictionary<int, int> oldToNewFieldIds)
        {
            var parameters = new DynamicParameters();
            for (int i = 0; i < batch.Count; i++)
            {
                var ui = batch[i];
                parameters.Add($"NewId{i}", 0, DbType.Int32, ParameterDirection.Output);
                parameters.Add($"ExId{i}", exId);
                parameters.Add($"ParentId{i}", ui.ParentId);
                parameters.Add($"Class{i}", ui.Class);
                parameters.Add($"Config{i}", ui.Config);
                parameters.Add($"FieldId{i}", ui.FieldId != -1 ? oldToNewFieldIds[ui.FieldId] : ui.FieldId);
                parameters.Add($"ItemId{i}", ui.ItemId);
                parameters.Add($"Sequence{i}", ui.Sequence);
            }
            return parameters;
        }
        private async Task SaveExtensionUiDB(string sql, IDbTransaction dbTransaction, DynamicParameters dynamicParameters, CancellationToken cancellationToken)
        {
            await _writeRepository.GetLazyRepository<EntityExtensionUI>().Value.ExecuteAsync(sql, cancellationToken, dynamicParameters, dbTransaction, "text");
        }

        private async Task DeleteEntityExtensionUI(int tabItemId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"declare @EntityExtensionUI table (ItemId int)
                                                    ;WITH ret AS(
                                                            SELECT  *
                                                            FROM    EntityExtensionUI
                                                            WHERE   ItemId = @ItemId
                                                            UNION ALL
                                                            SELECT  t.*
                                                            FROM    EntityExtensionUI t INNER JOIN
                                                                    ret r ON t.ParentID = r.ItemId
                                                    )
                                                    insert into @EntityExtensionUI
                                                    select ItemId from ret

                                                    delete from EntityExtensionUI  eeu
                                                    inner join @EntityExtensionUI  tempEEU on eeu.ItemId = tempEEU.itemid";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ItemId", tabItemId);
            await _writeRepository.GetLazyRepository<EntityExtensionUI>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction, "text");
        }

        private async Task DeleteEntityExtensionUIByItem(int tabItemId, List<EntityExtensionUI> uiComps, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = @"declare @EntityExtensionUI table (ItemId int)
                                                    ;WITH ret AS(
                                                            SELECT  *
                                                            FROM    EntityExtensionUI
                                                            WHERE   ItemId = @ItemId
                                                            UNION ALL
                                                            SELECT  t.*
                                                            FROM    EntityExtensionUI t INNER JOIN
                                                                    ret r ON t.ParentID = r.ItemId
                                                    )
                                                    insert into @EntityExtensionUI
                                                    select ItemId from ret


                                                    delete eeu from EntityExtensionUI  eeu
                                                    inner join @EntityExtensionUI  tempEEU on eeu.ItemId = tempEEU.itemid
                                                    where  eeu.ItemId not in @ItemIds";
            var itemIds = uiComps.Select(f => f.ItemId).ToList();
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ItemId", tabItemId);
            queryParameters.Add("@ItemIds", itemIds);
            await _writeRepository.GetLazyRepository<EntityExtensionUI>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction,"text");
        }

        private async Task UpdateEntityExtensionUI(int value, int key, int exId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var sql = "Update EntityExtensionUI set ParentId=@ParentIdValue where ParentId=@ParentIdKey and ExId=@ExId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ParentIdValue", value);
            queryParameters.Add("@ParentIdKey", key);
            queryParameters.Add("@ExId", exId);
            await _writeRepository.GetLazyRepository<EntityExtensionUI>().Value.ExecuteAsync(sql, cancellationToken, queryParameters, dbTransaction,"text");
        }

        private const string SAVE_ENTITY_EXTENSION_SCHEMA =
            @"
              if not exists(select Exid from EntityExtensionSchema where ExId=@ExId)
                begin
                    INSERT INTO EntityExtensionSchema(OwnerType,OwnerId,ExtensionArea,ExtensionEntityId,IsInUse)
                    VALUES(@OwnerType,@OwnerId,@ExtensionArea,@ExtensionEntityId,0)
                    SELECT @@Identity AS NewId
                end
                ELSE
                BEGIN
                    UPDATE EntityExtensionSchema
                    SET OwnerType=@OwnerType,
                        OwnerId=@OwnerId,
                        ExtensionArea=@ExtensionArea,
                        ExtensionEntityId=@ExtensionEntityId,
                        IsInUse=@IsInUse
                    WHERE EXID=@ExId
                    SELECT @ExId AS NewId
                END
             ";
        private const string SAVE_ENTITY_EXTENSION_FIELD = @"
                if not exists(select id from EntityExtensionField where id=@Id{0})
            begin
	            INSERT INTO EntityExtensionField(ExId,FieldSetId,Name,Caption,Description,DataType,IsInUse,IsMultiValue)
                                        VALUES(@ExId{0},@FieldSetId{0},@Name{0},@Caption{0},@Description{0},@DataType{0},0,@IsMultiValue{0})
	            set @NewId{0}=@@Identity
            end
            else
            begin
              update EntityExtensionField 
               set Name=@Name{0},
                   Caption=@Caption{0},
                   Description=@Description{0},
	               DataType=@DataType{0},
                   IsInUse=@IsInUse{0},
                   IsMultiValue=@IsMultiValue{0}
	            where Id=@Id{0}
                set @NewId{0}=@Id{0}
            end
        ";
        private const string SAVE_FIELD_VALUE = @"                       
                        if(@FieldValueId{0} <= 0)
                        begin
                             set @FieldValueId{0} = ((select (select isnull(max(FieldValueId),0) from EntityExtensionFieldValues)+1))
                            if(@FieldValueId{0} <=@maxFieldValueId{0} )
                            begin
                             set @FieldValueId{0} =  (select @maxFieldValueId{0} + 1)
                            end
                        end
                        insert into EntityExtensionFieldValues(FieldId,[Key],Caption,Description,Lang,Value,Sequence,FieldValueId)
				        values(@FieldId{0},@Key{0},@Caption{0},@Description{0},@Lang{0},@Value{0},@Sequence{0},@FieldValueId{0})
        ";
        private const string SAVE_ENTITY_EXTENSION_FIELDSET = @"

            if not exists(select id from EntityExtensionFieldSet where id=@Id)
            begin
	            INSERT INTO EntityExtensionFieldSet(ExId,Name,Caption,Description,IsInUse)
                                        VALUES(@ExId,@Name,@Caption,@Description,0)
	            set @NewId=@@Identity
            end
            else
            begin
              update EntityExtensionFieldSet 
               set Name=@Name,
                   Caption=@Caption,
                   Description=@Description,
                   IsInUse=@IsInUse
	            where Id=@Id
                set @NewId=@Id
            end
        ";
        private const string SAVE_ENTITY_EXTENSION_UI = @"   
            if not exists(select ItemId from EntityExtensionUI where ItemId=@ItemId{0})
            begin
	            INSERT INTO EntityExtensionUI(ExId,ParentId,Class,Config,FieldId,Sequence)
                                  VALUES(@ExId{0},@ParentId{0},@Class{0},@Config{0},@FieldId{0},@Sequence{0})
	            set @NewId{0}=@@Identity
            end
            else
            begin
              update EntityExtensionUI 
               set ParentId=@ParentId{0},
                   Class=@Class{0},
	               Config=@Config{0},
	               FieldId=@FieldId{0},
                   Sequence=@Sequence{0}
	            where ItemId=@ItemId{0}

                set @NewId{0}=@ItemId{0}
            end
        ";

    }
}
