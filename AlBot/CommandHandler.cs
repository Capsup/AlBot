using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

            if( result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand ) // it's bad practice to send 'unknown command' errors
                await context.Channel.SendMessageAsync( result.ToString() );
        }
    }
}
