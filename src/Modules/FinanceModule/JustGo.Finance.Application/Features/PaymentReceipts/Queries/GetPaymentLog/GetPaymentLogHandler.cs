using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentLog
{

    public class GetPaymentLogHandler : IRequestHandler<GetPaymentLogQuery, List<PaymentLog>>
    {
        private readonly LazyService<IReadRepository<PaymentLog>> _readRepository;
        private readonly IMediator _mediator;

        public GetPaymentLogHandler(LazyService<IReadRepository<PaymentLog>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<PaymentLog>> Handle(GetPaymentLogQuery request, CancellationToken cancellationToken)
        {

            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);
            var sql = @"select DocId,RowId,Name,'' as Logtype,Description,Date,Time  
                        from PaymentReceipts_Log 
                        Where DocId=@DocId
                        AND Description NOT LIKE '%{%'";
            return (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
        }
    }

}
