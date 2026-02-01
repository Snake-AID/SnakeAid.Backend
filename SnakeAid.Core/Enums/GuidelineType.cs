using System.Runtime.Serialization;

namespace SnakeAid.Core.Enums
{
    public enum GuidelineType
    {
        [EnumMember(Value = "GENERAL")]
        General = 0,

        [EnumMember(Value = "SPECIES_SPECIFIC")]
        SpeciesSpecific = 1
    }
}
