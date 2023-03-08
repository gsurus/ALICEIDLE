using Discord;
using Discord.WebSocket;
using ALICEIDLE.Logic;
using ALICEIDLE.Gelbooru;
using System.Data;

namespace ALICEIDLE.Services
{ 
    public class EmbedHandler : Values
    {
        public static bool hasIterated { get; set; } = false;
        public static List<EmbedBuilder> waifuPages { get; set; }
        public static List<PlayerData> playerList { get; set; } = new List<PlayerData>();
        public static Waifu waifuToDisplay { get; set; }
        public static Color HomeColor = new Color(23, 178, 255);
        public static UserData userData { get; set; }
        public static OwnedWaifus wList { get; set; }
        private static Dictionary<ulong, PlayerData> playerDictionary = new Dictionary<ulong, PlayerData>();
        

        public static async Task<EmbedBuilder> GelEmbedBuilder(string user, string type)
        {
            EmbedBuilder emb = new EmbedBuilder();

            var _user = userData.Users.Find(d => d.Username == user);

            Gelbooru.Root root = _user.Posts;

            Post post = new Post();
            switch (type)
            {
                case "gelbooru":
                    _user.Page = 0;
                    post = root.post[0];
                    break;
                case "gelNext":
                    _user.Page += 1;
                    post = root.post[_user.Page];
                    break;
                case "gelPrevious":
                    _user.Page -= 1;
                    post = root.post[_user.Page];
                    break;
            }
            string desc = $"**Score**: {post.score}\n**Rating**: {post.rating}\n**Dimensions**: {post.width}x{post.height}";
            if (post.source != null)
                desc += $"\n[Source]({post.source})";


            emb.WithImageUrl(post.file_url).WithTitle("Search Result").WithUrl($"https://gelbooru.com/index.php?page=post&s=view&id={post.id}")
                .AddField("Info", desc)
                .WithFooter($"Page: {_user.Page + 1}/{root.post.Count()}");
            return emb;
        }
        public static async Task<EmbedBuilder> BuildEmbed(string type, string username, ulong uid, string name = "none")
        {
            PlayerData playerData = new PlayerData();
            OwnedWaifus wList = new OwnedWaifus();

            if (playerDictionary.ContainsKey(uid))
                playerData = playerDictionary[uid];
            else
            { // Check if the user exists in the database
                bool playerExists = await SqlDBHandler.PlayerExists(uid);
                if (!playerExists)
                { // Insert player data into the database and add to player list
                    playerData = await SqlDBHandler.InsertPlayerData(username, uid);
                    playerList.Add(playerData);
                }
                else // Retrieve player data from the database
                    playerData = await SqlDBHandler.RetrievePlayerData(uid);

                // Add player data to the dictionary
                playerDictionary.Add(uid, playerData);
            }

            if (playerData == null)
                playerData = await SqlDBHandler.RetrievePlayerData(uid);

            EmbedBuilder emBuilder = new EmbedBuilder()
                .WithTitle($"{username} [lvl {CalculateLevel(playerData.Xp)}]");

            switch (GetCommandType(type))
            {
                case CmdType.Catching:
                    await ProcessCatching(emBuilder, playerData);
                    break;

                case CmdType.Favorite:
                case CmdType.HistFavorite:
                    await ProcessFavorite(emBuilder, playerData, name);
                    break;

                case CmdType.Home:
                    await ProcessHome(emBuilder, playerData);
                    break;

                case CmdType.Leaderboard:
                    await ProcessLeaderboard(emBuilder);
                    break;

                case CmdType.Favorites:
                case CmdType.Next:
                case CmdType.Previous:
                case CmdType.History:
                case CmdType.HistNext:
                case CmdType.HistPrevious:
                case CmdType.Remove:
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(FavoritesToIdList(playerData.OwnedWaifus), true);
                    await ProcessWaifuIterator(emBuilder, playerData, wList, GetCommandType(type));
                    break;
            }
            return emBuilder;
        }
        public static async Task ProcessCatching(EmbedBuilder emBuilder, PlayerData playerData)
        {
            var waifu = await CatchWaifu(playerData);
            if (waifu == null)
            {
                return;
            }

            WaifuEmbedInfo embedInfo = CreateEmbedContent(waifu);
            emBuilder.WithImageUrl(embedInfo.ImageURL)
                     .WithColor(embedInfo.EmbedColor)
                     .AddField(embedInfo.PrimaryField)
                     .AddField(embedInfo.InfoField)
                     .AddField(embedInfo.LinkField)
                     .WithFooter(new EmbedFooterBuilder().WithText($"{waifu.Series}"));
        }

        public static async Task ProcessFavorite(EmbedBuilder emBuilder, PlayerData playerData, string name)
        {
            var _waifu = await Favorite(playerData, name);
            if (_waifu == null)
            {
                emBuilder.AddField("Duplicate", $"{name} is already in your favorites", false)
                         .WithColor(errorColor);
            }
            else
            {
                emBuilder.AddField("Favorited", $"Added {name} to favorites", false)
                         .WithColor(successColor);
            }
        }

        public static async Task ProcessHome(EmbedBuilder emBuilder, PlayerData playerData)
        {
            emBuilder.WithColor(HomeColor)
                     .AddField("Level", CalculateLevel(playerData.Xp).ToString("N0"), true)
                     .AddField("Favorites", playerData.OwnedWaifus.Count().ToString("N0"), true)
                     .AddField("Rolled", playerData.TotalRolls.ToString("N0"), true)
                     .AddField("XP", $"{playerData.Xp.ToString("N0")}/{CalculateXPRequired(playerData.Xp).ToString("N0")}", true);
        }


        public static async Task ProcessLeaderboard(EmbedBuilder emBuilder)
        {
            string leaderboardPlayers = "";
            var players = await RetrieveAllPlayerData();
            int i = 1;
            foreach (var player in players)
            {
                emBuilder.AddField($"{player.Name}", $"Score: {player.Xp.ToString("N0")}\nRolls: {player.TotalRolls.ToString("N0")}");
                i++;
            }
            emBuilder.WithTitle("Leaderboard");
        }

        public static async Task ProcessWaifuIterator(EmbedBuilder emBuilder, PlayerData playerData, OwnedWaifus wList, CmdType commandType)
        {
            wList.Waifus.Reverse();
            emBuilder = await WaifuIterator(wList, playerData, emBuilder, commandType.ToString().ToLowerInvariant());
        }

        static async Task<EmbedBuilder> WaifuIterator(OwnedWaifus waifuList, PlayerData player, EmbedBuilder emb, string arg)
        {
            int waifuIndex = 0;
            int nextWaifu = 0;
            Waifu waifu = await QueryWaifuById(player.CurrentWaifu); 
            waifuIndex = waifuList.Waifus.FindIndex(a => a.Name.Full == waifu.Name.Full);

            switch (GetCommandType(arg))
            {
                case CmdType.Next:
                    nextWaifu = waifuIndex + 1;
                    Console.WriteLine(waifu.Name.Full);
                    if (nextWaifu > waifuList.Waifus.Count() - 1)
                        nextWaifu = 0;
                    player.CurrentWaifu = waifuList.Waifus[nextWaifu].Id;
                    break;
                case CmdType.Previous:
                    nextWaifu = waifuIndex - 1;
                    if (nextWaifu < 0)
                        nextWaifu = waifuList.Waifus.Count() - 1;
                    player.CurrentWaifu = waifuList.Waifus[nextWaifu].Id;
                    break;
                case CmdType.Remove:
                    nextWaifu = waifuIndex - 1;
                    if (nextWaifu < 0)
                        nextWaifu = waifuList.Waifus.Count() - 1;
                    player.CurrentWaifu = waifuList.Waifus[nextWaifu].Id;
                    await RemoveWaifu(player, waifuList.Waifus[waifuIndex].Id);
                    break;
                case CmdType.Landing:
                    player.CurrentWaifu = waifuList.Waifus.FirstOrDefault().Id;
                    waifuToDisplay = waifu;
                    break;
            }

            await UpdatePlayerData(player);
            emb = await HistoryBuilder(emb, player, waifuList.Waifus, nextWaifu);

            return emb;
        }
        static async Task<EmbedBuilder> HistoryBuilder(EmbedBuilder emBuilder, PlayerData player, List<Waifu> waifus, int nextWaifu)
        {
            Waifu waifu = new Waifu();
            if (player.CurrentWaifu > -1)
            {
                emBuilder
                    .WithTitle("No Favorites")
                    .WithDescription("Add a favorite, and it'll be added to this page.");
                return emBuilder;
            }
            waifu = await QueryWaifuById(player.CurrentWaifu);
            WaifuEmbedInfo embedInfo = CreateEmbedContent(waifu);
            emBuilder.WithImageUrl(embedInfo.ImageURL).WithColor(embedInfo.EmbedColor)
                .AddField(embedInfo.PrimaryField)
                .AddField(embedInfo.InfoField)
                .AddField(embedInfo.LinkField)
                .WithFooter(
                    new EmbedFooterBuilder()
                        .WithText($"Page {nextWaifu + 1} of {waifus.Count()}"));
            return emBuilder;
        }

        public static WaifuEmbedInfo CreateEmbedContent(Waifu waifu)
        {
            string info = "";
            string gender = "";
            try
            {
                gender = waifu.Gender.ToLower();
            }
            catch (Exception e) { }
            var r18 = new Emoji("\uD83D\uDD1E");
            var male = new Emoji("♂️");
            var female = new Emoji("♀️");
            var unknownGender = new Emoji("❔");
            if (waifu.Age != "Unknown")
                info += $"{waifu.Age} years old\n";
            else
                info += $"{waifu.Age}\n";
            info += $"{BirthdayFormatter(waifu.DateOfBirth.month, waifu.DateOfBirth.day)}\n";

            switch (gender)
            {
                case "unknown":
                    info += $"{unknownGender}";
                    break;
                case "female":
                    info += $"{female}";
                    break;
                case "male":
                    info += $"{male}";
                    break;
            }
            
            if (waifu.IsAdult.Value)
                info += $"\n{r18}";

            WaifuEmbedInfo embedInfo = new WaifuEmbedInfo
            {
                ImageURL = waifu.ImageURL,
                EmbedColor = GetColorByRarity(waifu.Rarity),
                PrimaryField = new EmbedFieldBuilder()
                    .WithName(waifu.Name.Full)
                    .WithValue($"{waifu.Rarity}\n{waifu.XpValue}xp\n")
                    .WithIsInline(true),
                InfoField = new EmbedFieldBuilder()
                    .WithName("Info")
                    .WithValue(info)
                    .WithIsInline(true),
                LinkField = new EmbedFieldBuilder()
                    .WithName("Links")
                    .WithValue($"[[Character Page](https://anilist.co/character/{waifu.Id})]\n[[Anime Page](https://anilist.co/anime/{waifu.SeriesId})]"),
                Id = waifu.Id
            };
            return embedInfo;
        }
        public static EmbedBuilder CreateDetailedEmbedContent(Waifu waifu)
        {
            string info = "";
            string gender = "";
            try
            {
                gender = waifu.Gender.ToLower();
            }
            catch (Exception e) { }
            var r18 = new Emoji("\uD83D\uDD1E");
            var male = new Emoji("♂️");
            var female = new Emoji("♀️");
            var unknownGender = new Emoji("❔");
            string ageBDayGender = "";
            if (waifu.Age != null)
                ageBDayGender += $"{waifu.Age} years old\n";
            if (waifu.DateOfBirth.day > 0 && waifu.DateOfBirth.month > 0)
            ageBDayGender += $"{BirthdayFormatter(waifu.DateOfBirth.month, waifu.DateOfBirth.day)}\n";

            switch (gender)
            {
                case "unknown":
                    ageBDayGender += $"{unknownGender}";
                    break;
                case "Female":
                    ageBDayGender += $"{female}";
                    break;
                case "Male":
                    ageBDayGender += $"{male}";
                    break;
            }

            if (waifu.IsAdult.Value)
                info += $"\n{r18}";
            string nameInfo = "";
            foreach (var name in waifu.Name.Alternative)
            {
                if (name == waifu.Name.Alternative.Last())
                    nameInfo += $"{name}";
                else
                    nameInfo += $"{name}, ";
            }

            EmbedBuilder emb = new EmbedBuilder()
                .WithImageUrl(waifu.ImageURL).WithColor(GetColorByRarity(waifu.Rarity))
                .AddField("Name", waifu.Name.Full, true);
            if (waifu.Name.Alternative.Count() > 0)
                emb.AddField("Alternative Names", nameInfo, true);
            if (waifu.Name.Native.Length > 0)
                emb.AddField("Native Name", waifu.Name.Native, true);
            if (waifu.Age != null)

                emb
                    .AddField("Age", waifu.Age, true)
                    .AddField("Birthday", BirthdayFormatter(waifu.DateOfBirth.month, waifu.DateOfBirth.day), true)
                    .AddField("Gender", gender, true)
                    .AddField("XP Value", waifu.XpValue.ToString("N0"), true)
                    .AddField("Favorites", waifu.Favorites.ToString("N0"), true)
                    .AddField("Media", "The following is a list of media this character appears in.", false);
            foreach (var node in waifu.Media.nodes.Take(10))
            {
                string mediaInfo = $"";
                if (node.Title.English != null)
                    mediaInfo += $"{node.Title.English}\n";
                if (node.Title.Native != null)
                    mediaInfo += $"{node.Title.Native}\n";
                if (node.Type != null)
                    mediaInfo += $"{node.Type}";
                emb.AddField(node.Title.UserPreferred, mediaInfo);
            }
            return emb;
        }
        static string BirthdayFormatter(int? month, int? day)
        {
            if (day > -1)
                return $"🎂 {month}/{day}";
            else if (month > -1 && day == -1)
                return $"🎂 {GetMonthByInt(month.Value)}";
            else
                return "🎂 Unknown";
        }



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

    public class ButtonHandler : EmbedHandler
    {
        public static async Task MyButtonHandler(SocketMessageComponent component)
        {
            string id = component.Data.CustomId;

            // We can now check for our custom id
            switch (GetCommandType(component.Data.CustomId))
            {
                case CmdType.Catching:
                    await component.RespondAsync(
                        embed: BuildEmbed(id, component.User.Username, component.User.Id).Result.Build(), components: ComponentHandler.BuildComponent(id).Build());
                    break;
                case CmdType.Favorite:
                case CmdType.HistFavorite:
                    if (!component.Message.Embeds.First().Title.Contains(component.User.Username))
                    {
                        await component.RespondAsync(embed: new EmbedBuilder().WithDescription("You can't favorite someone else's waifu!").WithColor(Values.errorColor).Build(), ephemeral: true);
                        break;
                    }
                    string waifuName = component.Message.Embeds.FirstOrDefault().Fields.First().Name;
                    await component.RespondAsync(
                        embed: BuildEmbed(id, component.User.Username, component.User.Id, waifuName).Result.Build(), components: ComponentHandler.BuildComponent(id).Build(), ephemeral: true);
                    break;
                case CmdType.Home:
                    await component.RespondAsync(
                        embed: BuildEmbed(id, component.User.Username, component.User.Id).Result.Build(), components: ComponentHandler.BuildComponent(id).Build());
                    break;
                case CmdType.Favorites:
                case CmdType.Next:
                case CmdType.Previous:
                case CmdType.History:
                case CmdType.HistNext:
                case CmdType.HistPrevious:
                case CmdType.Remove:
                    await ButtonUpdateHelper(component, id);
                    break;

                case CmdType.Leaderboard:
                    await ButtonUpdateHelper(component, id);
                    break;
                case CmdType.Upgrade:
                    await component.RespondAsync(
                        embed: BuildEmbed(id, component.User.Username, component.User.Id).Result.Build(), components: ComponentHandler.BuildComponent(id).Build());
                    break;
                case CmdType.Gelbooru:
                case CmdType.GelNext:
                case CmdType.GelPrevious:
                    await component.UpdateAsync(
                        func: async (msg) =>
                        {
                            msg.Embeds = new Optional<Embed[]>(new Embed[] { GelEmbedBuilder(component.User.Username, id).Result.Build() });
                            msg.Components = ComponentHandler.BuildComponent(id).Build();
                        });
                    break;
            }
        }
        public static async Task ButtonUpdateHelper(SocketMessageComponent component, string idType)
        {
            await component.UpdateAsync(
                        func: async (msg) =>
                        {
                            msg.Embeds = new Optional<Embed[]>(new Embed[] { EmbedHandler.BuildEmbed(idType, component.User.Username, component.User.Id).Result.Build() });
                            msg.Components = ComponentHandler.BuildComponent(idType).Build();
                        });
        }
        
    }
    public class ComponentHandler : EmbedHandler
    {
        public static ComponentBuilder BuildComponent(string type)
        {
            var comBuilder = new ComponentBuilder();

            switch (GetCommandType(type))
            {
                case CmdType.Catching:
                    AddButtons(comBuilder, ("Roll", "catching", ButtonStyle.Primary), ("Favorite", "favorite", ButtonStyle.Success), ("Return", "home", ButtonStyle.Secondary));
                    break;

                case CmdType.Home:
                    AddButtons(comBuilder, ("Roll", "catching", ButtonStyle.Primary), ("Favorites", "favorites", ButtonStyle.Success), ("History", "history", ButtonStyle.Secondary), ("Leaderboard", "leaderboard", ButtonStyle.Secondary));
                    break;
                case CmdType.Favorite:
                case CmdType.HistFavorite:
                    AddButtons(comBuilder, ("Roll", "catching", ButtonStyle.Primary), ("Home", "home", ButtonStyle.Secondary));
                    break;
                case CmdType.Favorites:
                case CmdType.Next:
                case CmdType.Previous:
                case CmdType.Remove:
                    AddButtons(comBuilder, ("Previous", "previous", ButtonStyle.Primary), ("Next", "next", ButtonStyle.Primary), ("Remove", "remove", ButtonStyle.Danger), ("Return", "home", ButtonStyle.Secondary));
                    break;

                case CmdType.History:
                case CmdType.HistNext:
                case CmdType.HistPrevious:
                    AddButtons(comBuilder, ("Previous", "histPrevious", ButtonStyle.Primary), ("Next", "histNext", ButtonStyle.Primary), ("Favorite", "histFavorite", ButtonStyle.Success), ("Return", "home", ButtonStyle.Secondary));
                    break;

                case CmdType.Leaderboard:
                    AddButtons(comBuilder, ("Return", "home"));
                    break;

                case CmdType.Gelbooru:
                case CmdType.GelNext:
                case CmdType.GelPrevious:
                    AddButtons(comBuilder, ("Previous", "gelPrevious", ButtonStyle.Primary), ("Next", "gelNext", ButtonStyle.Primary));
                    break;
               
            }

            return comBuilder;
        }

        
        private static void AddButtons(ComponentBuilder comBuilder, params (string label, string customId, ButtonStyle style)[] buttons)
        {
            foreach (var (label, customId, style) in buttons)
            {
                comBuilder.WithButton(label, customId, style);
            }
        }

        private static void AddButtons(ComponentBuilder comBuilder, params (string label, string customId)[] buttons)
        {
            foreach (var (label, customId) in buttons)
            {
                comBuilder.WithButton(label, customId);
            }
        }
    }
}
