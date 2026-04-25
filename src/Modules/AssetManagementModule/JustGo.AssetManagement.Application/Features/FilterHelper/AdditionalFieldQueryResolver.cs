using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Dapper;

namespace JustGo.AssetManagement.Application.Features.FilterHelper
{
    public class AdditionalFieldQueryResolver
    {

        public static async Task<Dictionary<string, string>> GetDynamicFormTableNames
         (IReadRepositoryFactory _readRepository,
          List<SearchSegmentDTO> SearchItems,
          CancellationToken cancellationToken)
        {
            var fieldIds = SearchItems.
                           Where(r => !string.IsNullOrEmpty(r.FieldId)).
                           Select(r => r.FieldId).ToList();


            if (fieldIds.Any())
            {

                string query = $@"select f.SyncGuid [Text],
                                concat
                                ('ExNGBAsset_', 
                                  case 
                                    when f.DataType = 0 then 'Int'
	                                when f.DataType = 1 then 'Decimal'
	                                when f.DataType = 2 then 'Currency'
	                                when f.DataType = 3 then 'Date'
                                    when f.DataType = 4 then 'SmallText'
	                                when f.DataType = 5 then 'Text'
                                    when f.DataType = 6 then 'LargeText'
	                                else ''
                                  end
                                ) [Value]
                                from EntityExtensionField f where f.SyncGuid in @fieldIds";

                DynamicParameters queryParameters = new DynamicParameters();
                queryParameters.Add("@fieldIds", fieldIds);
                var data = (await _readRepository.
                    GetLazyRepository<SelectListItemDTO<string>>().Value.
                    GetListAsync(query, cancellationToken, queryParameters, null, "text")).ToList();



                return data.ToDictionary(r => r.Text, r => r.Value);
            }


            return new Dictionary<string, string>();

        }

        private static async Task<Dictionary<string, int>> GetDynamicFormFieldIds
         (IReadRepositoryFactory _readRepository,
          List<SearchSegmentDTO> SearchItems,
          CancellationToken cancellationToken)
        {
            var fieldIds = SearchItems.
                           Where(r => !string.IsNullOrEmpty(r.FieldId)).
                           Select(r => r.FieldId).ToList();


            if (fieldIds.Any())
            {

                string query = $@"select f.SyncGuid [Text],
                                 Id [Value]
                                from EntityExtensionField f where f.SyncGuid in @fieldIds";

                DynamicParameters queryParameters = new DynamicParameters();
                queryParameters.Add("@fieldIds", fieldIds);
                var data = (await _readRepository.
                    GetLazyRepository<SelectListItemDTO<int>>().Value.
                    GetListAsync(query, cancellationToken, queryParameters, null, "text")).ToList();



                return data.ToDictionary(r => r.Text, r => r.Value);
            }


            return new Dictionary<string, int>();

        }

        public static async Task<string> MakeJoinsForDynamicFormData(
                IReadRepositoryFactory _readRepository,
                List<SearchSegmentDTO> searchItems,
                string joinWith,
                CancellationToken cancellationToken)
        {
            var tables = await GetDynamicFormTableNames(_readRepository, searchItems, cancellationToken);
            var fieldIds = await GetDynamicFormFieldIds(_readRepository, searchItems, cancellationToken);

            var joins = "";

            for (int i = 0; i < searchItems.Count(); i++)
            {
                var item = searchItems[i];

                if (!string.IsNullOrEmpty(item.FieldId))
                {
                    joins += " left join " + tables[item.FieldId] + " adf" + i + " on " + " adf" + i + ".DocId = " + joinWith + " and  adf" + i + ".FieldId = " + fieldIds[item.FieldId]+" ";
                }
            }

            return joins;


        }

    }
}
