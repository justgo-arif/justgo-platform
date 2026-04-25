using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class PaymentConsoleProduct
    {
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public int CategoryId { get; set; }
    }
}
