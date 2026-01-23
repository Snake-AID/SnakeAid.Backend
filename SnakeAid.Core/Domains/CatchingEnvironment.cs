namespace SnakeAid.Core.Domains
{
    public class CatchingEnvironment : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Price { get; set; }
    }
}