using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.LeaseAdditionalFees
{
    public class GetLeaseAdditionalFeeQuery : IRequest<List<AssetSurchargeDTO>>
    {
        public GetLeaseAdditionalFeeQuery(string leaseId,string ownerid)
        {
            LeaseId = leaseId;
            OwnerId = ownerid;
        }
        public string LeaseId { get; set; }
        public string OwnerId { get; set; }
    }
}
