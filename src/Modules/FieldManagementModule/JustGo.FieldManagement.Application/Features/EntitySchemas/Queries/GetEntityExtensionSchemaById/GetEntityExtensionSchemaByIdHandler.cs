using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionSchemaById
{
    public class GetEntityExtensionSchemaByIdHandler : IRequestHandler<GetEntityExtensionSchemaByIdQuery, EntityExtensionSchema>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetEntityExtensionSchemaByIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<EntityExtensionSchema> Handle(GetEntityExtensionSchemaByIdQuery request, CancellationToken cancellationToken)
        {
            var sql =
                """
                SELECT * FROM EntityExtensionSchema WHERE Exid=@ExId;
                SELECT * FROM EntityExtensionField WHERE EXID=@ExId;
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
                WHERE f.ExId = @ExId
                ORDER BY fv.Sequence;
                SELECT * FROM EntityExtensionFieldSet WHERE EXID=@ExId;                
                SELECT * FROM EntityExtensionUI WHERE EXID=@ExId;                
                """;

            await using var multi = await _readRepository
                .GetLazyRepository<object>()
                .Value
                .GetMultipleQueryAsync(sql, cancellationToken, new { ExId = request.ExId }, null, "text");

            var schema = (await multi.ReadAsync<EntityExtensionSchema>()).FirstOrDefault();
            if (schema == null)
                return null;

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

       
    }
}