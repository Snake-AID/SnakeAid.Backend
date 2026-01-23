namespace SnakeAid.Core.Domains
{
    public class MemberProfile : Account
    {
        public float Rating { get; set; }
        public int RatingCount { get; set; }
        public List<string> EmergencyContacts { get; set; } = new List<string>();
        public bool HasUnderlyingDisease { get; set; }
    }
}