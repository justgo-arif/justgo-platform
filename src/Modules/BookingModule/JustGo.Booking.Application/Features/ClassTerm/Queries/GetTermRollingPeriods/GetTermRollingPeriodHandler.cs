using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.Features.ClassTerm.Queries.GetTermTypes;
using JustGo.Booking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.ClassTerm.Queries.GetTermRollingPeriods
{
    public class GetTermRollingPeriodHandler : IRequestHandler<GetTermRollingPeriodQuery, List<TermRollingPeriod>>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetTermRollingPeriodHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<TermRollingPeriod>> Handle(GetTermRollingPeriodQuery request, CancellationToken cancellationToken = default)
        {
            var sql = $"select TermRollingPeriodId,RollingPeriodName as Name from JustGoBookingClassTermRollingPeriod ";
            var result = (await _readRepository
                .GetLazyRepository<TermRollingPeriod>()
                .Value
                .GetListAsync(
                    sql,
                    cancellationToken,
                    null,
                    null,
                    "text"
                )).ToList();

            return result;
        }
    }
}
