using AlBot.Database;
using AlBot.Database.Mongo;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AlBot
{
    public class Program
    {
        private static ILogger<Program> logger;
        private static IServiceProvider _services;
        public static IServiceProvider Services
        {
            get
            {
                return _services;
            }
            private set
            {
                _services = value;
            }
        }

        private static IConfiguration _config;

        public static void Main( string[] args )
        {
            Program.Run().GetAwaiter().GetResult();
        }

        public static async Task Run()
        {
            var configuration = _config = LoadConfiguration();

            if( string.IsNullOrEmpty( configuration.GetValue<string>( "BotToken" ) ) )
            {
                Console.WriteLine( "No BotToken configured! Stopping..." );
                return;
            }

            var services = Services = ConfigureServices( configuration );
            logger = services.GetRequiredService<ILogger<Program>>();
            var discordClient = services.GetRequiredService<DiscordSocketClient>();

            discordClient.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;
            services.GetRequiredService<CommandService>().CommandExecuted += OnCommandExecuted;

            await Connect();
            discordClient.Disconnected += DiscordClient_Disconnected;
            discordClient.LoggedOut += OnLoggedOut;

            services.GetRequiredService<CommandHandler>();
            services.GetRequiredService<InteractiveService>();

            Console.WriteLine( "Connected!" );
            await Task.Delay( -1 );
        }

        private static async Task OnLoggedOut()
        {
            logger.LogWarning( "WARNING! Disconnected, automatically attempting reconnect..." );
            Environment.Exit( 1 );
            /*await Connect();
            Console.WriteLine( "Reconnected!" );*/
        }

        private static async Task OnCommandExecuted( CommandInfo command, ICommandContext context, IResult result )
        {
            
        }

        private static async Task DiscordClient_Disconnected( Exception e )
        {
            logger.LogWarning( "WARNING! Disconnected, automatically attempting reconnect...", e );
            Environment.Exit( 1 );
            /*await Connect();
            Console.WriteLine( "Reconnected!" );*/
        }

        private static async Task Connect()
        {
            var discordClient = Services.GetRequiredService<DiscordSocketClient>();
            await discordClient.LoginAsync( Discord.TokenType.Bot, _config.GetValue<string>( "BotToken" ) );
            await discordClient.StartAsync();
        }

        private static IServiceProvider ConfigureServices( IConfiguration config )
        {
            var mongoString = $"mongodb://{config[ "DbUser" ]}:{config[ "DbPass" ]}@{config[ "DbIp" ]}:{config.GetValue<int>( "DbPort" )}/{config[ "DbUser" ]}";
            Hangfire.GlobalConfiguration.Configuration.UseMongoStorage( mongoString, config[ "DbName" ] );
            Hangfire.GlobalConfiguration.Configuration.UseColouredConsoleLogProvider();
            Hangfire.GlobalConfiguration.Configuration.UseDefaultActivator();

            var result = new ServiceCollection()
                .AddLogging( builder => builder.AddFile( "logs\\log.txt" ).SetMinimumLevel( System.Diagnostics.Debugger.IsAttached ? LogLevel.Trace : LogLevel.Information ) )
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<IDatabaseFactory, MongoDatabaseFactory>()
                .AddSingleton<IDatabase, MongoDatabase>( services => services.GetRequiredService<IDatabaseFactory>().Create( config[ "DbName" ], config[ "DbUser" ], config[ "DbPass" ], config[ "DbIp" ], config.GetValue<int>( "DbPort" ) ) as MongoDatabase )
                .AddSingleton( new BackgroundJobServer() )
                .BuildServiceProvider();

            Hangfire.Logging.LogProvider.SetCurrentLogProvider( new Hangfire.Logging.LogProviders.ColouredConsoleLogProvider() );

#if DEBUG
            using( var connection = JobStorage.Current.GetConnection() )
            {
                foreach( var recurringJob in connection.GetRecurringJobs() )
                {
                    RecurringJob.RemoveIfExists( recurringJob.Id );
                }
            }

            var monitor = JobStorage.Current.GetMonitoringApi();
            foreach( var queue in monitor.Queues() )
            {
                foreach( var item in monitor.FetchedJobs( queue.Name, 0, (int) queue.Fetched.Value ) )
                {
                    if( item.Value.State != "Deleted" )
                        BackgroundJob.Delete( item.Key );
                }
            }

            foreach( var item in monitor.ScheduledJobs( 0, 1000 ) )
            {
                BackgroundJob.Delete( item.Key );
            }
#endif

            return result;
        }

        private static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath( Directory.GetCurrentDirectory() )
                .AddInMemoryCollection( new Dictionary<string, string>()
                {
                    { "DbIp", "92.222.35.243" },
                    { "DbPort", "27017" },
                    { "DbUser", "alBotDev" },
                    { "DbPass", "GGHYbJNBB5bJGFARBMFj" },
                    { "DbName", "alBotDev" },
                    { "BotToken", "" }
                } )
                .AddJsonFile( "appsettings.dev.json", true )
                .AddJsonFile( "appsettings.json", true )
                .Build();
        }

        private static async Task LogAsync( Discord.LogMessage log )
        {
            var severity = LogLevel.Information;

            switch( log.Severity )
            {
                case Discord.LogSeverity.Critical:
                    severity = LogLevel.Critical;
                    break;
                case Discord.LogSeverity.Error:
                    severity = LogLevel.Error;
                    break;
                case Discord.LogSeverity.Warning:
                    severity = LogLevel.Warning;
                    break;
                case Discord.LogSeverity.Info:
                default:
                    severity = LogLevel.Information;
                    break;
                case Discord.LogSeverity.Verbose:
                    severity = LogLevel.Trace;
                    break;
                case Discord.LogSeverity.Debug:
                    severity = LogLevel.Debug;
                    break;
            }

            if( log.Exception != null )
                logger.Log( severity, log.Exception, log.ToString( fullException: false ) );
            else
                logger.Log( severity, log.ToString() );

            if( log.Exception is CommandException cmdEx )
            {
                //if( log.Exception.InnerException is HttpRequestException )
                await cmdEx.Context.Channel.SendMessageAsync( $"Sorry {MentionUtils.MentionUser( cmdEx.Context.User.Id )}, something went catastrophically wrong. Lighting the bat signal... {MentionUtils.MentionUser( 139473652675510272 )}. Done!" );
            }

            await Task.CompletedTask;
        }
    }
}
