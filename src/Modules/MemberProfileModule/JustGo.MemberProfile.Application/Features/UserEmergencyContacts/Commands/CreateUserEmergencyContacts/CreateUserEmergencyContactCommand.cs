using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.CreateUserEmergencyContacts
{
    public class CreateUserEmergencyContactCommand : IRequest<int>
    {
        public Guid UserSyncGuid { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Relation { get; set; }
        public string? ContactNumber { get; set; }
        public string? EmailAddress { get; set; }
        public bool? IsPrimary { get; set; } = false;
        public string? CountryCode { get; set; }
        public bool EmailMandatory { get; set; }
        public bool PhoneMandatory { get; set; }
    }
}
