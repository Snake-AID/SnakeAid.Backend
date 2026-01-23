namespace SnakeAid.Core.Domains
{
    public class LibraryMedia : BaseEntity
    {
        public Guid Id { get; set; }
        public int SnakeSpeciesId { get; set; }
        public string MediaUrl { get; set; }
        public LibraryMediaType MediaType { get; set; }
    }

    public enum LibraryMediaType
    {
        Image,
        Video,
        Document
    }
}