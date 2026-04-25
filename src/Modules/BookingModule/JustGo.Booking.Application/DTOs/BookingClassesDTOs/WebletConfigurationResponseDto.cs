using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs
{
    public sealed class WebletSqlPartsForClass
    {
        public string ConditionWebletSql { get; init; } = string.Empty;
        public string JoinWebletSql { get; init; } = string.Empty;
    }
    public sealed class WebletConfigRaw
    {
        public string FullWebletObject { get; set; } = string.Empty;
    }

    public sealed class WebletConfigurationResponse
    {
        public WebletConfig Config { get; set; } = new();
        public string DefaultLanding { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string RenderingDiv { get; set; } = string.Empty;
        public Guid WebletId { get; set; }
        public string WebletName { get; set; } = string.Empty;
    }

    public sealed class WebletConfig
    {
        public int DefaultProvider { get; set; }
        public string DefaultProviderCaption { get; set; } = string.Empty;
        public Guid DefaultProviderGuid { get; set; }
        public string DefaultProviderTitIe { get; set; } = string.Empty;
        public FilterConfig Filter { get; set; } = new();
        public bool HideHeroImage { get; set; }
        public bool HideSocialMediaBar { get; set; }
        public bool HideClubLogo { get; set; }
        public bool HideWaitlist { get; set; }
    }

    public sealed class FilterConfig
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? AgeGroups { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? Categories { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? ClassGroups { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? Subcategories { get; set; }

        public bool HideWeekdays { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? AvailableSortBy { get; set; }

        public string DefaultSortBy { get; set; } = string.Empty;

        public string DefaultSortDirection { get; set; } = string.Empty;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? ClassDuration { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? ColorGroups { get; set; }

        public bool HideGender { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int>? PaymentTypes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? TimeOfDay { get; set; }
    }
}
