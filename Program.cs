using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ALICEIDLE.Services;
using ALICEIDLE.Logic;
using MySqlConnector;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ALICEIDLE
{
    class Program
    {
        // setup our fields we assign later
        public static IConfiguration _config { get; set; }
        private DiscordSocketClient _client;
        private InteractionService _commands;
        private ulong _privGuildId;
        private ulong _squadronGuildId;


        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync(string[] args)
        {
            
        }

        public Program()
        {
            // create the configuration
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");
            
            // build the configuration and assign to _config          
            _config = _builder.Build();
            _privGuildId = ulong.Parse(_config["PrivGuildId"]);
            _squadronGuildId = ulong.Parse(_config["SquadronGuildId"]);
        }

        public async Task MainAsync()
        {
            
            //ALICEIDLE.Logic.Values.GenerateCharacterList();
            // call ConfigureServices to create the ServiceCollection/Provider for passing around the services
            using (var services = ConfigureServices())
            {
                //EmbedHandler.userData = new ALICEIDLE.Gelbooru.UserData();
                //EmbedHandler.userData.Users = new List<ALICEIDLE.Gelbooru.User>();
                // get the client and assign to client 
                // you get the services via GetRequiredService<T>
                var client = services.GetRequiredService<DiscordSocketClient>();
                var commands = services.GetRequiredService<InteractionService>();
                _client = client;
                _commands = commands;

                // setup logging and the ready event
                client.Log += LogAsync;
                commands.Log += LogAsync;
                client.ButtonExecuted += ButtonHandler.MyButtonHandler;
                client.Ready += ReadyAsync;
                client.GuildScheduledEventStarted += GuildScheduledEventStartedAsync;
                client.InviteCreated += Client_InviteCreated;

                // this is where we get the Token value from the configuration file, and start the bot
                await client.LoginAsync(TokenType.Bot, _config["Token"]);
                await client.StartAsync();

                // we get the CommandHandler class here and call the InitializeAsync method to start things up for the CommandHandler service
                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                await Task.Delay(Timeout.Infinite);
            }
        }

        private Task Client_InviteCreated(SocketInvite arg)
        {
            throw new NotImplementedException();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            if (IsDebug())
            {
                // this is where you put the id of the test discord guild
                Console.WriteLine($"In debug mode, adding commands to {_squadronGuildId}...");
                Console.WriteLine($"In debug mode, adding commands to {_privGuildId}...");
                await _commands.RegisterCommandsToGuildAsync(_squadronGuildId);
                await _commands.RegisterCommandsToGuildAsync(_privGuildId);
                await _commands.RegisterCommandsToGuildAsync(631818882901868574);

            }
            else
            {
                // this method will add commands globally, but can take around an hour
                await _commands.RegisterCommandsGloballyAsync(true);
            }
            Console.WriteLine($"Connected as -> [{_client.CurrentUser}] :)");
            //Values.GenerateWaifuList();
            string connectionString = SqlDBHandler.connectionString;
            SqlDBHandler.connection = new MySqlConnection(connectionString);
            
            

            //await Values.connection.OpenAsync();
        }
        private async Task GuildScheduledEventStartedAsync(SocketGuildEvent gEvent)
        {
            Console.WriteLine($"GuildScheduledEventStartedAsync: {gEvent.Name}");
        }
        // this method handles the ServiceCollection creation/configuration, and builds out the service provider we can call on later
        private ServiceProvider ConfigureServices()
        {
            // this returns a ServiceProvider that is used later to call for those services
            // we can add types we have access to here, hence adding the new using statement:
            // using csharpi.Services;
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }

        static bool IsDebug()
        {
            #if DEBUG
            return true;
            #else
            return false;
            #endif
        }
    }
}