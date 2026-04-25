using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetBillingAddress
{
    public class GetBillingAddressHandler : IRequestHandler<GetBillingAddressQuery, Address>
    {
        private readonly LazyService<IReadRepository<Address>> _readRepository;
        private readonly IMediator _mediator;

        public GetBillingAddressHandler(LazyService<IReadRepository<Address>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<Address> Handle(GetBillingAddressQuery request, CancellationToken cancellationToken)
        {
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);

            var billingDetailsSQL = @"SELECT   
                                        CONCAT(u.FirstName,' ',u.LastName) AS CustomerName, 
                                        CONCAT(u.Address1,' ',u.Address2) AS CustomerAddress, 
                                        ISNULL(up.Number,'') AS PhoneNumber,
                                        u.EmailAddress,
                                        (case 
                                        when u.ProfilePicURL='' or u.ProfilePicURL is null 
                                        then ''
                                        else 'Store/download?f='+u.ProfilePicURL +'&t=user&p=' +Convert(varchar(10),u.Userid) end) 
                                        as ProfilePicURL
                                    FROM PaymentReceipts_Default prd  
                                    INNER JOIN [User] u ON u.Userid = prd.Paymentuserid
                                    LEFT JOIN UserPhonenumber up 
                                           ON up.UserId = u.Userid 
                                           AND up.Type = 'Mobile'
                                        where prd.DocId =@DocId";
            var address = await _readRepository.Value.GetAsync(billingDetailsSQL, cancellationToken, queryParameters, null, "text");
            if (address is null)
                throw new InvalidOperationException("Billing address not found for the provided Document Id.");
            return address;
        }
    }
}
