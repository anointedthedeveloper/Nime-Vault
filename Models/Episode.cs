namespace NimeVault.Models
{
    public class Episode
    {
        public string Id { get; set; } = string.Empty;
        public string AnimeId { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public bool HasSub { get; set; }
        public bool HasDub { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string AirDate { get; set; } = string.Empty;
    }
}
