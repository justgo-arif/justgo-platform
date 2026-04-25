using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Balances;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAdyenPayouts
{
    public class GetAdyenPayoutsQuery : PaginationDateRangeFilter, IRequest<AdyenPayoutInfoVM>
    {
        public Guid MerchantId { get; set; }
        public string? Status { get; set; }
        public string? OrderBy { get; set; }
        public string? SortDirection { get; set; }
    }
}
