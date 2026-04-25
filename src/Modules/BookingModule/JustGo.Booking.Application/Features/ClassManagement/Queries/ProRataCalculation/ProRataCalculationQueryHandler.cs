using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.DTOs.ClassManagementDtos;
using Newtonsoft.Json;
using System.Data;

namespace JustGo.Booking.Application.Features.ClassManagement.Queries.ProRataCalculation
{

    public class ProRataCalculationHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<ProRataCalculationQuery, Result<object>>
    {
        private readonly IReadRepositoryFactory _readRepository = readRepository;
        public async Task<Result<object>> Handle(
            ProRataCalculationQuery request,
            CancellationToken cancellationToken)
        {
            var repo = _readRepository.GetLazyRepository<object>().Value;
            var discount = await CalculateProRataDiscountByClassProductAndDate(request.Request.ClassProductId,request.Request.StartDate, repo, cancellationToken);
            var result = new { ProRataDiscount = discount };
            return result;

        }

        public async Task<object> CalculateProRataDiscountByClassProductAndDate(int classProductId, DateTime date,
            IReadRepository<object> repo, CancellationToken cancellationToken)
        {
            string query = @"CalculateProRataDiscountByClassProduct";
            var parameters = new DynamicParameters();
            parameters.Add("@ClassProductDocId", classProductId, DbType.Int32);
            parameters.Add("@StartDate", date, DbType.DateTime);
            var discount =
                (await repo.GetSingleAsync<decimal>(query, parameters, null,cancellationToken, QueryType.StoredProcedure));
            return discount;
        }


    }
}
