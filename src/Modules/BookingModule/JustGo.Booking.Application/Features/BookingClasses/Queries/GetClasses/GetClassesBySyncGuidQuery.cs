using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.ComponentModel;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetClasses;

public class GetClassesBySyncGuidQuery : IRequest<KeysetPagedResult<BookingClassDto>>
{
    [DefaultValue("b0df64dc-1886-452a-8ca0-7a014813207e")]
    public Guid OwningEntityGuid { get; set; }

    [DefaultValue("7F9B26D0-033D-4D03-A2AB-091D6ED5C1AE")]
    public Guid? CategoryGuid { get; set; }
    public Guid? WebletGuid { get; set; }

    public int? AgeGroupId { get; set; }

    public string[]? Days { get; set; } = [];

    public int[]? AgeGroups { get; set; } = [];

    public int[]? Disciplines { get; set; } = [];

    public int[]? ClassGroups { get; set; } = [];

    public string[]? Genders { get; set; } = [];

    public int[]? ColorGroups { get; set; } = [];

    public string[]? Times { get; set; } = [];

    public int[]? Durations { get; set; } = [];

    public int[]? Payments { get; set; } = [];

    [DefaultValue(40)]
    public int NumberOfRow { get; set; }

    public int? LastSeenId { get; set; }

    public int? TotalRows { get; set; }

    [DefaultValue("Day")]
    public required string SortBy { get; set; }

    [DefaultValue("ASC")]
    public required string OrderBy { get; set; }
}
