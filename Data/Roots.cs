using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALICEIDLE
{
    public class WaifuRoot
    {
        public List<Waifu> Waifus;
    }
    public class Waifu
    {
        public Name Name { get; set; }
        public string? Gender { get; set; }
        public string? Age { get; set; }
        public DateOfBirth DateOfBirth { get; set; }
        public string? Series { get; set; }
        public Media Media { get; set; }
        public string? ImageURL { get; set; }
        public string Rarity { get; set; }
        public int XpValue { get; set; }
        public int Favorites { get; set; }
        public int Id { get; set; }
        public int? SeriesId { get; set; }
        public bool? IsAdult { get; set; }
    }

    public class PlayerData
    {
        public string Name { get; set; }
        public ulong Id { get; set; }
        public string GenderPreference { get; set; } = "none";
        public int Level { get; set; } = 1;
        public int Xp { get; set; } = 0;
        public double WaifuAmount { get; set; } = 0;
        public int LastCharacterRolled { get; set; }
        public int RollsSinceLastSSR { get; set; } = 0;
        public int CurrentWaifu { get; set; }
        public List<Tuple<int, int>> OwnedWaifus { get; set; }
        public List<int> RollHistory { get; set; }
        public int TotalRolls { get; set; } = 0;
    }
    public class FavoriteData
    {
        public int Id;
        public int Level;
    }
    public class OwnedWaifus
    {
        public List<Waifu> Waifus { get; set; }
    }

    public class WaifuEmbedInfo
    {
        public string ImageURL { get; set; }
        public Color EmbedColor { get; set; }
        public EmbedFieldBuilder PrimaryField { get; set; }
        public EmbedFieldBuilder InfoField { get; set; }
        public EmbedFieldBuilder LinkField { get; set; }

        public int Id { get; set; }
    }

    public class RedditPostData
    {
        public string title { get; set; }
        public string imageURL { get; set; }
    }

    public class Character
    {
        public int Id { get; set; }
        public Name Name { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public DateOfBirth DateOfBirth { get; set; }
        public Image Image { get; set; }
        public Media Media { get; set; }
        public int Favourites { get; set; }
    }

    public class Data
    {
        public Page Page { get; set; }
    }

    public class DateOfBirth
    {
        public int? month { get; set; }
        public int? day { get; set; }
        public int? year { get; set; }
    }

    public class Image
    {
        public string Large { get; set; }
        public string Medium { get; set; }
    }

    public class Media
    {
        public List<Node> nodes { get; set; }
    }

    public class Name
    {
        public string Full { get; set; }
        public string First { get; set; }
        public string Middle { get; set; }
        public string Last { get; set; }
        public string UserPreferred { get; set; }
        public List<string> Alternative { get; set; }
        public List<string> AlternativeSpoiler { get; set; }
        public string Native { get; set; }
    }

    public class Node
    {
        public int? Popularity { get; set; }
        public Title Title { get; set; }
        public int? Id { get; set; }
        public bool? IsAdult { get; set; }
        public string Type { get; set; }
    }

    public class Page
    {
        public List<Character> Characters { get; set; }
    }

    public class Root
    {
        public Data Data { get; set; }
    }

    public class Title
    {
        public string UserPreferred { get; set; }
        public string English { get; set; }
        public string Romaji { get; set; }
        public string Native { get; set; }
    }
}
