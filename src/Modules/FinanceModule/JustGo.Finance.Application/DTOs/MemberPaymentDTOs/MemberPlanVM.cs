using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class MemberPlanVM
    {
        public string CategoryName { get; set; }
        public List<MemberPayment> Payments { get; set; }
    }
}
