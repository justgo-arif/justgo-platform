using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDiscountList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.ResolveSessionId
{

    public class ResolveSessionIdHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<ResolveSessionIdQuery, string>
    {
        public async Task<string> Handle(ResolveSessionIdQuery request, CancellationToken cancellationToken = default)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SessionId", request.Id);

            const string sql = """
                               select ClassSessionGuid as sessionGuid from justgobookingclasssession where SessionId = @SessionId
                               """;

            var result = await readRepository
                .GetLazyRepository<object>()
                .Value
                .GetSingleAsync<string>(sql, parameters, null, cancellationToken,  "text");


            return result ?? string.Empty;
        }
    }
}
