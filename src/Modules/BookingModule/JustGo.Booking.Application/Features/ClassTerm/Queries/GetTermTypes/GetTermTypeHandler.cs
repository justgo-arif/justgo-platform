using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Domain.Entities;

namespace JustGo.Booking.Application.Features.ClassTerm.Queries.GetTermTypes
{
    public class GetTermTypeHandler : IRequestHandler<GetTermTypeQuery, List<TermType>>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetTermTypeHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<TermType>> Handle(GetTermTypeQuery request, CancellationToken cancellationToken = default)
        {
            var sql = $"select TermTypeId,TermTypeName as Name from JustGoBookingClassTermType;";
            var result = (await _readRepository
                .GetLazyRepository<TermType>()
                .Value
                .GetListAsync(
                    sql,
                    cancellationToken,
                    null,
                    null,
                    "text"
                )).ToList();

            return result ;
        }
    }
}
