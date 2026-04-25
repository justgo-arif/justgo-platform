using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using System.ComponentModel;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetUserBookingClasses;

public class GetUserBookingClassesQuery : IRequest<KeysetPagedResult<MemberClassDto>>
{
    [DefaultValue("E6938574-FE6B-4480-A235-93B8A5266CD4")]
    public Guid UserGuid { get; set; }
    [DefaultValue(false)]
    public bool IsPast { get; set; } = false;

    [DefaultValue(50)]
    public int NumberOfRow { get; set; }

    public int? LastSeenId { get; set; }

    public int? TotalRows { get; set; }

    //[DefaultValue("Day")]
    //public required string SortBy { get; set; }

    //[DefaultValue("ASC")]
    //public required string OrderBy { get; set; }
}
