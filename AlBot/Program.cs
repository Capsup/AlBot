using AlBot.Database;
using AlBot.Database.Mongo;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AlBot
{
    public class Program
    {
        private static ILogger<Program> logger;

        public static void Main( string[] args )
        {
            Program.Run().GetAwaiter().GetResult();
        }

        public static async Task Run()
        {
            var configuration = LoadConfiguration();

            if( string.IsNullOrEmpty( configuration.GetValue<string>( "BotToken" ) ) )
            {
                Console.WriteLine("No BotToken configured! Stopping..." );
                return;
            }

            var services = ConfigureServices( configuration );
            logger = services.GetRequiredService<ILogger<Program>>();
            var discordClient = services.GetRequiredService<DiscordSocketClient>();

            discordClient.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            await discordClient.LoginAsync( Discord.TokenType.Bot, configuration.GetValue<string>( "BotToken" ) );
            await discordClient.StartAsync();

            services.GetRequiredService<CommandHandler>();

            Console.WriteLine("Connected!");
            await Task.Delay( -1 );
        }

        private static IServiceProvider ConfigureServices( IConfiguration config )
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddLogging( builder => builder.AddFile( "logs\\log.txt" ).SetMinimumLevel( System.Diagnostics.Debugger.IsAttached ? LogLevel.Trace : LogLevel.Information ) )
                .AddSingleton<IDatabaseFactory, MongoDatabaseFactory>()
                .AddSingleton<IDatabase, MongoDatabase>( services => services.GetRequiredService<IDatabaseFactory>().Create( config[ "DbName" ], config[ "DbUser" ], config[ "DbPass" ], config[ "DbIp" ], config.GetValue<int>( "DbPort" ) ) as MongoDatabase )
                .BuildServiceProvider();
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

            await Task.CompletedTask;
        }
    }
}
