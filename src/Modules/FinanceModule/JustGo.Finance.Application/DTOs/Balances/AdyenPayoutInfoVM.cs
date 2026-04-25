using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs.Balances
{
    public class AdyenPayoutInfoVM : PaginatedDTO
    {
        public List<AdyenPayoutInfoDTO>? AdyenPayoutInfos { get; set; }
    }
}
