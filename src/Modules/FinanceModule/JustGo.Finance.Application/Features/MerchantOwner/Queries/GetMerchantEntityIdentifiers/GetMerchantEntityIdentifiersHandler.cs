using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantEntityIdentifiers
{
    public class GetMerchantEntityIdentifiersHandler : IRequestHandler<GetMerchantEntityIdentifiersQuery, MerchantEntityIdentifiersDto>
    {
        private readonly LazyService<IReadRepository<MerchantEntityIdentifiersDto>> _readRepository;

        public GetMerchantEntityIdentifiersHandler(LazyService<IReadRepository<MerchantEntityIdentifiersDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<MerchantEntityIdentifiersDto> Handle(GetMerchantEntityIdentifiersQuery request, CancellationToken cancellationToken)
        {
            var result = await _readRepository.Value.GetAsync(
                    @$"SELECT  
                        dm.DocId AS MerchantId,
                        CAST(dm.SyncGuid AS UNIQUEIDENTIFIER) AS MerchantGuid,

                        CASE 
                            WHEN mpd.MerchantType = 'NGB' 
                            THEN dm.DocId 
                            ELSE mpl.EntityId 
                        END AS EntityId,

                       CAST(
                            CASE 
                                WHEN mpd.MerchantType = 'NGB' 
                                THEN dm.SyncGuid 
                                ELSE d.SyncGuid 
                            END
                        AS UNIQUEIDENTIFIER) as EntityGuid

                    FROM Document dm

                    INNER JOIN MerchantProfile_Default mpd 
                        ON dm.DocId = mpd.DocId 

                    LEFT JOIN MerchantProfile_Links mpl 
                        ON mpd.DocId = mpl.DocId 

                    LEFT JOIN Document d 
                        ON mpl.EntityId = d.DocId 
                        AND d.RepositoryId = 2 

                    WHERE ( d.[SyncGuid] = @MerchantSyncGuid OR   dm.[SyncGuid] = @MerchantSyncGuid)
                    ",
                    cancellationToken,
                    new { MerchantSyncGuid = request.MerchantSyncGuid },
                    null,
                    "text"
            );
            if (result is null)
            {
                throw new InvalidOperationException("Merchant Id is not found");
            }
            return result;
        }
    }
}
