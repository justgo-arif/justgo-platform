using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionSchemaById;
using JustGo.FieldManagement.Domain.Entities;
using System.Globalization;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetEntityData
{

    public class GetEntityDataHandler : IRequestHandler<GetEntityDataQuery, Dictionary<string, object>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private const string VALUE_DELIMITER = "|";
        private readonly IMediator _mediator;

        public GetEntityDataHandler(IReadRepositoryFactory readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<Dictionary<string, object>> Handle(GetEntityDataQuery request, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object>();
            bool isArena = false;

            // 1. Get schema
            var schema = await _mediator.Send(new GetEntityExtensionSchemaByIdQuery(request.ExId, isArena));
            if (schema == null || !schema.IsInUse)
                return data;

            var ownerType = schema.OwnerType;
            var extensionArea = schema.ExtensionArea;
            var exId = request.ExId;
            var docId = request.DocId;

            var sql = $@"
                        SELECT IsNull(Tag,'') as Tag from Ex{ownerType}{extensionArea}_Master where ExId=@ExId and DocId=@DocId;
                        SELECT fieldId,Value from Ex{ownerType}{extensionArea}_Int where ExId=@ExId and DocId=@DocId;
                        SELECT fieldId,Value from Ex{ownerType}{extensionArea}_Decimal where ExId=@ExId and DocId=@DocId;
                        SELECT fieldId,Value,Currency from Ex{ownerType}{extensionArea}_Currency where ExId=@ExId and DocId=@DocId;
                        SELECT fieldId,Value from Ex{ownerType}{extensionArea}_Date where ExId=@ExId and DocId=@DocId;
                        SELECT fieldId,Value from Ex{ownerType}{extensionArea}_SmallText where ExId=@ExId and DocId=@DocId;
                        SELECT fieldId,Value from Ex{ownerType}{extensionArea}_LargeText where ExId=@ExId and DocId=@DocId;
                        SELECT fieldId,Value from Ex{ownerType}{extensionArea}_Text where ExId=@ExId and DocId=@DocId;
                    ";

            var parameters = new { ExId = exId, DocId = docId };

            await using var multi = await _readRepository.GetLazyRepository<object>().Value.GetMultipleQueryAsync(sql, cancellationToken, parameters, null, "text");

            // Tag
            var tag = (await multi.ReadAsync<string>()).FirstOrDefault();
            data["Tag"] = tag ?? string.Empty;

            // Int
            foreach (var row in await multi.ReadAsync<FieldValueResult>())
                data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Decimal
            foreach (var row in await multi.ReadAsync<FieldValueResult>())
                data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Currency
            foreach (var row in await multi.ReadAsync<CurrencyValueResult>())
            {
                data[row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;
                data[$"{row.fieldId}-Currency"] = row.Currency;
            }

            // Date
            foreach (var row in await multi.ReadAsync<FieldValueResult>())
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
            foreach (var row in await multi.ReadAsync<FieldValueResult>())
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!data.ContainsKey(key))
                    data[key] = val;
                else
                    data[key] += VALUE_DELIMITER + val;
            }

            // LargeText
            foreach (var row in await multi.ReadAsync<FieldValueResult>())
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!data.ContainsKey(key))
                    data[key] = val;
                else
                    data[key] += VALUE_DELIMITER + val;
            }

            // Text
            foreach (var row in await multi.ReadAsync<FieldValueResult>())
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
                var fieldSetData = await GetFieldSetData(repo, schema, fset, docId, cancellationToken);
                data["$" + fset.Name] = fieldSetData;
            }

            return data;
        }
        private async Task<Dictionary<int, Dictionary<string, object>>> GetFieldSetData(IReadRepository<object> repo, EntityExtensionSchema schema, EntityExtensionFieldSet fset, int docId, CancellationToken cancellationToken)
        {
            var rowData = new Dictionary<int, Dictionary<string, object>>();
            var ownerType = schema.OwnerType;
            var extensionArea = schema.ExtensionArea;

            var sql = $@"
            SELECT rowId,IsNull(Tag,'') as Tag from Ex{ownerType}{extensionArea}_FieldSet_Master where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
            SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Int where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
            SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Decimal where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
            SELECT rowId,fieldId,Value,Currency from Ex{ownerType}{extensionArea}_FieldSet_Currency where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
            SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Date where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
            SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_SmallText where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
            SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_LargeText where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
            SELECT rowId,fieldId,Value from Ex{ownerType}{extensionArea}_FieldSet_Text where ExId=@ExId and DocId=@DocId and FieldSetId=@FieldSetId;
        ";
            var parameters = new { schema.ExId, DocId = docId, FieldSetId = fset.Id };

            await using var multi = await repo.GetMultipleQueryAsync(sql, cancellationToken, parameters, null, "text");

            // Master
            foreach (var row in await multi.ReadAsync<dynamic>())
            {
                int rowId = row.rowId;
                rowData[rowId] = new Dictionary<string, object> { { "Tag", row.Tag } };
            }

            // Int
            foreach (var row in await multi.ReadAsync<dynamic>())
                rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Decimal
            foreach (var row in await multi.ReadAsync<dynamic>())
                rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;

            // Currency
            foreach (var row in await multi.ReadAsync<dynamic>())
            {
                rowData[row.rowId][row.fieldId.ToString(CultureInfo.InvariantCulture)] = row.Value;
                rowData[row.rowId][$"{row.fieldId}-Currency"] = row.Currency;
            }

            // Date
            foreach (var row in await multi.ReadAsync<dynamic>())
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
            foreach (var row in await multi.ReadAsync<dynamic>())
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!rowData[row.rowId].ContainsKey(key))
                    rowData[row.rowId][key] = val;
                else
                    rowData[row.rowId][key] += VALUE_DELIMITER + val;
            }

            // LargeText
            foreach (var row in await multi.ReadAsync<dynamic>())
            {
                var key = row.fieldId.ToString(CultureInfo.InvariantCulture);
                var val = row.Value as string;
                if (!rowData[row.rowId].ContainsKey(key))
                    rowData[row.rowId][key] = val;
                else
                    rowData[row.rowId][key] += VALUE_DELIMITER + val;
            }

            // Text
            foreach (var row in await multi.ReadAsync<dynamic>())
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
