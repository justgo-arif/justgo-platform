using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiSchemaById
{
    public class GetEntityExtensionUiSchemaByIdHandler : IRequestHandler<GetEntityExtensionUiSchemaByIdQuery, EntityExtensionSchema>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetEntityExtensionUiSchemaByIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<EntityExtensionSchema> Handle(GetEntityExtensionUiSchemaByIdQuery request, CancellationToken cancellationToken = default)
        {
            EntityExtensionSchema schema = null;
            if (!request.IsArena)
                schema = null;

            if (schema != null) return schema;
            var sql = $"{schemaQuery}{fieldQuery}{fieldValueQuery}{fieldsetQuery}{uiQuery}";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@SyncGuid", request.Id);
            await using var multi = await _readRepository.GetLazyRepository<dynamic>().Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");
            schema = (await multi.ReadAsync<EntityExtensionSchema>()).FirstOrDefault();
            var fields = (await multi.ReadAsync<EntityExtensionField>()).ToList();
            var fieldValues = (await multi.ReadAsync<EntityExtensionFieldValue>()).ToList();
            foreach (var field in fields)
            {
                field.AllowedValues = fieldValues.Where(fv => fv.FieldId == field.Id).ToList();
            }
            schema.Fields = new List<EntityExtensionField>();
            foreach (var field in fields)
            {
                if (field.FieldSetId == 0)
                {
                    schema.Fields.Add(field);
                }
            }
            schema.FieldSets = (await multi.ReadAsync<EntityExtensionFieldSet>()).ToList();
            foreach (var fset in schema.FieldSets)
            {
                fset.Fields = fields.Where(f => f.FieldSetId == fset.Id).ToList();
            }            
            schema.UiComps = (await multi.ReadAsync<EntityExtensionUI>()).ToList();
            return schema;
        }

        private const string schemaQuery =
            """
            SELECT s.[ExId]
                  ,s.[OwnerType]
                  ,s.[OwnerId]
                  ,s.[ExtensionArea]
                  ,s.[ExtensionEntityId]
                  ,s.[IsInUse]
                  ,s.[SyncGuid]
            FROM [dbo].[EntityExtensionSchema] s
                INNER JOIN [dbo].[EntityExtensionUI] ui
                    ON s.ExId=ui.ExId
            WHERE ui.SyncGuid= @SyncGuid;
            """;
        private const string fieldQuery =
            """
            WITH RecursiveChildren AS (
                SELECT ItemId, ParentId
                FROM [EntityExtensionUI]
                WHERE ParentId = (SELECT ItemId FROM [EntityExtensionUI]
            		WHERE [SyncGuid] = @SyncGuid)

                UNION ALL

                SELECT c.ItemId, c.ParentId
                FROM [EntityExtensionUI] c
                INNER JOIN RecursiveChildren rc ON c.ParentId = rc.ItemId
            )
            SELECT f.[ExId]
                  ,f.[Id]
                  ,f.[Name]
                  ,f.[Caption]
                  ,f.[Description]
                  ,f.[DataType]
                  ,f.[IsInUse]
                  ,f.[SyncGuid]
                  ,f.[FieldSetId]
                  ,f.[IsMultiValue]
            FROM [dbo].[EntityExtensionField] f
               	INNER JOIN [dbo].[EntityExtensionUI] ui
                ON f.[Id] = ui.[FieldId]
            WHERE ui.ItemId IN (SELECT ItemId FROM RecursiveChildren);
            """;
        private const string fieldValueQuery =
            """
            SELECT fv.[FieldId]
                  ,fv.[Key]
                  ,fv.[Caption]
                  ,fv.[Description]
                  ,fv.[Lang]
                  ,fv.[Value]
                  ,fv.[Sequence]
                  ,fv.[FieldValueId]
            FROM [dbo].[EntityExtensionFieldValues] fv
               	INNER JOIN [dbo].[EntityExtensionField] f
                  		ON fv.[FieldId]=f.[Id]
               	INNER JOIN [dbo].[EntityExtensionUI] ui
                  		ON f.[ExId] = ui.[ExId]
            WHERE ui.[SyncGuid] = @SyncGuid;
            """;
        private const string fieldsetQuery =
            """
            SELECT fs.[ExId]
                  ,fs.[Id]
                  ,fs.[Name]
                  ,fs.[Caption]
                  ,fs.[Description]
                  ,fs.[IsInUse]
                  ,fs.[SyncGuid]
            FROM [dbo].[EntityExtensionFieldSet] fs
               	INNER JOIN [dbo].[EntityExtensionUI] ui
                  		ON fs.[ExId] = ui.[ExId]
            WHERE ui.[SyncGuid] = @SyncGuid;
            """;
        private const string uiQuery =
            """
            WITH RecursiveChildren AS (
                SELECT ItemId, ParentId
                FROM [EntityExtensionUI]
                WHERE ParentId = (SELECT ItemId FROM [EntityExtensionUI]
                  		WHERE [SyncGuid] = @SyncGuid)

                UNION ALL

                SELECT c.ItemId, c.ParentId
                FROM [EntityExtensionUI] c
                INNER JOIN RecursiveChildren rc ON c.ParentId = rc.ItemId
            )
            SELECT ui.[ExId],
                   ui.[ItemId],
                   ui.[ParentId],
                   ui.[Sequence],
                   ui.[Class],
                   ui.[Config],
                   ui.[FieldId],
                   ui.[SyncGuid]
            FROM [dbo].[EntityExtensionUI] ui
            WHERE ui.ItemId IN (SELECT ItemId FROM RecursiveChildren);
            """;


    }
}
