using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.AddressPickers.Queries.GetAddressesByPostCode;

public class GetAddressesByPostCodeQuery : IRequest<List<AddressDto>>
{
    public required string Mode { get; set; }
    public required string Keyword { get; set; }
    public required string CountryName { get; set; }
}
