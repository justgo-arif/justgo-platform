namespace JustGo.Result.Application.Common.Enums;

public static class ResultUploadFields
{
    public static string MemberId => "Member ID";
    private static string FirstName => "First Name";
    private static string LastName => "Last Name";
    private static string HorseId => "Horse ID";
    private static string HorseName => "Horse Name";

    private static string OwnerFirstName => "Owner First Name";
    private static string OwnerLastName => "Owner Last Name";
    private static string OwnerNo => "Owner No";

    public static HashSet<string> ValidatableFields => new(StringComparer.OrdinalIgnoreCase)
    {
        MemberId,
        FirstName,
        LastName,
        HorseId,
        HorseName,
        OwnerFirstName,
        OwnerLastName,
        OwnerNo
    };

}