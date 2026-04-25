using System.Collections.Frozen;

namespace JustGo.Authentication.Helper.Enums
{
    public static class TenantIdentifier
    {
        public const string CLAIM_NAME = "TenantClientId";
        
        // public const string EQUESTRIAN_AUSTRALIA = "DTQ-05";
        // public const string EQUESTRIAN_AUSTRALIA_LIV = "LIV-DTQ-005";
        // public const string EQUESTRIAN_AUSTRALIA_SBX = "SBX-DTQ-005";
        // public const string USA_TT_SBX = "SBX-DTQ-010";
        // public const string USA_TT_LIV = "LIV-DTQ-010";
        // public const string SANDBOX = "DTQ-01";
        // public const string TEST_USA_TT_SBX = "DEV-DTQ-017";
    }
#if NET9_0_OR_GREATER
    // public static class TenantSportMapping
    // {
    //     public static readonly FrozenDictionary<string, SportType> Mapping = new Dictionary<string, SportType>
    //     {
    //         { TenantIdentifier.EQUESTRIAN_AUSTRALIA, SportType.HorseRace },
    //         { TenantIdentifier.EQUESTRIAN_AUSTRALIA_LIV, SportType.HorseRace },
    //         { TenantIdentifier.EQUESTRIAN_AUSTRALIA_SBX, SportType.HorseRace },
    //         { TenantIdentifier.USA_TT_LIV, SportType.TableTennis },
    //         { TenantIdentifier.USA_TT_SBX, SportType.TableTennis },
    //         { TenantIdentifier.SANDBOX, SportType.HorseRace }, //ToDo: Change this to appropriate sport
    //         { TenantIdentifier.TEST_USA_TT_SBX, SportType.TableTennis }
    //     }.ToFrozenDictionary();
    // }
    
    public enum SportType : uint
    {
        Equestrian = 1,
        TableTennis = 2,
        Gymnastics = 3
    }
#endif
}