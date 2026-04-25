using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Preferences.GetOptInCurrentsBySyncGuid;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Preferences.GetOptInMasterByOwner
{
    public class GetOptInMasterByOwnerHandler : IRequestHandler<GetOptInMasterByOwnerQuery, OptInMaster>
    {
        private readonly LazyService<IReadRepository<OptInMaster>> _readRepository;

        public GetOptInMasterByOwnerHandler(LazyService<IReadRepository<OptInMaster>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<OptInMaster> Handle(GetOptInMasterByOwnerQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT om.*
	                            ,CONVERT(VARCHAR(5),om.LastModifiedDate,108) LastModifiedTime
	                            ,og.*
	                            ,o.*
	                            ,och.*
                            FROM OptInMaster om
	                            LEFT JOIN OptInGroup og ON om.Id=og.OptInMasterId
	                            LEFT JOIN OptIn o ON og.Id=o.OptInGroupId
	                            LEFT JOIN OptInChangeHistory och ON om.Id=och.OptInMasterId
                            WHERE OwnerType=@OwnerType 
	                            AND OwnerId=@OwnerId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OwnerType", request.OwnerType);
            queryParameters.Add("@OwnerId", request.OwnerId, dbType: DbType.Int32);
            var result = await _readRepository.Value.GetMultiMappingAsync<OptInMaster, OptInGroup, OptIn, OptInChangeHistory>(
                sql
                , cancellationToken
                , "Id"
                , (master, group, optIn, history) =>
                {
                    master.Groups ??= new List<OptInGroup>();
                    var existingGroup = master.Groups.FirstOrDefault(g => g.Id == group.Id);
                    if (existingGroup == null)
                    {
                        existingGroup = group;
                        existingGroup.OptIns = new List<OptIn>();
                        master.Groups.Add(existingGroup);
                    }

                    if (optIn != null && !existingGroup.OptIns.Any(o => o.Id == optIn.Id))
                        existingGroup.OptIns.Add(optIn);

                    master.ChangeHistorys ??= new List<OptInChangeHistory>();
                    if (history != null && !master.ChangeHistorys.Any(h => h.Id == history.Id))
                        master.ChangeHistorys.Add(history);

                    return master;
                }
                , queryParameters
                , null
                , splitOn: "Id,Id,Id"
                , "text");

            return result;
        }
    }
}
