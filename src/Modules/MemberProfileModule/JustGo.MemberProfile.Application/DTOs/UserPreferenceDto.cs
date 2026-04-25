using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.DTOs
{
    
    public class SaveMultipleUserPreferenceRequestDto
    {
        public required string MemberDocId { get; set; }
        public List<UserPreferenceItem> Preferences { get; set; } = [];
    }
    public class UserPreferenceItem
    {
        public int OrganizationId { get; set; }
        public int PreferenceTypeId { get; set; }
        public string? PreferenceValue { get; set; }
    }
    public class SaveUserPreferenceRequestDto
    {
        public required string MemberDocId { get; set; }
        public int OrganizationId { get; set; }
        public int PreferenceTypeId { get; set; }
        public string? PreferenceValue { get; set; }
    }
    public class GetUserPreferenceRequestDto
    {
        public required string MemberDocId { get; set; }
        public int OrganizationId { get; set; }
        public int PreferenceTypeId { get; set; }
    }
}
