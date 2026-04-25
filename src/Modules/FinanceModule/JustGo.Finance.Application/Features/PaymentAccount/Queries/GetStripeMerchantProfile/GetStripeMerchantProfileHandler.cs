using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.PaymentAccount;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Queries.GetStripeMerchantProfile;

public class GetStripeMerchantProfileHandler : IRequestHandler<GetStripeMerchantProfileQuery, StripeMerchantProfileDTO?>
{
    private readonly LazyService<IReadRepository<StripeMerchantProfileDTO>> _readRepository;

    public GetStripeMerchantProfileHandler(LazyService<IReadRepository<StripeMerchantProfileDTO>> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<StripeMerchantProfileDTO?> Handle(GetStripeMerchantProfileQuery request, CancellationToken cancellationToken)
    {
        var query = @"
        SELECT 
            md.DocId AS Id,
            md.Name,
            md.[Description],
            md.Created,
            md.Updated,
            md.Isactive,
            md.Marchentprofiletag AS Tag
        FROM dbo.MerchantProfile_Default md
        WHERE md.DocId = @MarchentID
    ";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("MarchentID", request.MerchantId);

        var data = await _readRepository.Value
            .GetAsync(query, cancellationToken, queryParameters, null, "text");

        if(data is null)
        {
            return null; // No merchant profile found
        }

        return (StripeMerchantProfileDTO)data;
    }
}
