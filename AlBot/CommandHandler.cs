using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AlBot
{
    public class CommandHandler
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandler( IServiceProvider services )
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _commands.AddModulesAsync( Assembly.GetEntryAssembly() ).GetAwaiter().GetResult();

            _discord.MessageReceived += MessageReceivedAsync;
        }

        private async Task MessageReceivedAsync( SocketMessage rawMessage )
        {
            // Ignore system messages, or messages from other bots
            if( !( rawMessage is SocketUserMessage message ) )
                return;
            if( message.Source != MessageSource.User )
                return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            if( !message.HasMentionPrefix( _discord.CurrentUser, ref argPos ) )
                return;

            var context = new SocketCommandContext( _discord, message );
            var result = await _commands.ExecuteAsync( context, argPos, _services );

            if( result.Error.HasValue )
            {
                switch( result.Error )
                {
                    case CommandError.UnknownCommand:
                        await context.Channel.SendMessageAsync( $"Sorry {MentionUtils.MentionUser( context.User.Id )}, but I don't understand that command. Type '{MentionUtils.MentionUser( context.Client.CurrentUser.Id )} help' to get an overview of available commands." );
                        break;
                    case CommandError.BadArgCount:
                        await context.Channel.SendMessageAsync( $"Sorry {MentionUtils.MentionUser( context.User.Id )}, but that parameter count does not match the command. Type '{MentionUtils.MentionUser( context.Client.CurrentUser.Id )} help' to get an overview of available commands." );
                        break;
                    /*default:
                        await context.Channel.SendMessageAsync( $"Sorry {MentionUtils.MentionUser( context.User.Id )}, but an internal error occured. Blame Capsup or something ;)" );
                        Program.Services.GetRequiredService<ILogger<CommandHandler>>().LogError( result.Error.ToString() );
                        break;*/
                }
            }
        }
    }
}
