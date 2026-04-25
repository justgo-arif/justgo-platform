using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.Preferences.Queries.GetUserPreferences
{
    public class GetUserPreferencesQuery(string memberDocId, int organizationId, int preferenceTypeId)
        : IRequest<string>
    {
        public string MemberDocId { get; } = memberDocId;
        public int OrganizationId { get; } = organizationId;
        public int PreferenceTypeId { get; } = preferenceTypeId;
    }
}
