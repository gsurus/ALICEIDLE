using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ALICEIDLE.Services;
using MySqlConnector;
using cConsole = Colorful.Console;
using ALICEIDLE.Logging;

namespace ALICEIDLE
{
    class Program
    {
        // setup our fields we assign later
        public static IConfiguration _config { get; set; }
        public static string basePath { get; set; }
        private DiscordSocketClient _client;
        private InteractionService _commands;
        public static ulong[] _serverIds = new ulong[] { 631818882901868574, 642655184152952833, 891524758263431188 }; 
        public static Colorful.StyleSheet stySheet { get; set; }

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync(string[] args)
        {
            
        }

        public Program()
        {
            basePath = Directory.Exists("/Data/") ? "/Data/" : Path.Join(AppContext.BaseDirectory, "Data");

            //Console.WriteLine(basePath);
            // create the configuration
            var _builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(path: "config.json");
            
            // build the configuration and assign to _config          
            _config = _builder.Build();
        }

        public async Task MainAsync()
        {
            stySheet = await ConsoleHelper.GetSysLogStyleSheet();
            // call ConfigureServices to create the ServiceCollection/Provider for passing around the services
            using (var services = ConfigureServices())
            {
                // get the client and assign to client 
                // you get the services via GetRequiredService<T>
                var client = services.GetRequiredService<DiscordSocketClient>();
                var commands = services.GetRequiredService<InteractionService>();
                _client = client;
                _commands = commands;

                // setup logging and the ready event
                client.Log += LogAsync;
                commands.Log += LogAsync;
                client.MessageReceived += LogMessageAsync;
                client.ButtonExecuted += ButtonHandler.MyButtonHandler;
                client.Ready += ReadyAsync;
                client.GuildScheduledEventStarted += GuildScheduledEventStartedAsync;
                client.PresenceUpdated += PresenceUpdated;
                client.InviteCreated += Client_InviteCreated;

                // this is where we get the Token value from the configuration file, and start the bot
                await client.LoginAsync(TokenType.Bot, _config["Token"]);
                await client.StartAsync();

                // we get the CommandHandler class here and call the InitializeAsync method to start things up for the CommandHandler service
                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                await Task.Delay(Timeout.Infinite);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            if (log.ToString().Contains("Connected"))
                ConsoleHelper.WriteConnectedAsString();
            else
                cConsole.WriteLineStyled(log.ToString(), stySheet);
            return Task.CompletedTask;
        }
        private Task LogMessageAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return Task.CompletedTask;
            else
                ConsoleHelper.WriteLogMessage(message);
                LogHelper.AddServerMessage(message);
                return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            if (IsDebug())
                await AddCommandsToServers();
            else
                // this method will add commands globally, but can take around an hour
                await _commands.RegisterCommandsGloballyAsync(true);
            
            string connectionString = SqlDBHandler.connectionString;
            SqlDBHandler.connection = new MySqlConnection(connectionString);
        }

        private async Task GuildScheduledEventStartedAsync(SocketGuildEvent gEvent)
        {
            cConsole.WriteLineStyled($"GuildScheduledEventStartedAsync: {gEvent.Name}", stySheet);
        }
        
        private Task Client_InviteCreated(SocketInvite arg)
        {
            return Task.CompletedTask;
        }
        
        private Task PresenceUpdated(SocketUser usr, SocketPresence presc, SocketPresence _presc)
        {
            return Task.CompletedTask;
        }
        
        // this method handles the ServiceCollection creation/configuration, and builds out the service provider we can call on later
        private ServiceProvider ConfigureServices()
        {
            // this returns a ServiceProvider that is used later to call for those services
            // we can add types we have access to here, hence adding the new using statement:
            // using csharpi.Services;
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig{ GatewayIntents = GatewayIntents.All }))
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

        private async Task AddCommandsToServers()
        {
            // this is where you put the id of the test discord guild
            //await _commands.RegisterCommandsToGuildAsync(642655184152952833, true);
            foreach (var id in _serverIds)
            {
                //cConsole.WriteLineStyled($"In debug mode, adding commands to {id}", stySheet);
                await _commands.RegisterCommandsToGuildAsync(id, true);
            }
        }
    }
}