using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveUserPreference
{
    public class SaveUserPreferenceCommand : IRequest<string>
    {
        public required string MemberDocId { get; set; }
        public int OrganizationId { get; set; }
        public int PreferenceTypeId { get; set; }
        public string? PreferenceValue { get; set; }
    }
}
