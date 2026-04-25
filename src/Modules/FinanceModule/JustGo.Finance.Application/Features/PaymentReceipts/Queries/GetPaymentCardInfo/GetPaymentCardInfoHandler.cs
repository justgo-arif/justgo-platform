using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentCardInfo
{


    public class GetPaymentCardInfoHandler : IRequestHandler<GetPaymentCardInfoQuery, PaymentCardInfo>
    {
        private readonly LazyService<IReadRepository<PaymentCardInfo>> _readRepository;
        private readonly IMediator _mediator;

        public GetPaymentCardInfoHandler(LazyService<IReadRepository<PaymentCardInfo>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentCardInfo> Handle(GetPaymentCardInfoQuery request, CancellationToken cancellationToken)
        {
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);

            var sql = @"SELECT  
                            CONCAT( u.FirstName,' ',u.LastName) AS CustomerName,                           
                            prd.PaymentMethod as PaymentWith,
                            FORMAT(prd.Date,'dd MMM yyyy, hh:mm tt') as LastUpdateDate
                            FROM  PaymentReceipts_Default prd 
                            INNER JOIN [User] u ON u.Userid = prd.Paymentuserid
                            Where prd.docid=@DocId";
            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            if (result is null)
                throw new InvalidOperationException("Payment card info not found for the provided Document Id.");
            return result;
        }
    }

}
