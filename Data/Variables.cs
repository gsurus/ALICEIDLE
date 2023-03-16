using Colorful;
using Discord;
using Discord.WebSocket;
using sysColor = System.Drawing.Color;
namespace ALICEIDLE
{
    public class Variables
    {
        public static CmdType GetCommandType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException(nameof(type));
            }
            switch (type.ToLowerInvariant())
            {
                case "catching":
                    return CmdType.Catching;

                case "favorite":
                    return CmdType.Favorite;

                case "histfavorite":
                    return CmdType.HistFavorite;

                case "home":
                    return CmdType.Home;

                case "leaderboard":
                    return CmdType.Leaderboard;

                case "favorites":
                    return CmdType.Favorites;

                case "next":
                    return CmdType.Next;

                case "previous":
                    return CmdType.Previous;

                case "history":
                    return CmdType.History;

                case "histnext":
                    return CmdType.HistNext;

                case "histprevious":
                    return CmdType.HistPrevious;

                case "remove":
                    return CmdType.Remove;

                case "upgrade":
                    return CmdType.Upgrade;

                case "landing":
                    return CmdType.Landing;

                case "gelbooru":
                    return CmdType.Gelbooru;

                case "gelNext":
                    return CmdType.GelNext;

                case "gelPrevious":
                    return CmdType.GelPrevious;

                default:
                    throw new ArgumentException($"Invalid command type: {type}", nameof(type));
            }
        }

        public enum CmdType
        {
            Catching,
            Favorite,
            HistFavorite,
            Home,
            Leaderboard,
            Favorites,
            Next,
            Previous,
            History,
            HistNext,
            HistPrevious,
            Remove,
            Upgrade,
            Landing,
            Gelbooru,
            GelNext,
            GelPrevious
        }
    }
    
    public class EmbedColors
    {
        public static Color ssrColor = new Color(255, 66, 126);
        public static Color srColor = new Color(233, 66, 255);
        public static Color rColor = new Color(69, 66, 255);
        public static Color nColor = new Color(158, 221, 255);
        public static Color successColor = new Color(92, 184, 92);
        public static Color errorColor = new Color(184, 92, 92);
    }

    public class SqlQueries
    {
        public static string selectFrom = "SELECT * FROM `aliceidle`.`mytable`";
        public static string selectFromWhere = $"SELECT * FROM `aliceidle`.`mytable` WHERE";
        public static string selectDistinctFromWhere = "SELECT * FROM `aliceidle`.`mytable` WHERE";
        public static string selectFromOrderBy = "SELECT * FROM `aliceidle`.`mytable` ORDER BY";
    }

    public class AnilistQuery
    {
        public static string buildListSearchString = @"
                query($perPage: Int, $page: Int) {
                  Page(perPage: $perPage, page: $page) {
                    characters(sort: FAVOURITES_DESC) {
                      id
                      name {
                        full
                        first
                        middle
                        last
                        userPreferred
                        alternative
                        alternativeSpoiler
                        native
                      }
                      gender
                      age
                      dateOfBirth {
                        month
                        day
                        year
                      }
                      image {
                        large
                        medium
                      }
                      media(sort: FAVOURITES_DESC) {
                        nodes {
                          popularity
                          title {
                            userPreferred
                            english
                            romaji
                            native
                          }
                          id
                          isAdult
                          type
                        }
                      }
                      favourites
                    }
                  }
                }
            ";
    }
}
