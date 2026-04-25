using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Application.Features.FieldManagement.Queries.FieldManagementQueryModel
{
    public class FieldManagenetBookingDetailsQuery
    {
        public required int SessionId { get; set; }
        public required int MemberDocId { get; set; }   
    }
}
