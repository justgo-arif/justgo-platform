using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiSchemaById;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetSpecificEntityData
{
    public class GetSpecificEntityDataHandler : IRequestHandler<GetSpecificEntityDataQuery, Dictionary<string, object>>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private const string VALUE_DELIMITER = "|";

        public GetSpecificEntityDataHandler(IMediator mediator, IReadRepositoryFactory readRepository)
        {
            _mediator = mediator;
            _readRepository = readRepository;
        }

        public async Task<Dictionary<string, object>> Handle(GetSpecificEntityDataQuery request, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            // 1. Get schema
            var schema = await _mediator.Send(new GetEntityExtensionUiSchemaByIdQuery(request.ItemId), cancellationToken);
            if (schema == null || !schema.IsInUse)
                return data;

            var ownerType = schema.OwnerType;
            var extensionArea = schema.ExtensionArea;
            var exId = request.ExId;
            var entityId = request.EntityId;

            var sql =
                    $"""
                    SELECT IsNull(Tag,'') as Tag FROM Ex{ownerType}{extensionArea}_Master WHERE ExId=@ExId AND DocId=@EntityId;
                    SELECT t.fieldId,t.Value FROM Ex{ownerType}{extensionArea}_Int t 
                        INNER JOIN [dbo].[EntityExtensionField] f ON f.Id=FieldId
                        INNER JOIN [dbo].[EntityExtensionUI] ui ON f.[Id] = ui.[FieldId]
                        INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON ui.[ParentId] = parent_ui.[ItemId]
                    WHERE t.ExId=@ExId AND t.DocId=@EntityId AND parent_ui.[SyncGuid]=@SyncGuid;
                    SELECT t.fieldId,t.Value FROM Ex{ownerType}{extensionArea}_Decimal t 
                        INNER JOIN [dbo].[EntityExtensionField] f ON f.Id=FieldId
                        INNER JOIN [dbo].[EntityExtensionUI] ui ON f.[Id] = ui.[FieldId]
                        INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON ui.[ParentId] = parent_ui.[ItemId]
                    WHERE t.ExId=@ExId AND t.DocId=@EntityId AND parent_ui.[SyncGuid]=@SyncGuid;
                    SELECT t.fieldId,t.Value,Currency FROM Ex{ownerType}{extensionArea}_Currency t 
                        INNER JOIN [dbo].[EntityExtensionField] f ON f.Id=FieldId
                        INNER JOIN [dbo].[EntityExtensionUI] ui ON f.[Id] = ui.[FieldId]
                        INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON ui.[ParentId] = parent_ui.[ItemId]
                    WHERE t.ExId=@ExId AND t.DocId=@EntityId AND parent_ui.[SyncGuid]=@SyncGuid;
                    SELECT t.fieldId,t.Value FROM Ex{ownerType}{extensionArea}_Date t 
                        INNER JOIN [dbo].[EntityExtensionField] f ON f.Id=FieldId
                        INNER JOIN [dbo].[EntityExtensionUI] ui ON f.[Id] = ui.[FieldId]
                        INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON ui.[ParentId] = parent_ui.[ItemId]
                    WHERE t.ExId=@ExId AND t.DocId=@EntityId AND parent_ui.[SyncGuid]=@SyncGuid;
                    SELECT t.fieldId,t.Value FROM Ex{ownerType}{extensionArea}_SmallText t 
                        INNER JOIN [dbo].[EntityExtensionField] f ON f.Id=FieldId
                        INNER JOIN [dbo].[EntityExtensionUI] ui ON f.[Id] = ui.[FieldId]
                        INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON ui.[ParentId] = parent_ui.[ItemId]
                    WHERE t.ExId=@ExId AND t.DocId=@EntityId AND parent_ui.[SyncGuid]=@SyncGuid;
                    SELECT t.fieldId,t.Value FROM Ex{ownerType}{extensionArea}_LargeText t 
                        INNER JOIN [dbo].[EntityExtensionField] f ON f.Id=FieldId
                        INNER JOIN [dbo].[EntityExtensionUI] ui ON f.[Id] = ui.[FieldId]
                        INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON ui.[ParentId] = parent_ui.[ItemId]
                    WHERE t.ExId=@ExId AND t.DocId=@EntityId AND parent_ui.[SyncGuid]=@SyncGuid;
                    SELECT t.fieldId,t.Value FROM Ex{ownerType}{extensionArea}_Text t 
                        INNER JOIN [dbo].[EntityExtensionField] f ON f.Id=FieldId
                        INNER JOIN [dbo].[EntityExtensionUI] ui ON f.[Id] = ui.[FieldId]
                        INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON ui.[ParentId] = parent_ui.[ItemId]
                    WHERE t.ExId=@ExId AND t.DocId=@EntityId AND parent_ui.[SyncGuid]=@SyncGuid;
                    """;

            var parameters = new { ExId = exId, EntityId = entityId, SyncGuid = request.ItemId };

            await using var multi = await _readRepository.GetLazyRepository<object>().Value.GetMultipleQueryAsync(sql, cancellationToken, parameters, null, "text");

            // Tag
            var tag = (await multi.ReadAsync<string>()).FirstOrDefault();
            data["Tag"] = tag ?? string.Empty;

            // Int
            var intValues = (await multi.ReadAsync<FieldValueResult>()).ToList();
            foreach (var row in intValues)
                data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Decimal
            var decimalValues = (await multi.ReadAsync<FieldValueResult>()).ToList();
            foreach (var row in decimalValues)
                data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Currency
            var currencyValues = (await multi.ReadAsync<CurrencyValueResult>()).ToList();
            foreach (var row in currencyValues)
            {
                data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;
                data[$"{row.fieldId}-Currency"] = row.Currency;
            }

            // Date
            var dateValues = (await multi.ReadAsync<FieldValueResult>()).ToList();
            foreach (var row in dateValues)
            {
                try
                {
                    data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;
                }
                catch
                {
                    data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = string.Empty;
                }
            }

            // SmallText
            var smallTextValues = (await multi.ReadAsync<FieldValueResult>()).ToList();
            foreach (var row in smallTextValues)
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!data.ContainsKey(key))
                    data[key] = val;
                else
                    data[key] += VALUE_DELIMITER + val;
            }

            // LargeText
            var largeTextValues = (await multi.ReadAsync<FieldValueResult>()).ToList();
            foreach (var row in largeTextValues)
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!data.ContainsKey(key))
                    data[key] = val;
                else
                    data[key] += VALUE_DELIMITER + val;
            }

            // Text
            var textValues = (await multi.ReadAsync<FieldValueResult>()).ToList();
            foreach (var row in textValues)
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!data.ContainsKey(key))
                    data[key] = val;
                else
                    data[key] += VALUE_DELIMITER + val;
            }


            var repo = _readRepository.GetLazyRepository<object>().Value;
            foreach (var fset in schema.FieldSets)
            {
                var fieldSetData = await GetFieldSetData(repo, schema, fset, entityId, cancellationToken);
                data["$" + fset.Name] = fieldSetData;
            }

            return data;
        }
        private async Task<Dictionary<int, Dictionary<string, object>>> GetFieldSetData(IReadRepository<object> repo, EntityExtensionSchema schema, EntityExtensionFieldSet fset, int entityId, CancellationToken cancellationToken)
        {
            var rowData = new Dictionary<int, Dictionary<string, object>>();
            var ownerType = schema.OwnerType;
            var extensionArea = schema.ExtensionArea;

            var sql =
                $"""
                SELECT rowId,IsNull(Tag,'') as Tag from Ex{ownerType}{extensionArea}_FieldSet_Master where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Int where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Decimal where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                SELECT rowId,fieldId,Value,Currency from Ex{ownerType}{extensionArea}_FieldSet_Currency where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Date where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_SmallText where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_LargeText where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Text where ExId=@ExId and DocId=@EntityId and FieldSetId=@FieldSetId;
                """;
            var parameters = new { schema.ExId, EntityId = entityId, FieldSetId = fset.Id };

            await using var multi = await repo.GetMultipleQueryAsync(sql, cancellationToken, parameters, null, "text");

            // Master
            var tags = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in tags)
            {
                int rowId = row.rowId;
                rowData[rowId] = new Dictionary<string, object> { { "Tag", row.Tag } };
            }

            // Int
            var intValues = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in intValues)
                rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Decimal
            var decimalValues = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in decimalValues)
                rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Currency
            var currencyValues = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in currencyValues)
            {
                rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;
                rowData[row.rowId][$"{row.fieldId}-Currency"] = row.Currency;
            }

            // Date
            var dateValues = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in dateValues)
            {
                try
                {
                    rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;
                }
                catch
                {
                    rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = string.Empty;
                }
            }

            // SmallText
            var smallTextValues = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in smallTextValues)
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!rowData[row.rowId].ContainsKey(key))
                    rowData[row.rowId][key] = val;
                else
                    rowData[row.rowId][key] += VALUE_DELIMITER + val;
            }

            // LargeText
            var largeTextValues = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in largeTextValues)
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!rowData[row.rowId].ContainsKey(key))
                    rowData[row.rowId][key] = val;
                else
                    rowData[row.rowId][key] += VALUE_DELIMITER + val;
            }

            // Text
            var textValues = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var row in textValues)
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!rowData[row.rowId].ContainsKey(key))
                    rowData[row.rowId][key] = val;
                else
                    rowData[row.rowId][key] += VALUE_DELIMITER + val;
            }
            return rowData;
        }



    }
}
