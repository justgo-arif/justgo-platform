using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventBookingList 
{
    public class GetEventOccuranceBookingCountQuery : IRequest<IList<IDictionary<string, object>>>
    {
        [Required]
        public int RowId { get; set; }
        [Required]
        public string OccuranceDate { get; set; }
    }
}
