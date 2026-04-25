using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionSchemaById;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiSchemaById;
using JustGo.FieldManagement.Domain.Entities;
using Newtonsoft.Json;

namespace JustGo.FieldManagement.Application.Features.EntityData.Commands.CreateSpecificEntityData
{
    public class CreateSpecificEntityDataHandler : IRequestHandler<CreateSpecificEntityDataCommand, int>
    {
        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IAzureBlobFileService _azureBlobFileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly ICustomError _error;

        public CreateSpecificEntityDataHandler(IMediator mediator, IWriteRepositoryFactory writeRepository, IAzureBlobFileService azureBlobFileService, IUnitOfWork unitOfWork, IUtilityService utilityService, ICustomError error)
        {
            _mediator = mediator;
            _writeRepository = writeRepository;
            _azureBlobFileService = azureBlobFileService;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _error = error;
        }

        public async Task<int> Handle(CreateSpecificEntityDataCommand request, CancellationToken cancellationToken = default)
        {
            var result = await SaveExtensionData(request.ExId,request.ItemId, request.EntityId, request.Data, cancellationToken);

            try
            {
                List<dynamic> audits = new List<dynamic>();
                var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

                // Get the schema for audit logging
                var schema = await _mediator.Send(new GetEntityExtensionSchemaByIdQuery(request.ExId, false), cancellationToken);
                if (schema is null)
                {
                    _error.NotFound<object>("Schema not found.");
                    return 0;
                }

                foreach (var dt in request.Data)
                {
                    var field = schema.Fields.Find(f => f.Id == Convert.ToInt32(dt.Key));
                    audits.Add(new
                    {
                        FieldName = field?.Name,
                        FieldId = Convert.ToInt32(dt.Key),
                        dt.Value
                    });
                }

                if (schema.ExtensionArea == "Profile" && schema.OwnerType == "Ngb")
                {
                    CustomLog.Event(
                        "User Changed|Basic Details|Profile",
                        currentUserId,
                        request.EntityId,
                        EntityType.User,
                        request.EntityId,
                        "Field Management Update;" + JsonConvert.SerializeObject(audits)
                    );
                }
            }
            catch (Exception)
            {
                // Optionally log the exception, but do not rethrow to avoid breaking the flow
            }

            return result;
        }
        private async Task<int> SaveExtensionData(int exId, string itemId, int entityId, Dictionary<string, object> Data, CancellationToken cancellationToken)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            var schema = await _mediator.Send(new GetEntityExtensionUiSchemaByIdQuery(itemId), cancellationToken);
            if (schema is null)
            {
                _error.NotFound<object>("Schema not found.");
                return 0;
            }

            if (!schema.IsInUse)
            {
                schema.IsInUse = true;
                schema.SaveSchema = true;
            }

            var attachmentField = new List<int>();
            var attachmentFieldData = new Dictionary<string, object>();
            foreach (var dt in Data)
            {
                bool isAttachmentField = false;
                if (dt.Key == "Tag") continue;
                if (dt.Key.StartsWith("$"))
                {
                    // Handle fieldset data
                    await SaveFieldSetDataAsync(exId, entityId, dt, schema, cancellationToken, transaction);
                    continue;
                }
                if (dt.Key.EndsWith("-Currency")) continue;
                if (dt.Key.Equals("SchemeId")) continue;

                var field = schema.Fields.FirstOrDefault(f => f.Id == Convert.ToInt32(dt.Key));
                if (field == null) continue;

                // Prepare parameters
                var value = dt.Value;
                var fieldType = field.Type.ToString();
                var tablePrefix = $"Ex{schema.OwnerType}{schema.ExtensionArea}";
                var parameters = new DynamicParameters();
                parameters.Add("ExId", exId);
                parameters.Add("DocId", entityId);
                parameters.Add("FieldId", field.Id);

                if (field.Type != ExtensionFieldDataType.Currency)
                {
                    switch (field.Type)
                    {
                        case ExtensionFieldDataType.Decimal:
                            parameters.Add("Value", Convert.ToDecimal(value));
                            break;
                        case ExtensionFieldDataType.Date:
                            if (value != null)
                            {
                                DateTime dateValue;
                                bool isDate = DateTime.TryParse(value.ToString(), out dateValue);
                                parameters.Add("Value", isDate ? dateValue.ToString("yyyy-MM-dd") : value);
                            }
                            else
                                parameters.Add("Value", value);
                            break;
                        default:
                            parameters.Add("Value", value);
                            break;
                    }

                    // Multi-value
                    if (field.IsMultiValue)
                    {
                        // Remove old values
                        var deleteSql = $"DELETE {tablePrefix}_{fieldType} WHERE ExId=@ExId AND DocId=@DocId AND FieldId=@FieldId";
                        await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(deleteSql, cancellationToken, parameters, transaction, "text");

                        if (value != null)
                        {
                            var selectedUiComp = schema.UiComps.FirstOrDefault(f => f.FieldId == field.Id);
                            if (selectedUiComp != null && selectedUiComp.Class == "MA_Attachment")
                            {
                                attachmentField.Add(field.Id);
                                isAttachmentField = true;
                            }
                            var values = value.ToString().Split('|');
                            if (isAttachmentField)
                            {
                                attachmentFieldData.Add(field.Id.ToString(), string.Join("|", values));
                            }
                            foreach (var v in values)
                            {
                                parameters.Add("Value", isAttachmentField ? v.Replace("temp_", "") : v);
                                // If allowed values, set FieldValueId
                                int fieldValueId = -1;
                                if (!isAttachmentField && field.AllowedValues != null && field.AllowedValues.Count > 0)
                                {
                                    var fieldValue = field.AllowedValues.FirstOrDefault(fv => fv.FieldId == field.Id && fv.Value == v);
                                    fieldValueId = fieldValue != null ? fieldValue.FieldValueId : -1;
                                }
                                parameters.Add("FieldValueId", fieldValueId);

                                var insertSql = $"INSERT INTO {tablePrefix}_{fieldType} (ExId, FieldId, DocId, Value, FieldValueId) VALUES (@ExId, @FieldId, @DocId, @Value, @FieldValueId)";
                                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(insertSql, cancellationToken, parameters, transaction, "text");
                            }
                        }
                    }
                    else
                    {
                        // Single value
                        int fieldValueId = -1;
                        if (field.AllowedValues != null && field.AllowedValues.Count > 0 && value != null)
                        {
                            var fieldValue = field.AllowedValues.FirstOrDefault(fv => fv.FieldId == field.Id && fv.Value == value.ToString());
                            fieldValueId = fieldValue != null ? fieldValue.FieldValueId : -1;
                        }
                        parameters.Add("FieldValueId", fieldValueId);

                        var upsertSql = fieldValueId != -1
                            ? $"MERGE INTO {tablePrefix}_{fieldType} AS target " +
                              "USING (SELECT @ExId AS ExId, @DocId AS DocId, @FieldId AS FieldId) AS source " +
                              "ON (target.ExId = source.ExId AND target.DocId = source.DocId AND target.FieldId = source.FieldId) " +
                              "WHEN MATCHED THEN UPDATE SET Value=@Value, FieldValueId=@FieldValueId " +
                              "WHEN NOT MATCHED THEN INSERT (ExId, FieldId, DocId, Value, FieldValueId) VALUES (@ExId, @FieldId, @DocId, @Value, @FieldValueId);"
                            : $"MERGE INTO {tablePrefix}_{fieldType} AS target " +
                              "USING (SELECT @ExId AS ExId, @DocId AS DocId, @FieldId AS FieldId) AS source " +
                              "ON (target.ExId = source.ExId AND target.DocId = source.DocId AND target.FieldId = source.FieldId) " +
                              "WHEN MATCHED THEN UPDATE SET Value=@Value " +
                              "WHEN NOT MATCHED THEN INSERT (ExId, FieldId, DocId, Value) VALUES (@ExId, @FieldId, @DocId, @Value);";

                        // Only use FieldValueId for fields/tables that support it
                        await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(upsertSql, cancellationToken, parameters, transaction, "text");
                    }
                }
                else
                {
                    // Currency
                    parameters.Add("Value", Convert.ToDecimal(value));
                    parameters.Add("Currency", Data[field.Id + "-Currency"]);
                    var upsertSql = $"MERGE INTO {tablePrefix}_{fieldType} AS target " +
                                    "USING (SELECT @ExId AS ExId, @DocId AS DocId, @FieldId AS FieldId) AS source " +
                                    "ON (target.ExId = source.ExId AND target.DocId = source.DocId AND target.FieldId = source.FieldId) " +
                                    "WHEN MATCHED THEN UPDATE SET Value=@Value, Currency=@Currency " +
                                    "WHEN NOT MATCHED THEN INSERT (ExId, FieldId, DocId, Value, Currency) VALUES (@ExId, @FieldId, @DocId, @Value, @Currency);";
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(upsertSql, cancellationToken, parameters, transaction, "text");
                }
            }

            if (attachmentField.Count > 0)
            {
                await SaveEntityExtensionFieldsetAttachment(attachmentField, attachmentFieldData, cancellationToken);
            }

            await _unitOfWork.CommitAsync(transaction);
            return entityId;
        }
        private async Task SaveFieldSetDataAsync(int exId, int entityId, KeyValuePair<string, object> fieldSetData, EntityExtensionSchema schema, CancellationToken cancellationToken, IDbTransaction transaction)
        {
            var fset = schema.FieldSets.FirstOrDefault(fs =>
                fs.Name.Equals(fieldSetData.Key.Replace("$", string.Empty), StringComparison.InvariantCultureIgnoreCase));
            if (fset == null) return;

            if (!fset.IsInUse)
            {
                fset.IsInUse = true;
            }

            Dictionary<int, Dictionary<string, object>> rowData;
            if (fieldSetData.Value is string str && string.IsNullOrEmpty(str))
            {
                rowData = new Dictionary<int, Dictionary<string, object>>();
            }
            else if (fieldSetData.Value is Newtonsoft.Json.Linq.JObject jObj)
            {
                rowData = jObj.ToObject<Dictionary<int, Dictionary<string, object>>>();
            }
            else
            {
                rowData = (Dictionary<int, Dictionary<string, object>>)fieldSetData.Value;
            }


            var tablePrefix = $"Ex{schema.OwnerType}{schema.ExtensionArea}";
            var attachmentField = new List<int>();
            var attachmentFieldData = new Dictionary<string, object>();
            var rowIds = new List<int>();

            foreach (var row in rowData)
            {
                var rowId = row.Key;
                var tag = row.Value.ContainsKey("Tag") ? row.Value["Tag"]?.ToString() : string.Empty;

                var masterParams = new DynamicParameters();
                masterParams.Add("ExId", schema.ExId);
                masterParams.Add("DocId", entityId);
                masterParams.Add("FieldSetId", fset.Id);
                masterParams.Add("RowId", rowId);
                masterParams.Add("Tag", tag);
                masterParams.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var masterSql = $@"
                                IF NOT EXISTS (SELECT 1 FROM {tablePrefix}_FieldSet_Master WHERE ExId=@ExId AND DocId=@DocId AND FieldSetId=@FieldSetId AND RowId=@RowId)
                                BEGIN
                                    INSERT INTO {tablePrefix}_FieldSet_Master (ExId, DocId, FieldSetId, Tag)
                                    VALUES (@ExId, @DocId, @FieldSetId, @Tag)
                                    SET @NewId = SCOPE_IDENTITY()
                                END
                                ELSE
                                BEGIN
                                    SET @NewId = @RowId
                                END
                            ";
                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(masterSql, cancellationToken, masterParams, transaction, "text");
                var actualRowId = masterParams.Get<int>("NewId");
                rowIds.Add(actualRowId);

                foreach (var fv in row.Value)
                {
                    if (fv.Key == "Tag" || fv.Key.EndsWith("-Currency")) continue;

                    var field = fset.Fields.FirstOrDefault(f => f.Id == Convert.ToInt32(fv.Key));
                    if (field == null) continue;

                    var value = fv.Value;
                    var fieldType = field.Type.ToString();

                    if (field.Type != ExtensionFieldDataType.Currency)
                    {
                        var fieldParams = new DynamicParameters();
                        fieldParams.Add("FieldId", field.Id);
                        fieldParams.Add("ExId", schema.ExId);
                        fieldParams.Add("DocId", entityId);
                        fieldParams.Add("FieldSetId", fset.Id);
                        fieldParams.Add("RowId", actualRowId);

                        switch (field.Type)
                        {
                            case ExtensionFieldDataType.Decimal:
                                fieldParams.Add("Value", Convert.ToDecimal(value));
                                break;
                            case ExtensionFieldDataType.Date:
                                if (value != null && DateTime.TryParse(value.ToString(), out var dateValue))
                                    fieldParams.Add("Value", dateValue.ToString("yyyy-MM-dd"));
                                else
                                    fieldParams.Add("Value", value);
                                break;
                            default:
                                fieldParams.Add("Value", value);
                                break;
                        }

                        // Multi-value
                        if (field.IsMultiValue)
                        {
                            var selectedUiComp = schema.UiComps.FirstOrDefault(f => f.FieldId == field.Id);
                            bool isAttachmentField = selectedUiComp != null && selectedUiComp.Class == "MA_Attachment";
                            var deleteSql = $"DELETE {tablePrefix}_FieldSet_{fieldType} WHERE ExId=@ExId AND DocId=@DocId AND FieldId=@FieldId AND FieldSetId=@FieldSetId AND RowId=@RowId";
                            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(deleteSql, cancellationToken, fieldParams, transaction, "text");

                            if (value != null)
                            {
                                var values = value.ToString().Split('|');
                                if (isAttachmentField)
                                {
                                    attachmentField.Add(field.Id);
                                    attachmentFieldData.Add(field.Id.ToString(), string.Join("|", values));
                                }
                                foreach (var v in values)
                                {
                                    fieldParams.Add("Value", isAttachmentField ? v.Replace("temp_", "") : v);
                                    var insertSql = $"INSERT INTO {tablePrefix}_FieldSet_{fieldType} (ExId, FieldId, DocId, FieldSetId, RowId, Value) VALUES (@ExId, @FieldId, @DocId, @FieldSetId, @RowId, @Value)";
                                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(insertSql, cancellationToken, fieldParams, transaction, "text");
                                }
                            }
                        }
                        else
                        {
                            var upsertSql = $@"
                        IF NOT EXISTS (SELECT 1 FROM {tablePrefix}_FieldSet_{fieldType} WHERE ExId=@ExId AND DocId=@DocId AND FieldId=@FieldId AND FieldSetId=@FieldSetId AND RowId=@RowId)
                        BEGIN
                            INSERT INTO {tablePrefix}_FieldSet_{fieldType} (ExId, FieldId, DocId, FieldSetId, RowId, Value)
                            VALUES (@ExId, @FieldId, @DocId, @FieldSetId, @RowId, @Value)
                        END
                        ELSE
                        BEGIN
                            UPDATE {tablePrefix}_FieldSet_{fieldType} SET Value=@Value
                            WHERE ExId=@ExId AND DocId=@DocId AND FieldId=@FieldId AND FieldSetId=@FieldSetId AND RowId=@RowId
                        END
                    ";
                            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(upsertSql, cancellationToken, fieldParams, transaction, "text");
                        }
                    }
                    else
                    {
                        // Currency
                        var currencyParams = new DynamicParameters();
                        currencyParams.Add("FieldId", field.Id);
                        currencyParams.Add("ExId", schema.ExId);
                        currencyParams.Add("DocId", entityId);
                        currencyParams.Add("FieldSetId", fset.Id);
                        currencyParams.Add("RowId", actualRowId);
                        currencyParams.Add("Value", Convert.ToDecimal(value));
                        currencyParams.Add("Currency", row.Value.ContainsKey(field.Id + "-Currency") ? row.Value[field.Id + "-Currency"] : string.Empty);

                        var upsertSql = $@"
                    IF NOT EXISTS (SELECT 1 FROM {tablePrefix}_FieldSet_{fieldType} WHERE ExId=@ExId AND DocId=@DocId AND FieldId=@FieldId AND FieldSetId=@FieldSetId AND RowId=@RowId)
                    BEGIN
                        INSERT INTO {tablePrefix}_FieldSet_{fieldType} (ExId, FieldId, DocId, FieldSetId, RowId, Value, Currency)
                        VALUES (@ExId, @FieldId, @DocId, @FieldSetId, @RowId, @Value, @Currency)
                    END
                    ELSE
                    BEGIN
                        UPDATE {tablePrefix}_FieldSet_{fieldType} SET Value=@Value, Currency=@Currency
                        WHERE ExId=@ExId AND DocId=@DocId AND FieldId=@FieldId AND FieldSetId=@FieldSetId AND RowId=@RowId
                    END
                ";
                        await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(upsertSql, cancellationToken, currencyParams, transaction, "text");
                    }
                }

                if (attachmentField.Count > 0)
                {
                    await SaveEntityExtensionFieldsetAttachment(attachmentField, attachmentFieldData, cancellationToken);
                    attachmentField.Clear();
                    attachmentFieldData.Clear();
                }
            }

            var rowIdsStr = rowIds.Count > 0 ? string.Join(",", rowIds) : "-1";
            var deleteTypes = new[] { "Master", "Int", "Decimal", "Currency", "Date", "SmallText", "LargeText", "Text" };
            foreach (var type in deleteTypes)
            {
                var deleteSql = $"DELETE {tablePrefix}_FieldSet_{type} WHERE ExId=@ExId AND DocId=@DocId AND FieldSetId=@FieldSetId AND RowId NOT IN ({rowIdsStr})";
                var deleteParams = new DynamicParameters();
                deleteParams.Add("ExId", schema.ExId);
                deleteParams.Add("DocId", entityId);
                deleteParams.Add("FieldSetId", fset.Id);
                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(deleteSql, cancellationToken, deleteParams, transaction, "text");
            }

        }
        private async Task SaveEntityExtensionFieldsetAttachment(List<int> fieldIds, Dictionary<string, object> data, CancellationToken cancellationToken)
        {
            foreach (var fieldId in fieldIds)
            {
                if (data.ContainsKey(fieldId.ToString()))
                {
                    var result = data[fieldId.ToString()];
                    string[] attachmentPaths = result.ToString().Split('|');

                    foreach (var attachmentPath in attachmentPaths)
                    {
                        if (string.IsNullOrEmpty(attachmentPath))
                            continue;

                        try
                        {
                            if (attachmentPath.ToLower().StartsWith("temp") || attachmentPath.ToLower().IndexOf("copy|", StringComparison.Ordinal) > -1)
                            {
                                bool isCopy = attachmentPath.IndexOf("copy|", StringComparison.Ordinal) > -1;
                                string[] parts = isCopy ? attachmentPath.Split('|') : null;
                                if (isCopy && (parts == null || parts.Length < 3))
                                    continue;

                                int souceDocId = isCopy ? Convert.ToInt32(parts[1]) : 0;
                                var fileName = isCopy ? parts[2] : Path.GetFileName(attachmentPath);

                                fileName = fileName.Replace("temp_", "");

                                string sourcePath = isCopy
                                    ? await _azureBlobFileService.MapPath($"~/store/fieldmanagementattachment/{fieldId}/" + fileName)
                                    : await _azureBlobFileService.MapPath("~/store/Temp/fieldmanagementattachment/attachments/" + fileName);

                                var destinationPath = await _azureBlobFileService.MapPath($"~/store/fieldmanagementattachment/{fieldId}/");

                                if (isCopy)
                                {
                                    await _azureBlobFileService.CopyFileAsync(sourcePath, $"{destinationPath}{fileName}", cancellationToken);
                                }
                                else if (await _azureBlobFileService.Exists(sourcePath, cancellationToken))
                                {
                                    await _azureBlobFileService.MoveFileAsync(sourcePath, $"{destinationPath}{fileName}", cancellationToken);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Optionally log the exception
                            // Log.Error(ex, $"Failed to process attachment: {attachmentPath}");
                        }
                    }
                }
            }
        }
    }
}
