using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.UpdateUserEmergencyContacts
{
    public class UpdateUserEmergencyContactCommand : IRequest<int>
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Relation { get; set; }
        public string? ContactNumber { get; set; }
        public string? EmailAddress { get; set; }
        public bool? IsPrimary { get; set; }
        public string CountryCode { get; set; }
        public required string SyncGuid { get; set; }
        public bool EmailMandatory { get; set; }
        public bool PhoneMandatory { get; set; }
     }
}
