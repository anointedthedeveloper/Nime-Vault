using System.Collections.Generic;

namespace NimeVault.Models
{
    public class Anime
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
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
        public int SubCount { get; set; }
        public int DubCount { get; set; }
        public string AgeRating { get; set; } = string.Empty;  // PG-13, R, etc.
        public string AnimeType { get; set; } = string.Empty;  // TV, Movie, OVA
        public string Quality { get; set; } = "HD";
        public string Duration { get; set; } = string.Empty;
        public string Aired { get; set; } = string.Empty;
        public string Studio { get; set; } = string.Empty;
        public bool HasSub { get; set; }
        public bool HasDub { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPopular { get; set; }
        public bool IsRecentlyAdded { get; set; }
    }
}
