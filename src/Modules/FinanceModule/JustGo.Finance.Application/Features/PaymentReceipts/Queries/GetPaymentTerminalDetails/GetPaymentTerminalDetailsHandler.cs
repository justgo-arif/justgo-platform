using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentTerminalDetails
{

    class GetPaymentTerminalDetailsHandler : IRequestHandler<GetPaymentTerminalDetailsQuery, PaymentTerminalDetails>
    {
        private readonly LazyService<IReadRepository<PaymentTerminalDetails>> _readRepository;
        private readonly IMediator _mediator;

        public GetPaymentTerminalDetailsHandler(LazyService<IReadRepository<PaymentTerminalDetails>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentTerminalDetails> Handle(GetPaymentTerminalDetailsQuery request, CancellationToken cancellationToken)
        {
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);
            var demoPaymentTerminalDetails = new PaymentTerminalDetails
            {
                StatementDescriptor = "Badminton England",
                TransferDestination = "acct_1JH9kZ2eZvKYlo2C",
                Transfer = "tr_1JH9mD2eZvKYlo2CFzVq7GpD",
                CollectedFeeURL = "tr_1JH9mD2eZvKYlo2CFzVq7GpD",
                CollectedFee = 25.50m,
                TransferGroup = "group_2025_05_event",
                Description = "KHV8isudkoL0AFm8vj62och1LZZy5dd"
            };
            return demoPaymentTerminalDetails;
        }
    }

}
