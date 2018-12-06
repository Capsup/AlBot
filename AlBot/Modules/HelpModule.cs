using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Modules
{
    [Group("help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;

        public HelpModule( CommandService service )
        {
            _service = service;
        }

        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [Command(), Summary("Gives the user an overview of available commands that they can use") ]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder()
            {
                Color = new Color( 114, 137, 218 ),
                Description = $"These are the commands **you** can use\n\nAny command can be used with '{MentionUtils.MentionUser( Context.Client.CurrentUser.Id ) } help _command name_' to get additional information.\n_Italic_ is parameter names that you need to fill in\n_(Italic)_ parameters inside braces () are optional parameters\n[Command names] inside brackets [] are aliases for the given command. All of them work the same\nCommands with more than one parameter, requires the individual parameters to be enclosed in quotations \"\", for example 'games addme \"League of Legends\"', to work "
            };

            foreach( var module in _service.Modules )
            {
                string description = null;
                foreach( var cmd in module.Commands )
                {
                    var result = await cmd.CheckPreconditionsAsync( Context );
                    if( result.IsSuccess )
                    {
                        var alias = cmd.Aliases.First();
                        if( cmd.Aliases.Count > 1 )
                        {
                            var optionalNames = string.Join( ",", cmd.Aliases.Skip( 1 ).Where( x => !string.IsNullOrEmpty( x ) ).OrderBy( x => x.Length ).Select( x => (x.Length <= alias.Length || x.Substring( 0, alias.Length ) != alias ? x.Substring( alias.LastIndexOf( " " ) + 1 ) : x.Substring( alias.Length + 1 )).Trim() ) );
                            alias = alias + (alias.Count( x => x == ' ' ) != cmd.Aliases.Skip(1).First().Count( x => x == ' ' ) ? " " : "" ) + "[" + optionalNames + "]" ;
                        }
                        description += $"{alias} {string.Join(' ', cmd.Parameters.Select( x => !x.IsOptional ? "_" + x.Name + "_" :  "_(" + x.Name + ")_" ) )} \n";
                    }
                }

                if( !string.IsNullOrWhiteSpace( description ) )
                {
                    builder.AddField( x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    } );
                }
            }

            await ReplyAsync( "", false, builder.Build() );
        }

        [Command(), Summary( "Allows a user to get a more detailed explanation of individual commands and their parameters" )]
        public async Task HelpAsync( [Summary("An optional parameter that shows help for a specific command"), Remainder] string command )
        {
            var result = _service.Search( Context, command );

            if( !result.IsSuccess )
            {
                await ReplyAsync( $"Sorry, I couldn't find a command like **{command}**." );
                return;
            }

            var builder = new EmbedBuilder()
            {
                Color = new Color( 114, 137, 218 ),
                Description = $"Here are some commands like **{command}**"
            };

            foreach( var match in result.Commands )
            {
                var cmd = match.Command;

                builder.AddField( x =>
                {
                    x.Name = string.Join( ", ", cmd.Aliases );
                    x.Value = $"Summary: {cmd.Summary}\n" +
                              $"Parameters:\n{string.Join( "\n", cmd.Parameters.Select( p => $"_{p.Name}_ - {p.Summary}" ) )}";
                    x.IsInline = false;
                } );
            }

            await ReplyAsync( "", false, builder.Build() );
        }
    }
}
