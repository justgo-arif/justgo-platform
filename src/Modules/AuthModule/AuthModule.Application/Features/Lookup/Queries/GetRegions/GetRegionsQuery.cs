using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetRegions;

public class GetRegionsQuery : IRequest<List<SelectListItemDTO<string>>> { }
