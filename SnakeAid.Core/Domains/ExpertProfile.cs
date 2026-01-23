namespace SnakeAid.Core.Domains
{
    public class ExpertProfile : Account
    {
        public string Biography { get; set; }
        public bool IsOnline { get; set; }
        public decimal ConsultationFee { get; set; }
        public float Rating { get; set; }
        public int RatingCount { get; set; }

        public List<Specialization> Specializations { get; set; } = new List<Specialization>();
    }
}