using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlansDetails
{
    public class GetInstallmentPlansDetailsQueryHandler : IRequestHandler<GetInstallmentPlansDetailsQuery, InstallmentResponse>
    {
        private readonly LazyService<IReadRepository<RecurringPaymentScheduleDto>> _readRepository;
        private readonly IMediator _mediator;

        public GetInstallmentPlansDetailsQueryHandler(LazyService<IReadRepository<RecurringPaymentScheduleDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<InstallmentResponse> Handle(GetInstallmentPlansDetailsQuery request, CancellationToken cancellationToken)
        {
            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var queryParameters = new DynamicParameters();

            queryParameters.Add("RecurringType", RecurringType.Installment);
            queryParameters.Add("OwnerId", ownerId);
            queryParameters.Add("PlanId", request.PlanId);

            var sql = @"GetInstallmentSubscriptionsPlansDetails";
            await using var productsresult = await _readRepository.Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters);
            var installmentResponse = (await productsresult.ReadAsync<InstallmentResponse>()).FirstOrDefault() ?? new InstallmentResponse();
            var paymentMethod = (await productsresult.ReadAsync<PaymentMethod>()).FirstOrDefault();
            var billingAddress = (await productsresult.ReadAsync<Address>()).FirstOrDefault();
            installmentResponse.PaymentMethod = paymentMethod ?? new PaymentMethod
            {
                Expires = string.Empty,
                CardType = string.Empty,
                LastFourDigits = string.Empty,
                Origin = string.Empty,
                Fingerprint = string.Empty
            };
            installmentResponse.BillingDetails = billingAddress ?? new Address
            {
                CustomerName = string.Empty,
                CustomerAddress = string.Empty,
                PhoneNumber = string.Empty,
                EmailAddress = string.Empty
            };
            return installmentResponse;
        }
    }
}
