using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Utils.Extensions
{
    public static class ModuleBaseExtensions
    {
        public static Task DefaultError( this ModuleBase<SocketCommandContext> t )
        {
            return t.Context.Message.Channel.SendMessageAsync( $"Sorry <@{t.Context.User.Id}>, but I don't understand that command" );
        }
    }
}
