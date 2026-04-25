namespace JustGo.Organisation.Application.DTOs
{
    public class MyClubDto
    {
        public int MemberDocId { get; set; }
        public int ClubDocId { get; set; }
        public string Image { get; set; }

        public string ClubAddressLine1 { get; set; }
        public string ClubAddressLine2 { get; set; }
        public string ClubAddressLine3 { get; set; }
        public string ClubTown { get; set; }
        public string ClubPostcode { get; set; }

        public string ClubPhoneNumber { get; set; }
        public string ClubWebsite { get; set; }
        public string ClubEmailAddress { get; set; }

        public string ClubName { get; set; }
        public string ClubID { get; set; }
        public DateTime? NextExpiryDate { get; set; }

        public string LocalAuthority { get; set; }
        public string ClubCountry { get; set; }
        public string Region { get; set; }
        public string ClubType { get; set; }

        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string Linkedin { get; set; }
        public string Googleplus { get; set; }
        public string LatLng { get; set; }
        public string Pinterest { get; set; }

        public int CurrentStateId { get; set; }
        public string State { get; set; }

        public int ClubMemberDocId { get; set; }
        public bool IsPrimary { get; set; }
        public string MyRoles { get; set; }
        public string ClubMembershipCategory { get; set; }
        public DateTime? ClubMembershipExpiry { get; set; }

        public bool IsTransfer { get; set; }
        public int LicenseCount { get; set; }
        public string ClubState { get; set; }

        public string ClubMemberSyncGuid { get; set; }
        public string ClubSyncGuid { get; set; }

        public string RegionClubId { get; set; }
        public int? RegionClubDocId { get; set; }
        public string RegionClubType { get; set; }
        public string RegionClubName { get; set; }

        public string SubRegionClubId { get; set; }
        public int? SubRegionClubDocId { get; set; }
        public string SubRegionClubType { get; set; }
        public string SubRegionClubName { get; set; }

    }

}
