using System.Collections.Generic;

namespace NimeVault.Models
{
    public class Anime
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AlternativeTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public string BackgroundUrl { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new();
        public int Year { get; set; }
        public string Status { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int TotalEpisodes { get; set; }
        public bool HasSub { get; set; }
        public bool HasDub { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPopular { get; set; }
        public bool IsRecentlyAdded { get; set; }
    }
}
