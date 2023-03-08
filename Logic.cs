using ALICEIDLE;
using Discord;
using Newtonsoft.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ALICEIDLE.Services;

namespace ALICEIDLE.Logic
{
    public class Values
    {
        public static List<Character> characterList { get; set; }
        public static List<Waifu> waifuList { get; set; }
        public static List<double> weights { get; set; }
        public static bool weightsGenerated { get; set; } = false;

        public static Color ssrColor = new Color(255, 66, 126);
        public static Color srColor = new Color(233, 66, 255);
        public static Color rColor = new Color(69, 66, 255);
        public static Color nColor = new Color(158, 221, 255);
        public static Color successColor = new Color(92, 184, 92);
        public static Color errorColor = new Color(184, 92, 92);
       

        public static async Task <Waifu> CatchWaifu(PlayerData player)
        {
            //Console.WriteLine(player.OwnedWaifus[0].Item1);
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
            var selectedChr = await GetRandomWaifuByTier(rollResult, player);
            //Console.WriteLine($"Selected Character: {selectedChr.Name.Full}");
            duplicate = await IsDuplicateRoll(selectedChr, player.RollHistory);
            while (duplicate)
            {
                Console.WriteLine($"Rolled Duplicate: {selectedChr.Name.Full} (id){selectedChr.Id}");
                rollResult = RollGacha(itemProbabilities, pityRateRolls, pityRateIncrease, player);
                selectedChr = await GetRandomWaifuByTier(rollResult, player);
                duplicate = await IsDuplicateRoll(selectedChr, player.RollHistory.Take(20).ToList());
            }

            string rarity = CalculateRarity(selectedChr.Favorites);
            int xpValue = CalculateXPValue(selectedChr.Favorites, rarity);


            player.WaifuAmount++;
            player.Xp += xpValue;
            player.Level = CalculateLevel(player.Xp);
            player.LastCharacterRolled = selectedChr.Id;
            if (player.RollHistory.Count >= 100)
                player.RollHistory.RemoveAt(0);
            player.RollHistory.Add(selectedChr.Id);
            player.TotalRolls++;
            
            await SqlDBHandler.UpdatePlayerData(player);

            return selectedChr;
        }
        public static async Task<bool> IsDuplicateRoll(Waifu character, List<int> rollHistory)
        {
            for (int i = 0; i < rollHistory.Count; ++i)
                if (rollHistory[i] == character.Id)
                {
                    Console.WriteLine("dupe");
                    return true;
                } 
            return false;
        }
        public static async Task<bool> IsDuplicateFavorite(List<Waifu> favorites, Waifu waifu)
        {
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
                waifus = SqlDBHandler.QueryWaifuByIds(ids).Result;
            
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
                Series = character.Media.nodes.FirstOrDefault().Title.UserPreferred,
                Media = character.Media,
                ImageURL = character.Image.Large,
                Rarity = CalculateRarity(character.Favourites),
                XpValue = CalculateXPValue(character.Favourites, CalculateRarity(character.Favourites)),
                Id = character.Id,
                SeriesId = character.Media.nodes.FirstOrDefault().Id,
                IsAdult = character.Media.nodes.FirstOrDefault().IsAdult
            };
            if (character.Gender == null)
            {
                character.Gender = "unknown";
            }
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

            Waifu _waifu = SqlDBHandler.QueryWaifuByName(name).Result;
            if(player.OwnedWaifus.Count > 0)
            {
                if (await IsDuplicateFavorite(await IdListToWaifuList(favoriteIds), _waifu))
                {
                    Console.WriteLine("Dupe");
                    return null;
                }
            }
            player.OwnedWaifus.Add(new Tuple<int, int>(_waifu.Id, 0));
            await SqlDBHandler.UpdatePlayerData(player);
            
            return _waifu;
        }
        public static async Task RemoveWaifu(PlayerData player, int waifuId)
        {
            Console.WriteLine(player.OwnedWaifus.Count);
            foreach (var tuple in player.OwnedWaifus)
            {
                if (tuple.Item1 == waifuId)
                    player.OwnedWaifus.RemoveAt(player.OwnedWaifus.IndexOf(tuple));
            }
            Console.WriteLine(player.OwnedWaifus.Count);
        }
        public static async Task HistoryFavorite(PlayerData player)
        {
            List<int> favoriteIds = FavoritesToIdList(player.OwnedWaifus);
            Waifu waifu = await SqlDBHandler.QueryWaifuById(player.CurrentWaifu);
            if (await IsDuplicateFavorite(await IdListToWaifuList(favoriteIds), waifu))
                return;
            else
            {
                player.OwnedWaifus.Add(new Tuple<int, int>(waifu.Id, 0));
                await SqlDBHandler.UpdatePlayerData(player);
            }

        }
        public static List<int> FavoritesToIdList(List<Tuple<int, int>> favorites)
        {
            List<int> ids = new List<int>();
            foreach (var favorite in favorites)
            {
                ids.Add(favorite.Item1);
            }
            return ids;
        }
        public static async Task<Waifu> GetCharacterByID(int id)
        {

            //Console.WriteLine(waifuList.Count());
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
                {
                    names.Add(name);
                }
            }
                
            string closestMatch = FindClosestName(searchName, names, 6);
            Waifu charMatch = waifuList.FirstOrDefault(c => c.Name.Full.ToLower() == closestMatch.ToLower());
            
            if (charMatch == null)
            {
                foreach (var waifu in waifuList)
                {
                    foreach (var name in waifu.Name.Alternative)
                    {
                        if (name == closestMatch)
                            charMatch = waifu;
                    }
                }
                
            }
            
            return charMatch;
        }
        static async Task<Waifu> GetRandomWaifuByTier(int rarity, PlayerData player)
        {
            return SqlDBHandler.QueryWaifuByTier(rarity, player.GenderPreference).Result;
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
                    return nColor;
                    break;
                case "Rare":
                    return rColor;
                    break;
                case "Super Rare":
                    return srColor;
                    break;
                case "Super Super Rare":
                    return ssrColor;
                    break;
            }
            return ssrColor;
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
                {
                    itemProbabilities[j] += pityRateIncrease * (player.RollsSinceLastSSR * 0.01);
                }
                Console.WriteLine($"Pity Rate: N {itemProbabilities[0].ToString("#.0000")} | R {itemProbabilities[1].ToString("#.0000")} | SR {itemProbabilities[2].ToString("#.0000")} | SSR {itemProbabilities[3].ToString("#.0000")}");
                
            }


            // No pity rate - loop through the item probabilities and determine which item was rolled
            double cumulativeProbability = 0;
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
            {
                File.Create("waifu_data.json").Close();
            }

            // Read the contents of the file
            string fileContents = File.ReadAllText("waifu_data.json");

            // If the file is not empty, deserialize the contents of a list of PlayerData objects
            List<PlayerData> waifuDataList = new List<PlayerData>();
            if (fileContents != "")
            {
                waifuDataList = JsonConvert.DeserializeObject<List<PlayerData>>(fileContents);
            }
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
            {
                File.Create("waifu_data.json").Close();
            }

            // Read the contents of the file
            string fileContents = File.ReadAllText("waifu_data.json");

            // If the file is not empty, deserialize the contents doubleo a list of PlayerData objects
            List<PlayerData> waifuDataList = new List<PlayerData>();
            if (fileContents != "")
            {
                waifuDataList = JsonConvert.DeserializeObject<List<PlayerData>>(fileContents);
            }
            return waifuDataList.OrderBy(p => p.Xp).Reverse().ToList();
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

        static void WeightedList()
        {
            var _weights = new List<double>();
            var _characterList = characterList.OrderBy(p => p.Favourites).Reverse().ToList();
            int[] rankLimit = { 50, 250, 500, 1000 };
            double[] rankWeight = { 0.005, 0.01, 0.05, 0.2 };
            
            Console.WriteLine($"{_characterList[50].Favourites}|{_characterList[250].Favourites}|{_characterList[500].Favourites}|{_characterList[1000].Favourites}");
            
            // assign weights to each character based on their popularity
            for (int i = 0; i < _characterList.Count; i++)
            {
                int rank = i + 1;

                for (int j = 0; j < rankLimit.Length; j++)
                {
                    if (rank <= rankLimit[j])
                    {
                        _weights.Add(rankWeight[j]);
                        break;
                    }
                }
            }

            double totalWeight = _weights.Sum();
            for (int i = 0; i < _weights.Count; i++)
            {
                _weights[i] = _weights[i] / totalWeight;
            }

            weightsGenerated = true;
            weights = _weights;
        }
        static int WeightedRandom(List<double> weights, Random random)
        {
            // calculate the total weight
            double totalWeight = weights.Sum();

            // select a random number between 0 and the total weight
            double randomValue = random.NextDouble() * totalWeight;

            // find the index of the selected item based on the weights
            for (int i = 0; i < weights.Count; i++)
            {
                if (randomValue < weights[i])
                {
                    return i;
                }

                randomValue -= weights[i];
            }

            // this should never happen, but just in case...
            return weights.Count - 1;
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
            {
                d[i, 0] = i;
            }

            for (int j = 0; j <= t.Length; j++)
            {
                d[0, j] = j;
            }

            for (int j = 1; j <= t.Length; j++)
            {
                for (int i = 1; i <= s.Length; i++)
                {
                    if (s[i - 1] == t[j - 1])
                    {
                        d[i, j] = d[i - 1, j - 1];
                    }
                    else
                    {
                        d[i, j] = Math.Min(d[i - 1, j] + 1, Math.Min(d[i, j - 1] + 1, d[i - 1, j - 1] + 1));
                    }
                }
            }

            return d[s.Length, t.Length];
        }
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

