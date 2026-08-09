using System.Collections.Generic;
using NimeVault.Models;

namespace NimeVault.Services.Mock
{
    public static class MockData
    {
        public static List<Anime> Animes { get; } = new()
        {
            new Anime
            {
                Id = "1",
                Title = "Attack on Titan",
                AlternativeTitle = "Shingeki no Kyojin",
                Description = "In a world where humanity lives within enormous walled cities to protect themselves from Titans, gigantic humanoid creatures, a young boy named Eren Yeager vows to exterminate every Titan after they destroy his hometown and kill his mother.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/10/47347l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/668/thumb-1920-668056.jpg",
                Genres = new() { "Action", "Drama", "Fantasy", "Military" },
                Year = 2013,
                Status = "Completed",
                Rating = 9.0,
                TotalEpisodes = 87,
                HasSub = true,
                HasDub = true,
                IsFeatured = true,
                IsPopular = true
            },
            new Anime
            {
                Id = "2",
                Title = "Demon Slayer",
                AlternativeTitle = "Kimetsu no Yaiba",
                Description = "A boy raised by boars, who wears a boar's head, joins the Demon Slayer Corps alongside his comrades, fighting against powerful demonic forces threatening humanity.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/1286/99889l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/108/thumb-1920-1080578.jpg",
                Genres = new() { "Action", "Adventure", "Supernatural" },
                Year = 2019,
                Status = "Ongoing",
                Rating = 8.7,
                TotalEpisodes = 55,
                HasSub = true,
                HasDub = true,
                IsPopular = true
            },
            new Anime
            {
                Id = "3",
                Title = "Jujutsu Kaisen",
                AlternativeTitle = "Sorcery Fight",
                Description = "A boy swallows a cursed talisman — the finger of a demon — and becomes host to a powerful creature. Now the young student must join a secret organization of Jujutsu Sorcerers to find the remaining body parts of the demon and die fighting.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/1171/109222l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/114/thumb-1920-1147147.jpg",
                Genres = new() { "Action", "Fantasy", "School", "Supernatural" },
                Year = 2020,
                Status = "Ongoing",
                Rating = 8.6,
                TotalEpisodes = 48,
                HasSub = true,
                HasDub = true,
                IsPopular = true,
                IsRecentlyAdded = true
            },
            new Anime
            {
                Id = "4",
                Title = "One Piece",
                AlternativeTitle = "OP",
                Description = "Follows the adventures of Monkey D. Luffy and his pirate crew in order to find the greatest treasure ever left by the legendary Pirate, Gold Roger. The famous mystery treasure named 'One Piece'.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/6/73245l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/878/thumb-1920-878958.jpg",
                Genres = new() { "Action", "Adventure", "Comedy", "Fantasy" },
                Year = 1999,
                Status = "Ongoing",
                Rating = 8.8,
                TotalEpisodes = 1100,
                HasSub = true,
                HasDub = true,
                IsPopular = true
            },
            new Anime
            {
                Id = "5",
                Title = "Fullmetal Alchemist: Brotherhood",
                AlternativeTitle = "Hagane no Renkinjutsushi",
                Description = "Two brothers search for a Philosopher's Stone after an attempt to revive their deceased mother goes wrong, leaving them in damaged physical forms.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/1223/96541l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/562/thumb-1920-562665.jpg",
                Genres = new() { "Action", "Adventure", "Drama", "Fantasy", "Military" },
                Year = 2009,
                Status = "Completed",
                Rating = 9.1,
                TotalEpisodes = 64,
                HasSub = true,
                HasDub = true,
                IsPopular = true
            },
            new Anime
            {
                Id = "6",
                Title = "Vinland Saga",
                AlternativeTitle = "Vinland Saga",
                Description = "Thorfinn pursues a journey with his father's killer in order to take revenge and end his life in a duel as an honorable warrior and pay his father a homage.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/1714/119421l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/100/thumb-1920-1003768.jpg",
                Genres = new() { "Action", "Adventure", "Drama", "Historical" },
                Year = 2019,
                Status = "Ongoing",
                Rating = 8.8,
                TotalEpisodes = 48,
                HasSub = true,
                HasDub = false,
                IsRecentlyAdded = true
            },
            new Anime
            {
                Id = "7",
                Title = "Chainsaw Man",
                AlternativeTitle = "Chainsaw Man",
                Description = "Denji is a teenage boy living with a Chainsaw Devil named Pochita. Due to the debt his father left behind, he has been living a rock-bottom life while repaying his debt by harvesting devil corpses with Pochita.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/1806/126216l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/127/thumb-1920-1272098.jpg",
                Genres = new() { "Action", "Adventure", "Supernatural" },
                Year = 2022,
                Status = "Ongoing",
                Rating = 8.5,
                TotalEpisodes = 12,
                HasSub = true,
                HasDub = true,
                IsRecentlyAdded = true
            },
            new Anime
            {
                Id = "8",
                Title = "Spy x Family",
                AlternativeTitle = "Spy x Family",
                Description = "A spy on an undercover mission gets married and adopts a child as part of his cover. His wife and daughter have secrets of their own, and all three must work together.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/1441/122795l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/128/thumb-1920-1281062.jpg",
                Genres = new() { "Action", "Comedy", "Slice of Life" },
                Year = 2022,
                Status = "Ongoing",
                Rating = 8.4,
                TotalEpisodes = 37,
                HasSub = true,
                HasDub = true,
                IsRecentlyAdded = true
            },
            new Anime
            {
                Id = "9",
                Title = "Bleach",
                AlternativeTitle = "Bleach: Thousand-Year Blood War",
                Description = "High school student Ichigo Kurosaki, who has the ability to see ghosts, gains the powers of a Soul Reaper—a death personification similar to the Grim Reaper—from another Soul Reaper, Rukia Kuchiki.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/3/40451l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/868/thumb-1920-868185.jpg",
                Genres = new() { "Action", "Adventure", "Supernatural" },
                Year = 2004,
                Status = "Ongoing",
                Rating = 8.2,
                TotalEpisodes = 393,
                HasSub = true,
                HasDub = true,
                IsPopular = true
            },
            new Anime
            {
                Id = "10",
                Title = "Mushoku Tensei",
                AlternativeTitle = "Mushoku Tensei: Jobless Reincarnation",
                Description = "A 34-year-old underachiever gets run over by a bus, but wakes up in a new world as Rudeus Greyrat, beginning a new life as a mage.",
                PosterUrl = "https://cdn.myanimelist.net/images/anime/1530/117776l.jpg",
                BackgroundUrl = "https://images.alphacoders.com/119/thumb-1920-1195940.jpg",
                Genres = new() { "Drama", "Ecchi", "Fantasy" },
                Year = 2021,
                Status = "Ongoing",
                Rating = 8.3,
                TotalEpisodes = 35,
                HasSub = true,
                HasDub = false,
                IsRecentlyAdded = true
            }
        };

        public static List<Episode> GetEpisodes(string animeId, int count = 12)
        {
            var episodes = new List<Episode>();
            var titles = new[]
            {
                "The Beginning", "First Steps", "A New World", "The Power Within",
                "Rising Tide", "Awakening", "The Choice", "Broken Bonds",
                "Path of Thorns", "Into the Abyss", "The Truth Revealed", "Final Stand",
                "New Horizons", "Dark Waters", "The Last Hope", "Shadows Fall",
                "Dawn of Battle", "Edge of Despair", "Turning Point", "Unbreakable Will"
            };

            for (int i = 1; i <= count; i++)
            {
                episodes.Add(new Episode
                {
                    Id = $"{animeId}-ep-{i}",
                    AnimeId = animeId,
                    Number = i,
                    Title = titles[(i - 1) % titles.Length],
                    Duration = "23 min",
                    HasSub = true,
                    HasDub = i <= count / 2,
                    AirDate = $"2024-0{(i % 9) + 1}-{(i % 28) + 1:D2}"
                });
            }

            return episodes;
        }
    }
}
