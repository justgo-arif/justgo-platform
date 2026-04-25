using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Text;


namespace JustGo.AssetManagement.Application.Features.FilterHelper
{
    public class SearchConditionResolver
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly CancellationToken _cancellationToken;
        public SearchConditionResolver(IMediator mediator, 
                                       IReadRepositoryFactory readRepository,
                                       CancellationToken cancellationToken) {
            _mediator = mediator;
            _readRepository = readRepository;
            _cancellationToken = cancellationToken;
        }

        public async Task<string> GetSQLConditions(
            DynamicParameters queryParameters,  
            List<SearchSegmentDTO> searchItems)
        {


            var adFtables = await AdditionalFieldQueryResolver.GetDynamicFormTableNames(_readRepository, searchItems, _cancellationToken);

            var sqlBuilder = new StringBuilder();

            if (searchItems.Any())
            {
                searchItems[searchItems.Count - 1].ConditionJoiner = "";
            }

            for (int i = 0; i < searchItems.Count; i++)
            {
                var item = searchItems[i];
                ColumnDefinition columnDef;

                if (string.IsNullOrEmpty(item.FieldId))
                {
                    columnDef = ColumnNameResolver.GetColumnName(item.ColumnName);
                }
                else
                {
                    if (adFtables[item.FieldId].Contains("LargeText"))
                    {
                        columnDef = new ColumnDefinition(
                            "FieldValueId",
                            "adf" + i + ".FieldValueId",
                            null,
                            "adf" + i,
                            "FieldValueId",
                            FieldType.text
                            );
                    }
                    else
                    {
                        columnDef = new ColumnDefinition(
                            "Value",
                            "adf" + i + ".Value",
                            null,
                            "adf" + i,
                            "Value",
                            FieldType.text
                            );

                    }

                }

                string columnName = columnDef.NameInQuery;
                string paramName = "@"+columnDef.NameInView+"_"+i;



                if (columnDef.FieldType == FieldType.guid)
                {
                    item.Value = string.Join(",",
                        await _mediator.Send(new GetIdByGuidQuery()
                        {
                            Entity = columnDef.GuidEntity??AssetTables.Document,
                            RecordGuids = item.Value.Split(',').ToList()
                        }));
                }

                sqlBuilder.Append(' ').Append(columnName);

                switch (item.Operator)
                {
                    case "equals":
                        queryParameters.Add(paramName, item.Value);
                        sqlBuilder.Append(" = ").Append(paramName).Append("");
                        break;

                    case "not equals":
                        queryParameters.Add(paramName, item.Value);
                        sqlBuilder.Append(" != ").Append(paramName).Append("");
                        break;

                    case "contains":
                        queryParameters.Add(paramName, "%" + item.Value + "%");
                        sqlBuilder.Append(" like (").Append(paramName).Append(")");
                        break;

                    case "not contains":
                        queryParameters.Add(paramName, "%"+item.Value+"%");
                        sqlBuilder.Append(" not like (").Append(paramName).Append(")");
                        break;

                    case "any":
                        var inValues = item.Value.Split(',');
                        queryParameters.Add(paramName, inValues);
                        sqlBuilder.Append(" in ").Append(paramName).Append(" ");
                        break;

                    case "not in":
                        var notInValues = item.Value.Split(',');
                        queryParameters.Add(paramName, notInValues);
                        sqlBuilder.Append(" not in ").Append(paramName).Append(" ");
                        break;

                    case "between":
                        var betweenValues = item.Value.Split(',');
                        queryParameters.Add(paramName + "_1", betweenValues[0]);
                        queryParameters.Add(paramName + "_2", betweenValues[1]);
                        sqlBuilder.Append($@" between {(paramName + "_1")} and {(paramName + "_2")} ");
                        break;

                    case "not exists":
                        sqlBuilder.Append(" = 0 ").Append(" ");
                        break;

                }

                if (i < searchItems.Count - 1)
                {
                    if (item.ConditionJoiner == "and")
                    {
                        sqlBuilder.Append(" and");
                    }
                    else if (item.ConditionJoiner == "or")
                    {
                        sqlBuilder.Append(" or");
                    }
                }
            }

            return sqlBuilder.ToString();
        }

    }
}
