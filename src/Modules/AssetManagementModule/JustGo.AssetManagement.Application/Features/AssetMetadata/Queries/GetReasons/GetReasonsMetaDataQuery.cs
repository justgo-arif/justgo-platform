using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetReasonsMetaData
{
    public class GetReasonsMetaDataQuery : IRequest<List<SelectListItemDTO<string>>>
    {
    }
}
