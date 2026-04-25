using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities;
using MobileApps.Domain.Entities.V2.Clubs;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Club.Queries.GetClubList.V2
{
    class ClubEventWithClassFlagQueryHandler : IRequestHandler<ClubEventWithClassFlagQuery, List<ClubEventWithClassFlagResponseDto>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public ClubEventWithClassFlagQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
           
        }
        public async Task<List<ClubEventWithClassFlagResponseDto>> Handle(ClubEventWithClassFlagQuery request, CancellationToken cancellationToken)
        {
            var valuesSql = string.Join(",", request.ClubIds.Select(c =>$"({c.DocId}, '{c.SyncGuid}')"));

            string sql = $@"WITH ClubList AS (
                            SELECT * FROM (VALUES 
                                {valuesSql}
                            ) AS V(OwningEntityId, OwningEntitySyncGuid)
                        )
                        SELECT 
                            cl.OwningEntityId As DocId,
                            cl.OwningEntitySyncGuid As SyncGuid,
                            CASE 
                                WHEN EXISTS (
                                    SELECT 1 
                                    FROM Events_Default e 
                                    WHERE e.OwningEntityId = cl.OwningEntityId
                                ) THEN 1 ELSE 0 
                            END AS IsExistEvent,
                            CASE 
                                WHEN EXISTS (
                                    SELECT 1 
                                    FROM JustGoBookingClass c 
                                    WHERE c.OwningEntitySyncGuid = cl.OwningEntitySyncGuid
                                ) THEN 1 ELSE 0 
                            END AS IsExistClass
                        FROM 
                            ClubList cl;";


            
            var result = await _readRepository.Value.GetListAsync(sql, null, null, "text");

            return JsonConvert.DeserializeObject<List<ClubEventWithClassFlagResponseDto>>(JsonConvert.SerializeObject(result));
        }


    }
}
