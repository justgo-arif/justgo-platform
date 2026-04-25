using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetGuidMapById
{
    public class GetGuidMapByIdHandler : IRequestHandler<GetGuidMapByIdQuery, List<MapItemDTO<decimal, string>>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        public GetGuidMapByIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<MapItemDTO<decimal, string>>> Handle(GetGuidMapByIdQuery request, CancellationToken cancellationToken)
        {

            if(request.Entity != AssetTables.Document)
            {

                string pkSql = $@"SELECT TOP 1 KU.COLUMN_NAME PrimaryKey
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                ON TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                WHERE TC.CONSTRAINT_TYPE = 'PRIMARY KEY'
                AND KU.TABLE_NAME = @TableName";

                var queryParameters = new DynamicParameters();
                queryParameters.Add("@TableName", Utilities.GetEnumText(request.Entity));
                var pkData = await _readRepository.GetLazyRepository<PrimaryKeyDTO>().Value.GetAsync(pkSql, cancellationToken, queryParameters, null, "text");


                queryParameters = new DynamicParameters();
                queryParameters.Add("@Ids", request.Ids);
                queryParameters.Add("@Count", request.Ids.Count());
                string sql = $@"SELECT Top (@Count) 
                           {pkData.PrimaryKey} Key,
                           RecordGuid Value
                          FROM {request.Entity} WHERE  {pkData.PrimaryKey} in  @Ids";
                var data = (await _readRepository.GetLazyRepository<MapItemDTO<decimal, string>>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
                return data.ToList();
            }
            else
            {
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@Ids", request.Ids);
                queryParameters.Add("@Count", request.Ids.Count);
                string sql = $@" SELECT TOP (@Count) DocId [Key], SyncGuid Value FROM Document WHERE DocId in @Ids";


                var data = await _readRepository.GetLazyRepository<MapItemDTO<decimal, string>>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
                return data.ToList();
            }

        }
    }
}
