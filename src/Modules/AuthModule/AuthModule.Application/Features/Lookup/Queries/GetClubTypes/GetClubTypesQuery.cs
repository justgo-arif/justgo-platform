using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetClubTypes;

public class GetClubTypesQuery : IRequest<List<SelectListItemDTO<string>>> { }
