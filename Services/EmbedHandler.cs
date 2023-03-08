using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using ALICEIDLE.Logic;
using ALICEIDLE.Gelbooru;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http.Headers;
using System.Numerics;

namespace ALICEIDLE.Services
{
    public class EmbedHandler
    {
        public static bool hasIterated { get; set; } = false;
        public static List<EmbedBuilder> waifuPages { get; set; }
        public static List<PlayerData> playerList { get; set; } = new List<PlayerData>();
        public static Waifu waifuToDisplay { get; set; }
        public static Color HomeColor = new Color(23, 178, 255);
        public static UserData userData { get; set; }
        public static OwnedWaifus wList { get; set; }

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
            bool playerExists = await SqlDBHandler.PlayerExists(uid);
            if (!playerExists)
            {
                playerData = await SqlDBHandler.InsertPlayerData(username, uid);
                playerList.Add(playerData);
            }
            /*
            if(playerList.Count() > 0)
            {
                foreach (var player in playerList)
                {
                    if (uid == player.Id)
                        playerData = player;
                }
            }
            */
            else
                playerData = await SqlDBHandler.RetrievePlayerData(uid);
            if(playerData == null)
                playerData = await SqlDBHandler.RetrievePlayerData(uid);

            //var playerData = await Values.RetrievePlayerDataByID(uid, username);
            //var waifuData = Values.RetrievePlayerDataList(uid, username).First();
            int waifuIndex = 0;
            int nextWaifu = 0;

            EmbedBuilder emBuilder = new EmbedBuilder()
                .WithTitle($"{username} [lvl {Values.CalculateLevel(playerData.Xp)}]");

            switch (type)
            {

                case "catching":
                    var waifu = await Values.CatchWaifu(playerData);
                    if (waifu == null)
                        break;

                    WaifuEmbedInfo embedInfo = CreateEmbedContent(waifu);
                    emBuilder.WithImageUrl(embedInfo.ImageURL).WithColor(embedInfo.EmbedColor)
                        .AddField(embedInfo.PrimaryField)
                        .AddField(embedInfo.InfoField)
                        .AddField(embedInfo.LinkField)
                        .WithFooter(new EmbedFooterBuilder().WithText($"{waifu.Series}"));
                    break;

                case "favorite":
                case "histFavorite":
                    Waifu _waifu = await Values.Favorite(playerData, name);
                    if (_waifu == null)
                        emBuilder.AddField("Duplicate", $"{name} is already in your favorites", false).WithColor(Values.errorColor);
                    else
                        emBuilder.AddField("Favorited", $"Added {name} to favorites", false).WithColor(Values.successColor);
                    break;
                case "home":
                    emBuilder.WithColor(HomeColor)
                        .AddField("Level", Values.CalculateLevel(playerData.Xp).ToString("N0"), true)
                        .AddField("Favorites", playerData.OwnedWaifus.Count().ToString("N0"), true)
                        .AddField("Rolled", playerData.TotalRolls.ToString("N0"), true)
                        .AddField("XP", $"{playerData.Xp.ToString("N0")}/{Values.CalculateXPRequired(playerData.Xp).ToString("N0")}", true);
                    break;
                case "leaderboard":
                    string leaderboardPlayers = "";
                    var players = await Values.RetrieveAllPlayerData();
                    int i = 1;
                    foreach (var player in players)
                    {
                        emBuilder.AddField($"**{player.Name}**", $"Score: {player.Xp.ToString("N0")}\nRolls: {player.TotalRolls.ToString("N0")}");
                        //leaderboardPlayers += $"**#{i}** {player.Name}\nScore: {player.Xp.ToString("N0")}\nTotal rolls: {player.TotalRolls.ToString("N0")}\n\n";
                        i++;
                    }
                    emBuilder.WithTitle("Leaderboard");//.WithDescription(leaderboardPlayers);

                    break;
                case "favorites":
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(Values.FavoritesToIdList(playerData.OwnedWaifus), true);
                    emBuilder = await WaifuIterator(wList, playerData, emBuilder, "landing");
                    break;
                case "next":
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(Values.FavoritesToIdList(playerData.OwnedWaifus), true);
                    emBuilder = await WaifuIterator(wList, playerData, emBuilder, "next");
                    break;
                case "previous":
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(Values.FavoritesToIdList(playerData.OwnedWaifus), true);
                    emBuilder = await WaifuIterator(wList, playerData, emBuilder, "previous");
                    break;
                case "history":
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(playerData.RollHistory, true);
                    wList.Waifus.Reverse();
                    //Console.WriteLine($"rollhistory:{playerData.RollHistory[0]}|wList:{wList.Waifus[0].Id}");
                    emBuilder = await WaifuIterator(wList, playerData, emBuilder, "landing");
                    break;
                case "histNext":
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(playerData.RollHistory, true);
                    wList.Waifus.Reverse();
                    emBuilder = await WaifuIterator(wList, playerData, emBuilder, "next");
                    break;
                case "histPrevious":
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(playerData.RollHistory, true);
                    wList.Waifus.Reverse();
                    emBuilder = await WaifuIterator(wList, playerData, emBuilder, "previous");
                    break;
                case "remove":
                    wList.Waifus = await SqlDBHandler.QueryWaifuByIds(Values.FavoritesToIdList(playerData.OwnedWaifus), true);
                    emBuilder = await WaifuIterator(wList, playerData, emBuilder, "remove");
                    break;
            }
            return emBuilder;
        }

        static async void WafaifuListCheck(PlayerData player, string btnType, string waifuType)
        {
            switch(waifuType)
            {
                case "favorites":
                    if (wList.Waifus.Count > 0 && wList.Waifus.Count <= player.OwnedWaifus.Count())
                        return;
                    else
                        wList.Waifus = await SqlDBHandler.QueryWaifuByIds(Values.FavoritesToIdList(player.OwnedWaifus));
                    break;
            }
            return;
        }
        static async  Task<EmbedBuilder> WaifuIterator(OwnedWaifus waifuList, PlayerData player, EmbedBuilder emb, string arg)
        {
            int waifuIndex = 0;
            int nextWaifu = 0;
            Waifu waifu = await SqlDBHandler.QueryWaifuById(player.CurrentWaifu);
            switch (arg)
            {
                case "next":
                    waifuIndex = waifuList.Waifus.FindIndex(a => a.Name.Full == waifu.Name.Full);
                    nextWaifu = waifuIndex + 1;
                    Console.WriteLine(waifu.Name.Full);
                    if (nextWaifu > waifuList.Waifus.Count() - 1)
                        nextWaifu = 0;
                    player.CurrentWaifu = waifuList.Waifus[nextWaifu].Id;
                    break;
                case "previous":
                    waifuIndex = waifuList.Waifus.FindIndex(a => a.Name.Full == waifu.Name.Full);
                    nextWaifu = waifuIndex - 1;
                    if (nextWaifu < 0)
                        nextWaifu = waifuList.Waifus.Count() - 1;
                    player.CurrentWaifu = waifuList.Waifus[nextWaifu].Id;
                    break;
                case "remove":
                    waifuIndex = waifuList.Waifus.FindIndex(a => a.Name.Full == waifu.Name.Full);
                    nextWaifu = waifuIndex - 1;
                    if (nextWaifu < 0)
                        nextWaifu = waifuList.Waifus.Count() - 1;
                    player.CurrentWaifu = waifuList.Waifus[nextWaifu].Id;
                    Values.RemoveWaifu(player, waifuList.Waifus[waifuIndex].Id);
                    break;
                case "landing":
                    player.CurrentWaifu = waifuList.Waifus.FirstOrDefault().Id;
                    waifuToDisplay = waifu;
                    break;
            }
            await SqlDBHandler.UpdatePlayerData(player);
            emb = await HistoryBuilder(emb, player, waifuList.Waifus, nextWaifu);
            return emb;
        }
        static async Task<EmbedBuilder> HistoryBuilder(EmbedBuilder emBuilder, PlayerData player, List<Waifu> waifus, int nextWaifu)
        {
            Waifu waifu = new Waifu();
            string info = "";
            if (player.CurrentWaifu == null)
            {
                emBuilder
                    .WithTitle("No Favorites")
                    .WithDescription("Add a favorite, and it'll be added to this page.");
                return emBuilder;
            }
            waifu = await SqlDBHandler.QueryWaifuById(player.CurrentWaifu);
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
                EmbedColor = Values.GetColorByRarity(waifu.Rarity),
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

            //nameInfo += $"\n\n**Native**\n{waifu.Name.Native}";
            EmbedBuilder emb = new EmbedBuilder()
                .WithImageUrl(waifu.ImageURL).WithColor(Values.GetColorByRarity(waifu.Rarity))
                .AddField("Name", waifu.Name.Full, true);
            if (waifu.Name.Alternative.Count() > 0)
                emb.AddField("Alternative Names", nameInfo, true);
            if (waifu.Name.Native.Length > 0)
                emb.AddField("Native Name", waifu.Name.Native, true);

            //.AddField("Birthday & Gender", ageBDayGender, true)
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
                return $"🎂 {Values.GetMonthByInt(month.Value)}";
            else
                return "🎂 Unknown";
        }
    }

    public class ButtonHandler
    {
        public static async Task MyButtonHandler(SocketMessageComponent component)
        {
            string id = component.Data.CustomId;
            //var originalMsg = component.GetOriginalResponseAsync().Result;
            
            // We can now check for our custom id
            switch (component.Data.CustomId)
            {
                case "catching":
                    await component.RespondAsync(
                        embed: EmbedHandler.BuildEmbed(id, component.User.Username, component.User.Id).Result.Build(), components: ComponentHandler.BuildComponent(id).Build());
                    break;
                case "favorite":
                case "histFavorite":
                    if (!component.Message.Embeds.First().Title.Contains(component.User.Username))
                    {
                        await component.RespondAsync(embed: new EmbedBuilder().WithDescription("You can't favorite someone else's waifu!").WithColor(Values.errorColor).Build(), ephemeral: true);
                        break;
                    }
                    string waifuName = component.Message.Embeds.FirstOrDefault().Fields.First().Name;
                    await component.RespondAsync(
                        embed: EmbedHandler.BuildEmbed(id, component.User.Username, component.User.Id, waifuName).Result.Build(), components: ComponentHandler.BuildComponent(id).Build(), ephemeral: true);
                    break;
                case "home":
                    await component.RespondAsync(
                        embed: EmbedHandler.BuildEmbed(id, component.User.Username, component.User.Id).Result.Build(), components: ComponentHandler.BuildComponent(id).Build());
                    break;
                case "favorites":
                case "next":
                case "previous":
                case "history":
                case "histNext":
                case "histPrevious":
                case "remove":
                    await ButtonUpdateHelper(component, id);
                    break;

                case "leaderboard":
                    await ButtonUpdateHelper(component, id);
                    break;
                case "upgrade":
                    await component.RespondAsync(
                        embed: EmbedHandler.BuildEmbed(id, component.User.Username, component.User.Id).Result.Build(), components: ComponentHandler.BuildComponent(id).Build());
                    break;
                case "gelbooru":
                case "gelNext":
                case "gelPrevious":
                    await component.UpdateAsync(
                        func: async (msg) =>
                        {
                            msg.Embeds = new Optional<Embed[]>(new Embed[] { EmbedHandler.GelEmbedBuilder(component.User.Username, id).Result.Build() });
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
    public class ComponentHandler
    {
        public static ComponentBuilder BuildComponent(string type)
        {
            var comBuilder = new ComponentBuilder();

            switch (type)
            {
                case "catching":
                    AddButtons(comBuilder, ("Roll", "catching", ButtonStyle.Primary), ("Favorite", "favorite", ButtonStyle.Success), ("Return", "home", ButtonStyle.Secondary));
                    break;

                case "home":
                    AddButtons(comBuilder, ("Roll", "catching", ButtonStyle.Primary), ("Favorites", "favorites", ButtonStyle.Success), ("History", "history", ButtonStyle.Secondary), ("Leaderboard", "leaderboard", ButtonStyle.Secondary));
                    break;
                case "favorite":
                case "histFavorite":
                    AddButtons(comBuilder, ("Roll", "catching", ButtonStyle.Primary), ("Home", "home", ButtonStyle.Secondary));
                    break;
                case "favorites":
                case "next":
                case "previous":
                case "remove":
                    AddButtons(comBuilder, ("Previous", "previous", ButtonStyle.Primary), ("Next", "next", ButtonStyle.Primary), ("Remove", "remove", ButtonStyle.Danger), ("Return", "home", ButtonStyle.Secondary));
                    break;

                case "history":
                case "histNext":
                case "histPrevious":
                    AddButtons(comBuilder, ("Previous", "histPrevious", ButtonStyle.Primary), ("Next", "histNext", ButtonStyle.Primary), ("Favorite", "histFavorite", ButtonStyle.Success), ("Return", "home", ButtonStyle.Secondary));
                    break;

                case "leaderboard":
                    AddButtons(comBuilder, ("Return", "home"));
                    break;

                case "gelbooru":
                case "gelNext":
                case "gelPrevious":
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
