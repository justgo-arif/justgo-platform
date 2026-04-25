using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid
{
    public class GetIdByGuidHandler : IRequestHandler<GetIdByGuidQuery, List<int>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        public GetIdByGuidHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<int>> Handle(GetIdByGuidQuery request, CancellationToken cancellationToken)
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
                queryParameters.Add("@RecordGuids", request.RecordGuids);
                queryParameters.Add("@Count", request.RecordGuids.Count());
                string sql = $@"SELECT Top (@Count) {pkData.PrimaryKey} Id
                          FROM [{request.Entity}] WHERE 
                          "+(request.Entity == AssetTables.User ? " UserSyncId " : " RecordGuid ") +$@" in @RecordGuids";
                var data = (await _readRepository.GetLazyRepository<InsertedDataIdDTO>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
                return data.Select(r => r.Id).ToList();
            }
            else
            {
                var queryParameters = new DynamicParameters();
                queryParameters.Add("@RecordGuids", request.RecordGuids);
                queryParameters.Add("@Count", request.RecordGuids.Count);
                string sql = $@" SELECT TOP (@Count) DocId Id FROM Document WHERE SyncGuid in 
                          @RecordGuids";


                var data = await _readRepository.GetLazyRepository<InsertedDataIdDTO>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
                return data.Select(r => r.Id).ToList();
            }
        }
    }
}
