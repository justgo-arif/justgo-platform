using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetSessionTicketList
{
    class PaymentTypeListQueryHandler : IRequestHandler<PaymentTypeListQuery, IEnumerable<object>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public PaymentTypeListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<IEnumerable<object>> Handle(PaymentTypeListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT 
                sp.ProductType,
                MAX(pd.[Name]) AS ProductName,
                CASE sp.ProductType
                    WHEN 1 THEN 'One-off'
                    WHEN 2 THEN 'Trial'
                    WHEN 3 THEN 'Payg'
                    ELSE 'Subscription'
                END AS TypeName
            FROM JustGoBookingClassSessionProduct AS sp
            LEFT JOIN Products_Default AS pd 
                   ON sp.ProductId = pd.DocId

            inner join JustGoBookingClassSession cs on sp.SessionId=cs.SessionId
            inner join JustGoBookingClass cl on cs.ClassId=cl.ClassId
            WHERE cl.IsDeleted <> 1 AND cl.OwningEntityId=@ClubDocId
            GROUP BY sp.ProductType
            ORDER BY sp.ProductType;";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId",request.ClubDocId);

            return await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
        }
    }
}
