using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetLicenseDataCaptureItems
{
    public class GetLicenseDataCaptureItemsHandler : IRequestHandler<GetLicenseDataCaptureItemsQuery, List<LicenseDataCaptureItemDto>>
    {
        private readonly LazyService<IReadRepository<LicenseDataCaptureItemDto>> _readRepository;

        public GetLicenseDataCaptureItemsHandler(LazyService<IReadRepository<LicenseDataCaptureItemDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<LicenseDataCaptureItemDto>> Handle(GetLicenseDataCaptureItemsQuery request, CancellationToken cancellationToken)
        {
            string sql = @"
            SELECT RowId AS Id, [Sequence], [Type], Config 
            FROM [License_Datacaptureitems] ld 
            WHERE ld.DocId = @LicenseDocId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LicenseDocId", request.LicenseDocId);

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            return result;
        }
    }
}
