using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetGuidById
{
    public class GetGuidByIdHandler : IRequestHandler<GetGuidByIdQuery, List<string>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        public GetGuidByIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<string>> Handle(GetGuidByIdQuery request, CancellationToken cancellationToken)
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
                           RecordGuid RecordGuid
                          FROM {request.Entity} WHERE  {pkData.PrimaryKey} in  @Ids ";
                var data = (await _readRepository.GetLazyRepository<InsertedDataGuidDTO>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
                return data.Select(r => r.RecordGuid).ToList();
            }
            else
            {
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@Ids", request.Ids);
                queryParameters.Add("@Count", request.Ids.Count);
                string sql = $@" SELECT TOP (@Count) SyncGuid RecordGuid FROM Document WHERE DocId in 
                          @Ids";


                var data = await _readRepository.GetLazyRepository<InsertedDataGuidDTO>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
                return data.Select(r => r.RecordGuid).ToList();
            }

        }
    }
}
