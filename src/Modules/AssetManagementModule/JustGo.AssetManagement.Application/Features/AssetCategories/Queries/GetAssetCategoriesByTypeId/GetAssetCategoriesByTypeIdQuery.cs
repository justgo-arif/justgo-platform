using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetCategories.Queries.GetAssetCategories
{
    public class GetAssetCategoriesByTypeIdQuery:IRequest<List<AssetCategoryDTO>>
    {

        public string AssetTypeId { get; set; }
    }
}
