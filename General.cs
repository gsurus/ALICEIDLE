using ALICEIDLE.Gelbooru;
using ALICEIDLE.Logging;
using ALICEIDLE.Logic;
using ALICEIDLE.Services;
using Discord;
using Discord.Interactions;
using MySqlConnector;
using Newtonsoft.Json;
using System.Text.Json;


namespace ALICEIDLE
{
    public class General : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("roll", "Go out and catch some waifus")]
        public async Task CatchWaifu()
        {
            var builder = ComponentHandler.BuildComponent("catching");
            await RespondAsync(embed: EmbedHandler.BuildEmbed("catching", Context.User.Username, Context.User.Id).Result.Build(), components: ComponentHandler.BuildComponent("catching").Build());
        }
        [RequireOwner]
        [SlashCommand("osutest", "desc")]
        public async Task OsuTest(string dir)
        {
            OsuFile file = await OsuHelper.DeserializeOsuFile(dir);
            await OsuHelper.GetBeatmapStatistics(file);
        }
        [SlashCommand("search", "search for a character based on name or ID")]
        public async Task Search(string name = "", int id = -1)
        {
            Waifu waifu = null;
            if (name == "" & id == -1)
                id = 1;
            if (name != "")
                waifu = await SqlDBHandler.QueryWaifuByName(name);
            else if (id != -1)
                waifu = await SqlDBHandler.QueryWaifuById(id);
            
            EmbedBuilder emb = EmbedHandler.CreateDetailedEmbedContent(waifu);
            await RespondAsync(embed: emb.Build());
        }
        
        [SlashCommand("gelbooru", "Search Gelbooru for an image")]
        public async Task Testing(string tag)
        {
            User user = null;
            UserData userData = new UserData()
            {
                Users = new List<User>()
            };

            if (EmbedHandler.userData == null)
            {
                user = new User()
                {
                    Username = Context.User.Username,
                    Posts = new Gelbooru.Root()
                    {
                        post = new List<Post>(),
                        attributes = new Attributes()
                    },
                    Page = 0
                };
                
                userData.Users.Add(user);
                EmbedHandler.userData = userData;
            }

            user = EmbedHandler.userData.Users.Find(d => d.Username == Context.User.Username);
            var channel = Context.Client.GetChannel(Context.Channel.Id) as ITextChannel;
            
            if (!channel.IsNsfw)
                tag = "rating:general " + tag;
            
            Gelbooru.Root test = Gelbooru.Gelbooru.SearchPosts(tag).Result;
            user.Posts = test;

            await RespondAsync(embed: EmbedHandler.GelEmbedBuilder(Context.User.Username, "gelbooru").Result.Build(), components: ComponentHandler.BuildComponent("gelbooru").Build());
        }

        [SlashCommand("preference", "The gender of characters you'd prefer to roll")]
        public async Task GenderPreference([Choice("Male", "male"), Choice("Female", "female"), Choice("None", "none")] string preference)
        {
            PlayerData playerData = await SqlDBHandler.RetrievePlayerData(Context.User.Id);
            playerData.GenderPreference = preference;
            
            await SqlDBHandler.UpdatePlayerData(playerData);

            if (EmbedHandler.playerDictionary.ContainsKey(playerData.Id))
                EmbedHandler.playerDictionary[playerData.Id] = playerData;

            await RespondAsync(embed: new EmbedBuilder().WithTitle("Preference Modified").WithColor(EmbedColors.successColor).Build(), ephemeral: true);
        }
        [SlashCommand("title_preference", "Set the preferred display language of anime titles.")]
        public async Task TitleLanguagePreference([Choice("Native", "Native"), Choice("Romanji", "Romanji"), Choice("English", "English")] string preference)
        {
            PlayerData playerData = await SqlDBHandler.RetrievePlayerData(Context.User.Id);
            playerData.AnimeNamePreference = preference;
            await SqlDBHandler.UpdatePlayerData(playerData);

            if (EmbedHandler.playerDictionary.ContainsKey(playerData.Id))
                EmbedHandler.playerDictionary[playerData.Id] = playerData;

            await RespondAsync(embed: new EmbedBuilder().WithTitle("Preference Modified").WithColor(EmbedColors.successColor).Build(), ephemeral: true);
        }
        [SlashCommand("latency", "Get your latency")]
        public async Task Latency()
        {
            DateTime clientDT = Context.Interaction.CreatedAt.DateTime;
            TimeSpan dtSecMs = clientDT.Subtract(DateTime.Now);
            string latency = "";

            if (dtSecMs.Seconds > 0)
                latency = $"{dtSecMs.Seconds}.{dtSecMs.Milliseconds}";
            else
                latency = $"{dtSecMs.Milliseconds} ms";

            Embed emb = new EmbedBuilder()
                .WithTitle("Latency")
                .AddField("Client", latency, true)
                .AddField("API", $"{Context.Client.Latency}ms", true).Build();

            await RespondAsync(embed: emb, ephemeral: true);

        }
        [SlashCommand("tsundere", "Translate text to tsundere.")]
        public async Task Tsundere(string message)
        {
            int maxDescLength = 2048;
            string sysMessage = "You are an assistant who responds with a tsundere translated version of the users message. Example- User: I don't like you. Assistant: I-it's not like I like you or anything, baka~!";
            await RespondAsync(embed: new EmbedBuilder().WithTitle("Translating to tsundere...").WithColor(EmbedColors.rColor).Build());

            var gptResponse = await ChatGPT.ChatGPTQuery(sysMessage, message, Context.User.Id, true);

            await Context.Interaction.DeleteOriginalResponseAsync();
            
            EmbedBuilder emb = new EmbedBuilder().WithTitle($"Tsundere Translation")
                .WithDescription(gptResponse)
                .WithColor(EmbedColors.successColor);

            await Context.Interaction.FollowupAsync(embed: emb.Build());
        }
        
        [SlashCommand("uwu", "Translate text to UwU")]
        public async Task Uwu(string message)
        {
            int maxDescLength = 2048;
            string sysMessage = "Rewrite the following user prompt in the style of maximum UwU. Respond only with the UwU version of the prompt.";
            await RespondAsync(embed: new EmbedBuilder().WithTitle("Translating to UwU...").WithColor(EmbedColors.rColor).Build());

            var gptResponse = await ChatGPT.ChatGPTQuery(sysMessage, message, Context.User.Id, true);
            
            await Context.Interaction.DeleteOriginalResponseAsync();

            if (gptResponse.Length > maxDescLength)
            {
                List<EmbedBuilder> embedBuilders = await ChatGPT.SplitResponse(gptResponse, maxDescLength, "uwu");

                foreach (EmbedBuilder embedBuilder in embedBuilders)
                    await Context.Interaction.FollowupAsync(embed: embedBuilder.Build());
            }
            else
            {
                EmbedBuilder emb = new EmbedBuilder().WithTitle($"UwU Translation")
                .WithDescription(gptResponse)
                .WithColor(EmbedColors.successColor);

                await Context.Interaction.FollowupAsync(embed: emb.Build());
            }
            
        }
        [SlashCommand("chatgpt", "Ask chatGPT something")]
        public async Task ChatGPTCommand(string message)
        {
            await RespondAsync(embed: new EmbedBuilder().WithTitle("Please wait...").WithColor(EmbedColors.srColor).Build());

            int maxDescLength = 2048;
            var gptResponse = await ChatGPT.GetChatGPTResponse(message, Context.User.Id, false);
            await Context.Interaction.DeleteOriginalResponseAsync();
            if (gptResponse.Length > maxDescLength)
            {
                List<EmbedBuilder> embedBuilders = await ChatGPT.SplitResponse(gptResponse, maxDescLength, "chat");

                foreach (EmbedBuilder embedBuilder in embedBuilders)
                    await Context.Interaction.FollowupAsync(embed: embedBuilder.Build());
            }
            else
            {
                Embed emb = new EmbedBuilder()
                    .WithTitle($"ChatGPT Response")
                    .WithDescription(gptResponse)
                    .WithColor(EmbedColors.successColor).Build();

                await Context.Interaction.FollowupAsync(embed: emb);
            }

        }

        [SlashCommand("audio_transcription", "Create a transcription from an audio file.")]
        public async Task ChatGPTTranscription(IAttachment audioFile)
        {
            await RespondAsync("Please wait...");
            var response = await ChatGPT.GetWhisperResponse(audioFile, false);

            Embed emb = new EmbedBuilder()
            .WithTitle($"Transcription")
            .WithDescription(response)
            .WithColor(EmbedColors.successColor).Build();
            
            await Context.Interaction.DeleteOriginalResponseAsync();
            await Context.Interaction.FollowupAsync(embed: emb);
        }

        [SlashCommand("audio_translation", "Create a translation from an audio file.")]
        public async Task ChatGPTTranslation(IAttachment audioFile)
        {
            await RespondAsync("Please wait...");
            var response = await ChatGPT.GetWhisperResponse(audioFile, true);

            Embed emb = new EmbedBuilder()
            .WithTitle($"Translation")
            .WithDescription(response)
            .WithColor(EmbedColors.successColor).Build();
            
            await Context.Interaction.DeleteOriginalResponseAsync();
            await Context.Interaction.FollowupAsync(embed: emb);
        }

        [RequireOwner]
        [SlashCommand("remove", "test")]
        public async Task Remove()
        {
            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(50).FlattenAsync();
            List<IMessage> botMessages = new List<IMessage>();
            
            foreach (var message in messages)
            {

                if (message.Author.IsBot)
                    botMessages.Add(message);
            }
            
            var filteredMessages = botMessages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14);
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(filteredMessages);
        }
        /*
       [RequireOwner]
       [SlashCommand("buildlist", "testing")]
       public async Task BuildList()
       {
           // AniList API v2 endpoint to query for characters
           string apiUrl = "https://graphql.anilist.co";

           // Query string to get characters sorted by number of favorites
           string query = AnilistQuery.buildListSearchString;
           Page page = new Page();
           List<Character> _characters = new List<Character>();
           DateTime startTime = DateTime.Now;
           Console.WriteLine("");
           for (int i = 1; i <= 1000; i++)
           {
               // Variables to pass to the query
               var variables = new
               {
                   perPage = 50,
                   page = i
               };

               // Create a new HttpClient instance
               using var httpClient = new HttpClient();

               // Create a new HttpRequestMessage with the query and variables
               var httpRequestMessage = new HttpRequestMessage
               {
                   Method = HttpMethod.Post,
                   RequestUri = new Uri(apiUrl),
                   Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
                   {
                       query,
                       variables
                   }))
               };
               httpRequestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

               // Send the request and deserialize the response JSON into a list of AniListCharacter objects
               var response = await httpClient.SendAsync(httpRequestMessage);
               var content = await response.Content.ReadAsStringAsync();
               Root aniListResponse = JsonConvert.DeserializeObject<Root>(content);

               List<string> charList = new List<string>();
               foreach (var character in aniListResponse.Data.Page.Characters)
               {
                   _characters.Add(character);
               }

               Console.Write($"\r{_characters.Count()} characters added in {Calculations.TimeElapsed(startTime)}");
               if (i % 20 == 0)
                   Thread.Sleep(5000);
               else
                   Thread.Sleep(100);
           }
           page.Characters = _characters;
           var options = new JsonSerializerOptions { WriteIndented = true };
           var jsonString = System.Text.Json.JsonSerializer.Serialize(page, options);

           File.WriteAllText(@"E:\Visual Studio 2017\Projects\ALICEIDLE\bin\Debug\net7.0\characters.json", jsonString);
       }

       [RequireOwner]
       [SlashCommand("buildwaifulist", "testing")]
       public async Task BuildWaifuList()
       {
           // If the file is not empty, deserialize the contents of a list of PlayerData objects
           List<Waifu> waifus = new List<Waifu>();

           List<Character> characters = WaifuHandler.characterList;
           Console.WriteLine(characters.First().Name.Full);

           foreach (var character in characters)
           {
               var media = character.Media.nodes.FirstOrDefault();

               Waifu waifu = new Waifu();
               waifu.Name = character.Name;
               waifu.DateOfBirth = new DateOfBirth();
               waifu.Gender = character.Gender;
               waifu.Media = character.Media;
               if (media == null)
               {
                   waifu.Series = null;
                   waifu.SeriesId = null;
                   waifu.IsAdult = null;
               }
               else
               {
                   waifu.Series = media.Title.UserPreferred;
                   waifu.SeriesId = media.Id;
                   waifu.IsAdult = media.IsAdult;
               }
               waifu.Favorites = character.Favourites;
               waifu.ImageURL = character.Image.Large;
               waifu.Rarity = WaifuHandler.CalculateRarity(character.Favourites);
               waifu.XpValue = WaifuHandler.CalculateXPValue(character.Favourites, WaifuHandler.CalculateRarity(character.Favourites));
               waifu.Id = character.Id;


               if (character.Gender == null)
                   character.Gender = "unknown";
               if (character.DateOfBirth.month != null)
                   waifu.DateOfBirth.month = character.DateOfBirth.month;   
               if (character.DateOfBirth.day != null)
                   waifu.DateOfBirth.day = character.DateOfBirth.day;
               if (character.Age != null)
                   waifu.Age = character.Age;
               waifus.Add(waifu);
           }

           Console.WriteLine(waifus.Count());
           var options = new JsonSerializerOptions { WriteIndented = true };
           var jsonString = System.Text.Json.JsonSerializer.Serialize(waifus, options); 
           File.WriteAllText(@"E:\Visual Studio 2017\Projects\ALICEIDLE\bin\Debug\net7.0\waifus.json", jsonString);
       }

       [RequireOwner]
       [SlashCommand("mariadb", "temp")]
       public async Task MarDB()
       {
           PlayerData data = await WaifuHandler.RetrievePlayerDataByID(Context.User.Id, Context.User.Username);
           await SqlDBHandler.InsertPlayerData(Context.User.Username, Context.User.Id);
           Console.WriteLine(data.Name);
       }
      
        [RequireOwner]
        [SlashCommand("mariaupdate", "temp")]
        public async Task MariaUpdate()
        {
            PlayerData data = await SqlDBHandler.RetrievePlayerData(Context.User.Id);
            foreach (var id in data.RollHistory)
            {
                Console.WriteLine(id);
            }
           
        }

                
        [RequireOwner]
        [SlashCommand("mariainsert", "temp")]
        public async Task MariaInsert(IAttachment audioFile)
        {
            var response = await ChatGPT.GetWhisperResponse(audioFile, true);
            Console.WriteLine(response);
        }
        
        [RequireOwner]
        [SlashCommand("query", "temp")]
        public async Task Query()
        {
            string connectionString = Program._config["SQLConnectionString"];
            MySqlConnection connection = new MySqlConnection(connectionString);

            List<Waifu> data = JsonConvert.DeserializeObject<List<Waifu>>(File.ReadAllText(@"E:\Visual Studio 2017\Projects\ALICEIDLE\bin\Debug\net7.0\waifus.json"));
            data = data.OrderByDescending(p => p.Favorites).ToList();

            var properties = typeof(Waifu).GetProperties();
            var columns = properties.Select(p => new { Name = p.Name, DataType = SqlDBHandler.GetSqlDataType(p.PropertyType) }).ToList();

            Console.WriteLine($"Field Names: {columns.Count()}\nData Types: {columns.Count()}");
            // Create a new table in the database using the extracted field names and data types
            using (connection = new MySqlConnection(connectionString))
            {
                Console.WriteLine("Starting");

                int batchSize = 1000;
                await connection.OpenAsync();
                string tableName = "mytable";
                string createTableQuery = "CREATE TABLE " + tableName + " (";

                for (int i = 0; i < columns.Count; i++)
                {
                    Console.WriteLine($"i: {i}");

                    string columnName = columns[i].Name;
                    string sqlDataType = columns[i].DataType;
                    createTableQuery += columnName + " " + sqlDataType + ",";

                }

                createTableQuery = createTableQuery.TrimEnd(',') + ")";
                using (MySqlCommand command = new MySqlCommand(createTableQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                // Insert the data into the new table using a parameterized SQL query
                string insertQuery = "INSERT INTO " + tableName + " (";
                foreach (var column in columns)
                {
                    insertQuery += column.Name + ",";
                }

                insertQuery = insertQuery.TrimEnd(',') + ") VALUES (";
                for (int i = 0; i < columns.Count; i++)
                {
                    insertQuery += "@" + i.ToString() + ",";
                }

                insertQuery = insertQuery.TrimEnd(',') + ")";
                Console.WriteLine(insertQuery);
                using (MySqlCommand command = new MySqlCommand(insertQuery, connection))
                {
                    // Execute the insert query for each batch of rows
                    for (int batchStart = 0; batchStart < data.Count; batchStart += batchSize)
                    {
                        int batchEnd = Math.Min(batchStart + batchSize, data.Count);
                        for (int rowIndex = batchStart; rowIndex < batchEnd; rowIndex++)
                        {
                            Waifu row = data[rowIndex];
                            for (int i = 0; i < columns.Count; i++)
                            {
                                string columnName = columns[i].Name;
                                object columnValue = properties.Single(p => p.Name == columnName).GetValue(row);
                                command.Parameters.AddWithValue("@" + i.ToString(), columnValue ?? DBNull.Value);
                            }
                            await command.ExecuteNonQueryAsync();
                            command.Parameters.Clear();
                        }
                    }
                }
            }

            Console.WriteLine("Done");
        }
         */

        public class CatchWaifusAutoCompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                // Create a collection with suggestions for autocomplete
                IEnumerable<AutocompleteResult> results = new[]
                {
                    new AutocompleteResult("Name1", "value111"),
                    new AutocompleteResult("Name2", "value2")
                };

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }
            
        public class MyAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                // Create a collection with suggestions for autocomplete
                IEnumerable<AutocompleteResult> results = new[]
                {
                    new AutocompleteResult("character", "Spike Spiegel"),
                    new AutocompleteResult("anime", "Cowboy Beebop")
                };

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }
    }
}
