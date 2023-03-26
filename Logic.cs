using ALICEIDLE;
using Discord;
using Newtonsoft.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ALICEIDLE.Services;

namespace ALICEIDLE.Logic
{
    public class WaifuHandler : SqlDBHandler
    {
        public static List<Character> characterList { get; set; }
        public static List<Waifu> waifuList { get; set; }
        public static List<double> weights { get; set; }
        public static bool weightsGenerated { get; set; } = false;
       

        public static async Task <Waifu> CatchWaifu(PlayerData player)
        {
            if (player.OwnedWaifus.Count > 1)
                if(player.OwnedWaifus[0].Item1 == -1)
                    player.OwnedWaifus.RemoveAt(0);

            double[] itemProbabilities = { 1, 0.05, 0.025, 0.01 };

            // Pity rate settings
            int pityRateRolls = 50;  // Number of rolls required to trigger pity rate
            double pityRateIncrease = 0.005;  // Amount to increase each item's probability during pity rate
            
            // Roll the gacha
            int rollResult = RollGacha(itemProbabilities, pityRateRolls, pityRateIncrease, player);
            
            bool duplicate = false;
            List<int> favoriteIds = FavoritesToIdList(player.OwnedWaifus);
            Waifu selectedWaifu = null;

            selectedWaifu = await GetRandomWaifuByTier(rollResult, player);
            duplicate = await IsDuplicateRoll(selectedWaifu, player);

            //Check if the roll is a duplicate of any of our favorites
            if (await IsDuplicateFavorite(await IdListToWaifuList(favoriteIds), selectedWaifu))
            {
                var favoritedWaifu = player.OwnedWaifus.Find(d => d.Item1 == selectedWaifu.Id);
                favoritedWaifu = new Tuple<int, int>(favoritedWaifu.Item1, favoritedWaifu.Item2 + 1);
            }
            else if (duplicate)
            {
                while (duplicate)
                {
                    Console.WriteLine($"Rolled Duplicate: {selectedWaifu.Name.Full} (id){selectedWaifu.Id}");
                    rollResult = RollGacha(itemProbabilities, pityRateRolls, pityRateIncrease, player);
                    selectedWaifu = await GetRandomWaifuByTier(rollResult, player);
                    duplicate = await IsDuplicateRoll(selectedWaifu, player);
                }
            }

            await ModifyPlayerData(player, selectedWaifu);
            
            return selectedWaifu;
        }
        public static async Task<PlayerData> ModifyPlayerData(PlayerData player, Waifu selectedWaifu)
        {
            string rarity = CalculateRarity(selectedWaifu.Favorites);
            int xpValue = CalculateXPValue(selectedWaifu.Favorites, rarity);
            int playerLevel = CalculateLevel(player.Xp);
            int waifuPoints = CalculateWaifuPoints(selectedWaifu.Favorites);
            int levelUpPoints = CalculateLevelUpPoints(playerLevel);

            if (playerLevel > player.Level)
                if (playerLevel - player.Level > 1)
                    if(playerLevel - (playerLevel - player.Level) % 5 == 0)
                    {
                        player.Points += levelUpPoints;
                        await Variables.msgComponent.Channel.SendMessageAsync($"Recieved {levelUpPoints} points for reaching level {playerLevel}!");
                    }    
                if(playerLevel % 5 == 0)
                {
                    player.Points += levelUpPoints;
                    await Variables.msgComponent.Channel.SendMessageAsync($"Recieved {levelUpPoints} points for reaching level {playerLevel}!");
                }


            player.WaifuAmount++;
            player.Xp += xpValue;
            player.Points += waifuPoints;
            player.Level = CalculateLevel(player.Xp);
            player.LastCharacterRolled = selectedWaifu.Id;
            if (player.RollHistory.Count >= 100)
                player.RollHistory.RemoveAt(0);
            player.RollHistory.Add(selectedWaifu.Id);
            player.TotalRolls++;

            await UpdatePlayerData(player);
            return player;
        }
        
        public static async Task<bool> IsDuplicateRoll(Waifu character, PlayerData player)
        {
            var waifus = player.OwnedWaifus;
            for (int i = 0; i < waifus.Count(); i++)
                if (character.Id == waifus[i].Item1)
                    player.OwnedWaifus[i] = new Tuple<int, int>(waifus[i].Item1, waifus[i].Item2 + 1);
            for (int i = 0; i < player.RollHistory.Count; ++i)
                if (player.RollHistory[i] == character.Id)
                    return true;
            return false;
        }
        public static async Task<bool> IsDuplicateFavorite(List<Waifu> favorites, Waifu waifu)
        {
            if (favorites == null || favorites.Count() <= 0)
                return false;
            var match = favorites.Find(d => d.ImageURL == waifu.ImageURL);
            if (match != null)
                return true;
            else
                return false;
        }
        public static async Task<List<Waifu>> IdListToWaifuList(List<int> ids)
        {
            List<Waifu> waifus = new List<Waifu>();
            if (ids.Count <= 0)
                return null;
            else
                waifus = QueryWaifuByIds(ids).Result;
            return waifus;
        }
        public static async Task<Waifu> CharacterToWaifu(Character character)
        {
            Waifu waifu = new Waifu()
            {
                Name = character.Name,
                DateOfBirth = character.DateOfBirth,
                Favorites = character.Favourites,
                Gender = character.Gender,
                Series = character.Media.nodes.First().Title.UserPreferred,
                Media = character.Media,
                ImageURL = character.Image.Large,
                Rarity = CalculateRarity(character.Favourites),
                XpValue = CalculateXPValue(character.Favourites, CalculateRarity(character.Favourites)),
                Id = character.Id,
                SeriesId = character.Media.nodes.First().Id,
                IsAdult = character.Media.nodes.First().IsAdult
            };
            if (character.Gender == null)
                character.Gender = "unknown";
            
            if (character.DateOfBirth.month != null)
            {
                waifu.DateOfBirth.month = character.DateOfBirth.month;
                waifu.DateOfBirth.day = character.DateOfBirth.day;
            }
            
            return waifu;
        }
        public static async Task<Waifu> Favorite(PlayerData player, string name)
        {
            List<int> favoriteIds = FavoritesToIdList(player.OwnedWaifus);
            Waifu _waifu = QueryWaifuByName(name).Result;

            if(player.OwnedWaifus.Count > 0)
                if (await IsDuplicateFavorite(await IdListToWaifuList(favoriteIds), _waifu))
                    return null;

            player.OwnedWaifus.Add(new Tuple<int, int>(_waifu.Id, 0));
            await UpdatePlayerData(player);
            
            return _waifu;
        }
        public static async Task RemoveWaifu(PlayerData player, int waifuId)
        {
            Console.WriteLine(player.OwnedWaifus.Count);
            
            foreach (var tuple in player.OwnedWaifus.ToList())
                if (tuple.Item1 == waifuId)
                    player.OwnedWaifus.RemoveAt(player.OwnedWaifus.IndexOf(tuple));
            
            Console.WriteLine(player.OwnedWaifus.Count);
        }
        public static async Task HistoryFavorite(PlayerData player)
        {
            List<int> favoriteIds = FavoritesToIdList(player.OwnedWaifus);
            Waifu waifu = await QueryWaifuById(player.CurrentWaifu);

            if (await IsDuplicateFavorite(await IdListToWaifuList(favoriteIds), waifu))
                return;
            else
            {
                player.OwnedWaifus.Add(new Tuple<int, int>(waifu.Id, 0));
                await UpdatePlayerData(player);
            }

        }
        public static List<int> FavoritesToIdList(List<Tuple<int, int>> favorites)
        {
            List<int> ids = new List<int>();

            foreach (var favorite in favorites)
                ids.Add(favorite.Item1);
            
            return ids;
        }
        public static async Task<Waifu> GetCharacterByID(int id)
        {
            Waifu charMatch = new Waifu();
            try
            {
                charMatch = waifuList.FirstOrDefault(c => c.Id == id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            return charMatch;
        }
        public static async Task<Waifu> GetCharacterByName(string searchName)
        {
            List<string> names = new List<string>();
            
            foreach (var waifu in waifuList)
            {
                names.Add(waifu.Name.Full);
                foreach (var name in waifu.Name.Alternative)
                    names.Add(name);
            }
                
            string closestMatch = FindClosestName(searchName, names, 6);
            Waifu charMatch = waifuList.FirstOrDefault(c => c.Name.Full.ToLower() == closestMatch.ToLower());
            
            if (charMatch == null)
                foreach (var waifu in waifuList)
                    foreach (var name in waifu.Name.Alternative)
                        if (name == closestMatch)
                            charMatch = waifu;

            return charMatch;
        }
        static async Task<Waifu> GetRandomWaifuByTier(int rarity, PlayerData player)
        {
            return QueryWaifuByTier(rarity, player.GenderPreference).Result;
        }
        static async Task<Waifu> GetRandomCharacterByTier(int rarity)
        {
            var _waifuList = waifuList.OrderBy(p => p.Favorites).Reverse().ToList();
            Random rand = new Random();
            int index = 0;

            if (rarity == 0)
                index = rand.Next(1001, waifuList.Count);
            if (rarity == 1)
                index = rand.Next(251, 1000);
            if (rarity == 2)
                index = rand.Next(51, 250);
            if (rarity == 3)
                index = rand.Next(0, 50);

            return _waifuList[index];
        }
        public static Color GetColorByRarity(string rarity)
        {
            switch (rarity)
            {
                case "Normal":
                    return EmbedColors.nColor;

                case "Rare":
                    return EmbedColors.rColor;

                case "Super Rare":
                    return EmbedColors.srColor;

                case "Super Super Rare":
                    return EmbedColors.ssrColor;
            }
            return EmbedColors.ssrColor;
        }

        static int RollGacha(double[] itemProbabilities, int pityRateRolls, double pityRateIncrease, PlayerData player)
        {
            // Generate a random number between 0 and 1
            double roll = new Random().NextDouble();
            
            if (roll < itemProbabilities[3])
            {
                // Item 4 was rolled
                player.RollsSinceLastSSR = 0;
                ModifyPlayerData(player);
                return 3;
            }
            if (roll < itemProbabilities[2])
            {
                player.RollsSinceLastSSR = 0;
                ModifyPlayerData(player);
                return 2;
            } // Check for pity rate
            if (player.RollsSinceLastSSR > pityRateRolls)
            {
                for (int j = 3; j > itemProbabilities.Length - 4; j--)
                    itemProbabilities[j] += pityRateIncrease * (player.RollsSinceLastSSR * 0.01);
                Console.WriteLine($"Pity Rate: N {itemProbabilities[0].ToString("#.0000")} | R {itemProbabilities[1].ToString("#.0000")} | SR {itemProbabilities[2].ToString("#.0000")} | SSR {itemProbabilities[3].ToString("#.0000")}");
                
            }
            // No pity rate - loop through the item probabilities and determine which item was rolled

            player.RollsSinceLastSSR++;
            return roll < itemProbabilities[3] ? 3 : roll <= itemProbabilities[2] ? 2 : roll <= itemProbabilities[1] ? 1 : 0;
            
            // If we get to this point, something went wrong - return -1
            return -1;
        }

        public static async void ModifyPlayerData(PlayerData playerData)
        {
            List<PlayerData> playerDataList = await RetrieveAllPlayerData();
            PlayerData storedPlayerData = playerDataList.Find(d => d.Id == playerData.Id);
            
            if (storedPlayerData == null)
            {
                storedPlayerData = await CreatePlayerData(playerData.Id, playerData.Name);
                playerDataList.Add(storedPlayerData);
            }

            storedPlayerData.Xp = playerData.Xp;
            storedPlayerData.Level = playerData.Level;
            storedPlayerData.Points = playerData.Points;
            storedPlayerData.WaifuAmount = playerData.WaifuAmount;
            storedPlayerData.RollsSinceLastSSR = playerData.RollsSinceLastSSR;
            storedPlayerData.LastCharacterRolled = playerData.LastCharacterRolled;
            storedPlayerData.CurrentWaifu = playerData.CurrentWaifu;
            storedPlayerData.OwnedWaifus = playerData.OwnedWaifus;
            storedPlayerData.RollHistory = playerData.RollHistory;
            storedPlayerData.GenderPreference = playerData.GenderPreference;
            storedPlayerData.TotalRolls = playerData.TotalRolls;

            WriteAllPlayerData(playerDataList);
            // Add the new PlayerData object to the list


        }
        public static async void WriteAllPlayerData(List<PlayerData> playerDataList)
        {
            // Serialize the list of PlayerData objects doubleo a JSON string
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(playerDataList, options);

            // Write the JSON string to the file
            File.WriteAllText("waifu_data.json", jsonString);
        }
        public static async Task<PlayerData> RetrievePlayerDataByID(ulong id, string username)
        {
            string fileContents = File.ReadAllText("waifu_data.json");

            // If the file is not empty, deserialize the contents of a list of PlayerData objects
            List<PlayerData> playerDataList = new List<PlayerData>();

            if (fileContents != "")
            {
                playerDataList = JsonConvert.DeserializeObject<List<PlayerData>>(fileContents);
            }

            PlayerData playerData = playerDataList.Find(d => d.Id == id);

            if (playerData == null)
                playerData = await CreatePlayerData(id, username);
            
            return playerData;

        }
        public static async Task<List<PlayerData>> RetrievePlayerDataList(ulong id, string name)
        {
            // Check if the file exists, if not create it
            if (!File.Exists("waifu_data.json"))
                File.Create("waifu_data.json").Close();
            // Read the contents of the file
            string fileContents = File.ReadAllText("waifu_data.json");

            // If the file is not empty, deserialize the contents of a list of PlayerData objects
            List<PlayerData> waifuDataList = new List<PlayerData>();

            if (fileContents != "")
                waifuDataList = JsonConvert.DeserializeObject<List<PlayerData>>(fileContents);

            return waifuDataList;
        }
        
        public static async Task<PlayerData> CreatePlayerData(ulong id, string username)
        {
            PlayerData playerData = new PlayerData
            {
                Name = username,
                Id = id,
                GenderPreference = "None",
                OwnedWaifus = new List<Tuple<int, int>>(),
                RollHistory = new List<int>()
            };

            playerData.OwnedWaifus.Add(new Tuple<int, int>(-1, 0));
            return playerData;
        }
        public static async Task<List<PlayerData>> RetrieveAllPlayerData()
        {
            if (!File.Exists("waifu_data.json"))
                File.Create("waifu_data.json").Close();

            // Read the contents of the file
            string fileContents = File.ReadAllText("waifu_data.json");

            // If the file is not empty, deserialize the contents doubleo a list of PlayerData objects
            List<PlayerData> waifuDataList = new List<PlayerData>();
            if (fileContents != "")
                waifuDataList = JsonConvert.DeserializeObject<List<PlayerData>>(fileContents);

            return waifuDataList.OrderBy(p => p.Xp).Reverse().ToList();
        }
        public static int CalculateWaifuPoints(int favorites)
        {
            int value = Convert.ToInt32(favorites * 0.1);

            if (value < 1)
                value = 1;

            return value;
        }
        public static int CalculateLevelUpPoints(int level)
        {
            int levelUpPoints = level * 100;
            return levelUpPoints;
        }
        public static int CalculateXPValue(int favorites, string rarity)
        {
            int xp = Convert.ToInt32(favorites * 0.5);

            if (xp <= 9)
                xp = 10;

            return xp;
        }
        public static int CalculateLevel(int totalXP)
        {
            int xp = totalXP;

            int level = 1; // start at level 1
            int xpRequired = 100; // initial XP requirement for level 1

            while (xp >= xpRequired)
            {
                level++; // increase level
                xp -= xpRequired; // subtract XP requirement for previous level from current XP value
                xpRequired = (int)(xpRequired * 1.3); // scale XP requirement for next level by 10%
            }

            return level;
        }
        public static int CalculateXPFromLevel(int level)
        {
            int baseXp = 100;
            for (int i = 0; i < level; i++)
                baseXp = (int)(baseXp * 1.3);
            return baseXp;
        }
        public static int CalculateXPRequired(int totalXP)
        {
            int xp = totalXP;

            int level = 1; // start at level 1
            int xpRequired = 100; // initial XP requirement for level 1

            while (xp >= xpRequired)
            {
                level++; // increase level
                xp -= xpRequired; // subtract XP requirement for previous level from current XP value
                xpRequired = (int)(xpRequired * 1.3); // scale XP requirement for next level by 10%
            }

            return totalXP + xpRequired;
        }
        public static string CalculateRarity(int favorites)
        {
            return favorites < 1129 ? "Normal" : favorites <= 2315 ? "Rare" : favorites < 8356 ? "Super Rare" : "Super Super Rare";
        }

        public static void GenerateWaifuList()
        {
            string content = File.ReadAllText(@"E:\Visual Studio 2017\Projects\ALICEIDLE\bin\Debug\net7.0\waifus.json");
            waifuList = JsonConvert.DeserializeObject<List<Waifu>>(content);
        }
        public static void GenerateCharacterList()
        {
            string content = File.ReadAllText(@"E:\Visual Studio 2017\Projects\ALICEIDLE\bin\Debug\net7.0\characters.json");
            characterList = JsonConvert.DeserializeObject<Page>(content).Characters;
        }
        public static Waifu[] WaifuList()
        {
            return null;
        }
        public static string GetMonthByInt(int monthInt)
        {
            string[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            return months[monthInt-1];
        }
        public static string FindClosestName(string searchName, List<string> names, int threshold)
        {
            int minDistance = int.MaxValue;
            string closestName = "";

            foreach (string name in names)
            {
                int distance = LevenshteinDistance(searchName, name);

                if (distance < minDistance && distance <= threshold)
                {
                    minDistance = distance;
                    closestName = name;
                }
            }

            return closestName;
        }
        public static int LevenshteinDistance(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= t.Length; j++)
                d[0, j] = j;

            for (int j = 1; j <= t.Length; j++)
                for (int i = 1; i <= s.Length; i++)
                    if (s[i - 1] == t[j - 1])
                        d[i, j] = d[i - 1, j - 1];
                    else
                        d[i, j] = Math.Min(d[i - 1, j] + 1, Math.Min(d[i, j - 1] + 1, d[i - 1, j - 1] + 1));

            return d[s.Length, t.Length];
        }
    }
}
public class Calculations
{
    public static string TimeElapsed(DateTime startTime)
    {
        DateTime endTime = DateTime.Now;
        TimeSpan elapsedTime = endTime - startTime;

        return elapsedTime.ToString("mm':'ss");
    }
}

public class MyDbContext : DbContext
{
    public DbSet<Waifu> Waifu { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Waifu>(entity =>
        {
            entity.ToTable("Waifu");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.OwnsOne(e => e.Name, name =>
            {
                name.Property(n => n.Full).HasColumnName("Name_Full");
                name.Property(n => n.First).HasColumnName("Name_First");
                name.Property(n => n.Middle).HasColumnName("Name_Middle");
                name.Property(n => n.Last).HasColumnName("Name_Last");
                name.Property(n => n.UserPreferred).HasColumnName("Name_UserPreferred");
                name.Property(n => n.Native).HasColumnName("Name_Native");
            });

            entity.Property(e => e.Gender).HasColumnName("Gender");

            entity.Property(e => e.Age).HasColumnName("Age");

            entity.OwnsOne(e => e.DateOfBirth, dob =>
            {
                dob.Property(d => d.month).HasColumnName("DateOfBirth_month");
                dob.Property(d => d.day).HasColumnName("DateOfBirth_day");
                dob.Property(d => d.year).HasColumnName("DateOfBirth_year");
            });

            entity.Property(e => e.Series).HasColumnName("Series");

            entity.OwnsOne(e => e.Media, media =>
            {
                media.Ignore(m => m.nodes);
            });

            entity.Property(e => e.ImageURL).HasColumnName("ImageURL");

            entity.Property(e => e.Rarity).HasColumnName("Rarity");

            entity.Property(e => e.XpValue).HasColumnName("XpValue");

            entity.Property(e => e.Favorites).HasColumnName("Favorites");

            entity.Property(e => e.SeriesId).HasColumnName("SeriesId");

            entity.Property(e => e.IsAdult).HasColumnName("IsAdult");
        });

        base.OnModelCreating(modelBuilder);
    }
}
