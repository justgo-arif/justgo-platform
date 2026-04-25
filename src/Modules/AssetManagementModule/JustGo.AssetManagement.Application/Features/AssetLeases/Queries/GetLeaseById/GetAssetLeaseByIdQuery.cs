using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AssetLeases;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseById
{
    public class GetAssetLeaseByIdQuery : IRequest<AssetLeaseDetailDTO>
    {
        public string AssetLeaseId { get; set; }
    }
}
