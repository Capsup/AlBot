using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Modules
{
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command( "help" )]
        public async Task HelpAsync()
        {
            await ReplyAsync( "You asked for help! I don't have any, LUL!" );
        }
    }
}
