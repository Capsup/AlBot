using AlBot.Database;
using AlBot.Database.Mongo.Extensions;
using AlBot.Models.VotingModule;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Modules
{
    public class VotingModule : ModuleBase<SocketCommandContext>
    {
        private IDatabase _db;

        public VotingModule( IDatabase db )
        {
            this._db = db;
        }

        [Command( "vote" )]
        public async Task VoteAsync( string gameName )
        {
            var user = Context.User;
            if( user == null )
                return;

            if( string.IsNullOrEmpty( gameName ) )
            {
                await ReplyAsync( $"<@{user.Id}> You forgot to add a name, idjit!" );
                return;
            }

            var rep = _db.Get<GameVote>();
            if( rep.Query().Any( x => x.UserLong == user.Id && x.GameName == gameName ) )
            {
                await ReplyAsync( $"Sorry <@{user.Id}>, but you've already voted for {gameName}" );
                return;
            }

            await rep.InsertAsync( new GameVote() { GameName = gameName, UserLong = user.Id } );
            await ReplyAsync( $"The game {gameName} now has {rep.Query().Count( x => x.GameName == gameName )} votes!" );
        }

        [Command("games")]
        public async Task GameListAsync()
        {
            var rep = _db.Get<GameVote>();

            //Mongo's GroupBy appears to be bugged, or atleast behaving differently than you'd expect from IQueryable, so we load everything into RAM
            var games = rep.Query().ToArray().GroupBy( x => x.GameName ).ToArray();

            if( games.Length == 0 )
            {
                await ReplyAsync( "Sorry, but no games have been voted for!" );
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine( $"The following games have been voted for: " );
            foreach( var game in games )
            {
                var count = game.Select( x => x.UserLong ).Distinct().Count();
                sb.AppendLine( $"{game.Key} - {count} vote{( count == 1 ? "" : "s" )} " );
            }

            await ReplyAsync( sb.ToString() );
        }
    }
}
