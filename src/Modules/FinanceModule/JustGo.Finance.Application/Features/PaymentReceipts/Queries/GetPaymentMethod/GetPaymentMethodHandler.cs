using Dapper;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentMethod
{

    public class GetPaymentMethodHandler : IRequestHandler<GetPaymentMethodQuery, PaymentMethod?>
    {
        private readonly IMediator _mediator;

        public GetPaymentMethodHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<PaymentMethod?> Handle(GetPaymentMethodQuery request, CancellationToken cancellationToken)
        {
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);

            return new PaymentMethod
            {
                Id = "pm_1JH9nA2eZvKYlo2C7aXf9abc",
                CardNumber = "4242 4242 4242 4242",
                Expires = "12/2026",
                CardType = "Visa",
                Fingerprint = "F1A2B3C4D5E6F7G8",
                CardOwner = "John Doe",
                Address = "123 Main Street, New York, NY",
                Origin = "web",
                ZipCheck = "passed",
                SetupForFutureUse = "on_session"
            };

        }
    }

}
