using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AssetTransfers;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetTransfers.Queries.GetTransferById
{
    public class GetAssetTransferByIdQuery : IRequest<AssetTransferDetailDTO>
    {
        public string AssetTransferId { get; set; }
    }
}
