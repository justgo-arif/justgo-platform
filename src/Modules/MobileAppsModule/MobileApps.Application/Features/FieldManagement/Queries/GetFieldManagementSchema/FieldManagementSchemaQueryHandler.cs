using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFieldManagementSchema
{
    class FieldManagementSchemaQueryHandler:IRequestHandler<FieldManagementSchemaQuery, EntityExtensionSchema>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        public FieldManagementSchemaQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<EntityExtensionSchema> Handle(FieldManagementSchemaQuery request, CancellationToken cancellationToken)
        {
            var schema = new EntityExtensionSchema();

            // Common parameter setup
            var param = new DynamicParameters();
            param.Add("@ExId", request.ExId);

            // Fetch Field Sets
            var fieldSetRaw = await _readRepository.Value.GetListAsync("SELECT * FROM EntityExtensionFieldSet WHERE EXID=@ExId", param, null, "text");
            if (fieldSetRaw.Any())
            {
                var fieldSets = JsonConvert.DeserializeObject<List<EntityExtensionFieldSet>>(JsonConvert.SerializeObject(fieldSetRaw));
                schema.FieldSets.AddRange(fieldSets);
            }

            // Fetch Fields
            var fieldRaw = await _readRepository.Value.GetListAsync("SELECT * FROM EntityExtensionField WHERE EXID=@ExId", param, null, "text");
            if (fieldRaw.Any())
            {
                var fields = JsonConvert.DeserializeObject<List<EntityExtensionField>>(JsonConvert.SerializeObject(fieldRaw));
                schema.Fields.AddRange(fields);
            }

            // Fetch Field Values for schema.Fields
            var fieldIds = schema.Fields.Select(f => f.Id).ToList();
            if (fieldIds.Any())
            {
                var fieldIdsParam = string.Join(",", fieldIds);
                var fieldValueRaw = await _readRepository.Value.GetListAsync(
                    "SELECT * FROM EntityExtensionFieldValues WHERE FieldId IN (SELECT value FROM STRING_SPLIT(@FieldIds,',')) ORDER BY sequence",
                    new DynamicParameters(new { FieldIds = fieldIdsParam }),
                    null, "text");

                var fieldValuesDict = fieldValueRaw
                    .GroupBy(fv => fv.FieldId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var field in schema.Fields)
                {
                    if (fieldValuesDict.TryGetValue(field.Id, out var values))
                        field.AllowedValues.AddRange(JsonConvert.DeserializeObject<List<FieldValue>>(JsonConvert.SerializeObject(values)));
                }
            }

            // Fetch Field Values for schema.FieldSets
            var fsFieldIds = schema.FieldSets.SelectMany(fs => fs.Fields).Select(f => f.Id).ToList();
            if (fsFieldIds.Any())
            {
                var fsFieldIdsParam = string.Join(",", fsFieldIds);
                var fsFieldValueRaw = await _readRepository.Value.GetListAsync(
                    "SELECT * FROM EntityExtensionFieldValues WHERE FieldId IN (SELECT value FROM STRING_SPLIT(@FieldIds,',')) ORDER BY sequence",
                    new DynamicParameters(new { FieldIds = fsFieldIdsParam }),
                    null, "text");

                var fsFieldValuesDict = fsFieldValueRaw
                    .GroupBy(fv => fv.FieldId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var fs in schema.FieldSets)
                {
                    foreach (var field in fs.Fields)
                    {
                        if (fsFieldValuesDict.TryGetValue(field.Id, out var values))
                            field.AllowedValues.AddRange(JsonConvert.DeserializeObject<List<FieldValue>>(JsonConvert.SerializeObject(values)));
                    }
                }
            }

            // Fetch UI components
            var uiRaw = await _readRepository.Value.GetListAsync("SELECT * FROM EntityExtensionUI WHERE EXID=@ExId", param, null, "text");
            if (uiRaw.Any())
            {
                var uiComps = JsonConvert.DeserializeObject<List<ExtensionUI>>(JsonConvert.SerializeObject(uiRaw));
                schema.UiComps.AddRange(uiComps);
            }

            return schema;

        }
    }
}
