using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetAssetAudits
{
    public class GetAssetAuditsQuery : PaginationParams, IRequest<PagedResult<AssetAuditItemDTO>>
    {
        public EntityType EntityType { get; set; }
        public string EntityId { get; set; }

    }
}
