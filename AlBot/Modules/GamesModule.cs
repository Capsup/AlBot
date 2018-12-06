using AlBot.Database;
using AlBot.Models.GamesModule;
using AlBot.Models.GamesModule.AdminModule;
using AlBot.Models.VotingModule;
using AlBot.Utils.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AlBot.Utils.Utils;
using ut = AlBot.Utils;

namespace AlBot.Modules
{
    [Group( "games" )]
    public class GamesModule : ModuleBase<SocketCommandContext>
    {
        private IDatabase _db;

        public GamesModule( IDatabase db )
        {
            this._db = db;
        }

        [Group( "vote" )]
        public class VotingModule : ModuleBase<SocketCommandContext>
        {
            private IDatabase _db;

            public VotingModule( IDatabase db )
            {
                this._db = db;
            }

            [Command(), Summary( "Allows a user to add their vote for adding a new game to the curated list" ), Priority( -10 )]
            [Alias( "add" )]
            public async Task GamesVoteAddAsync( [Summary( "The name of the game that the users wishes added to the list" )] [Remainder] string gameName )
            {
                var user = Context.User;
                if( user == null )
                    return;

                if( string.IsNullOrEmpty( gameName ) )
                {
                    await this.DefaultError();
                    return;
                }

                gameName = this.FixGameName( gameName );

                var rep = _db.Get<GameVote>();
                if( rep.Query().Any( x => x.UserLong == user.Id && x.GameName.ToLower() == gameName.ToLower() ) )
                {
                    await ReplyAsync( $"Sorry <@{user.Id}>, but you've already voted for {gameName}" );
                    return;
                }

                if( _db.Get<AddedGame>().Query().Any( x => x.Name.ToLower() == gameName.ToLower() ) )
                {
                    await ReplyAsync( $"Sorry <@{user.Id}>, but '{gameName}' already exists on the curated list of games" );
                    return;
                }

                await rep.InsertAsync( new GameVote() { GameName = gameName, UserLong = user.Id } );
                await ReplyAsync( $"The game '{gameName}' now has {rep.Query().Count( x => x.GameName.ToLower() == gameName.ToLower() )} votes!" );
            }

            [Command( "list" ), Summary( "Lists the current games which users have voted for to be added to the curated list" )]
            public async Task GameListAsync()
            {
                var rep = _db.Get<GameVote>();

                var games = rep.Query().GroupBy( x => x.GameName ).Select( x => new { Key = x.Key, Owners = x } ).ToArray();

                if( games.Length == 0 )
                {
                    await ReplyAsync( "Sorry, but no games have been voted for!" );
                    return;
                }

                var builder = new EmbedBuilder();

                builder.WithTitle( "The following games have been voted for:" );
                builder.WithDescription( String.Join( '\n', games.OrderByDescending( x => x.Owners.Count() ).ThenBy( x => x.Key ).Select( x => $"{x.Key} - {x.Owners.Count()} vote{( x.Owners.Count() == 1 ? "" : "s" )} " ) ) );

                await ReplyAsync( "", embed: builder.Build() );
            }

            [Command( "remove" ), Summary( "Allows a user to remove their vote for the given game" )]
            public async Task RemoveVoteAsync( [Summary( "The name of the game that the users wishes their voted removed from" )] [Remainder] string gameName )
            {
                var user = Context.User;
                if( user == null )
                    return;

                if( string.IsNullOrEmpty( gameName ) )
                {
                    await this.DefaultError();
                    return;
                }

                gameName = this.FixGameName( gameName );

                var rep = _db.Get<GameVote>();
                GameVote found = null;
                if( ( found = rep.Query().FirstOrDefault( x => x.UserLong == user.Id && x.GameName == gameName ) ) == null )
                {
                    await ReplyAsync( $"Sorry <@{user.Id}>, but you haven't voted for {gameName}" );
                    return;
                }

                await rep.DeleteAsync( found );
                await ReplyAsync( $"The game '{gameName}' now has {rep.Query().Count( x => x.GameName == gameName )} votes!" );
            }
        }

        [Group( "schedule" )]
        public class ScheduleModule : ModuleBase<SocketCommandContext>
        {
            private IDatabase _db;

            public ScheduleModule( IDatabase db )
            {
                this._db = db;
            }

            [Command(), Summary( "Allows a users to view scheduled games" ), Priority( -10 )]
            [Alias( "list" )]
            public async Task ScheduleListAsync()
            {
                var user = Context.User;
                if( user == null )
                    return;

                var scheduledGames = _db.Get<ScheduledGame>().Query().ToArray();
                var builder = new EmbedBuilder();
                builder.WithTitle( $"Upcoming scheduled games" );
                builder.WithDescription( $"In total, there are {scheduledGames.Length} scheduled game{(scheduledGames.Length == 1 ? "" : "s" )}:" );

                /*#if DEBUG
                                var isMod = true;
                #else
                                var isMod = this.Context.Guild.GetUser( Context.User.Id ).Roles.Any( x => x.Id == 266611875708534785 );
                #endif*/

                foreach( var game in scheduledGames )
                {
                    var embedBuilder = new EmbedFieldBuilder();
                    var foundGame = _db.Get<AddedGame>().Query().FirstOrDefault( x => x.Id == game.GameId );
                    if( foundGame == null )
                        continue;

                    embedBuilder
                        .WithName( $"{foundGame.Name} - {game.StartTime.ToString( "U", System.Globalization.CultureInfo.InvariantCulture ) + " UTC"} ({ game.Id })" )
                        .WithValue( game.ConfirmedUserIds?.Length > 0 ? $"With the following players signed up:\n {string.Join( "\n", game.ConfirmedUserIds.Select( x => this.Context.Client.GetUser( x ).Username ) )}" : "No players signed up yet." );
                    builder.AddField( embedBuilder );
                }

                await ReplyAsync( "", embed: builder.Build() );
            }

            [Command( "signup" ), Summary( "Allows a users to sign up for a scheduled game" )]
            [Alias( "addme", "isinterested" )]
            public async Task ScheduleSignupAsync( [Summary( "The id of the scheduledGame to toggle attendance for. Can be found using the 'schedule list' command" )] string scheduledGameId )
            {
                var user = Context.User;
                if( user == null )
                    return;

                if( string.IsNullOrEmpty( scheduledGameId ) )
                {
                    await this.DefaultError();
                    return;
                }

                scheduledGameId = scheduledGameId.Trim( '"' );

                var scheduledGame = _db.Get<ScheduledGame>().Query().FirstOrDefault( x => x.Id == scheduledGameId );
                if( scheduledGame == null )
                {
                    await ReplyAsync( $"Sorry {MentionUtils.MentionUser( user.Id )}, but I couldn't find a scheduled game with that ID" );
                    return;
                }

                int index = -1;
                var list = scheduledGame.ConfirmedUserIds?.ToList();
                if( list == null )
                    list = new List<ulong>();
                if( ( index = list.IndexOf( user.Id ) ) >= 0 )
                {
                    list.RemoveAt( index );
                    await ReplyAsync( $"{MentionUtils.MentionUser( user.Id )}, I have removed you from the scheduledGame's attendance list" );
                }
                else
                {
                    list.Add( user.Id );
                    await ReplyAsync( $"{MentionUtils.MentionUser( user.Id )}, you have been added to the scheduledGame's attendance list" );
                }

                scheduledGame.ConfirmedUserIds = list.ToArray();
                await _db.Get<ScheduledGame>().ReplaceAsync( scheduledGame );
            }
        }

        [Command( "addme" ), Summary( "Allows a user to add a game from the curated list, to their list of owned games" )]
        [Alias( "thatiown", "whichiown" )]
        public async Task GamesAddMeAsync( [Summary( "The name of the game that the user wishes to be marked as owning" )] string gameName, [Remainder] [Summary( "Whether or not the user is interested in getting letsPlay notifications for this game. Available options are: 'true' or 'false'. Defaults to 'false'" )] string isInterested = "false" )
        {
            var user = Context.User;
            if( user == null )
                return;

            if( string.IsNullOrEmpty( gameName ) )
            {
                await this.DefaultError();
                return;
            }

            bool bIsInterested = false;
            if( !string.IsNullOrEmpty( isInterested ) )
            {
                int index = -1;
                if( ( index = isInterested.LastIndexOf( ' ' ) ) != -1 )
                {
                    gameName = gameName + " " + isInterested.Substring( 0, index );
                    isInterested = isInterested.Substring( index + 1 );
                }
                /*else
                    gameName = gameName + " " + isInterested;*/

                if( isInterested.ToLower() != "false" && isInterested.ToLower() != "true" )
                {
                    gameName = gameName + " " + isInterested;
                    isInterested = null;
                }
                else
                    bIsInterested = isInterested.ToLower() == "true";
            }

            gameName = this.FixGameName( gameName );

            var gameRep = _db.Get<AddedGame>();
            var found = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
            if( found == null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, the game of '{gameName}' hasn't been added to the curated list" );
                return;
            }

            var rep = _db.Get<UserGameOwnership>();
            await rep.ReplaceAsync( new UserGameOwnership() { UserId = user.Id, GameId = found.Id, IsInterested = bIsInterested }, ( x => x.GameId == found.Id && x.UserId == Context.User.Id ), true );

            await ReplyAsync( $"<@{user.Id}> now owns the game of '{gameName}' " + ( !bIsInterested ? "but is not interested in letsPlay notifications" : "and is interested in letsPlay notifications" ) );
        }

        [Command( "removeme" ), Summary( "Allows a user to remove a owned game from their list" )]
        public async Task GamesRemoveMeAsync( [Summary( "The name of the game that the user wishes to be marked as not owning" )] [Remainder] string gameName )
        {
            var user = Context.User;
            if( user == null )
                return;

            if( string.IsNullOrEmpty( gameName ) )
            {
                await this.DefaultError();
                return;
            }

            gameName = this.FixGameName( gameName );

            var gameRep = _db.Get<AddedGame>();
            var exists = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
            if( exists == null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, that game hasn't been added to the curated list" );
                return;
            }

            var rep = _db.Get<UserGameOwnership>();
            var found = rep.Query().FirstOrDefault( x => x.GameId == exists.Id && x.UserId == Context.User.Id );
            if( found != null )
            {
                rep.Delete( found );
                await ReplyAsync( $"<@{user.Id}> no longer owns the game of '{gameName}'" );
            }
            else
                await ReplyAsync( $"Sorry <@{user.Id}>, but I didn't have you registered as owning '{gameName}' already" );
        }

        [Command( "interestedIn" ), Summary( "Allows a user to mark them as interested in letsPlay notifcations for a game that they own. Use again on an already interested game to toggle off" )]
        public async Task GamesInterestedInAsync( [Summary( "The name of the game that the user wishes to toggle marked as interested in" )] string gameName, [Remainder] [Summary( "The days of the week that the user is interested in getting letsPlay notifications, in the format of '1111111' to specify days, starting with Monday and ending with Sunday. Default to '1111111'" )] string daysWeek = null )
        {
            var user = Context.User;
            if( user == null )
                return;

            if( string.IsNullOrEmpty( gameName ) )
            {
                await this.DefaultError();
                return;
            }

            Match match = null;
            if( !string.IsNullOrEmpty( daysWeek ) )
            {
                int index = -1;

                if( ( match = Regex.Match( daysWeek, @"\s*(\d{7})\s*$" ) )?.Success ?? false )
                {
                    gameName = gameName + " " + daysWeek.Substring( 0, daysWeek.Length - match.Length );
                    daysWeek = match.Groups[ 1 ].Value;
                }

                /*if( ( index = daysWeek.LastIndexOf( ' ' ) ) != -1 )
                {
                    gameName = gameName + " " + daysWeek.Substring( 0, index );
                    daysWeek = daysWeek.Substring( index + 1 );
                }*/
                else
                {
                    gameName = gameName + " " + daysWeek;
                    daysWeek = null;
                }
            }

            gameName = this.FixGameName( gameName );

            var gameRep = _db.Get<AddedGame>();
            var foundGame = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
            if( foundGame == null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, the game of '{gameName}' hasn't been added to the curated list" );
                return;
            }

            var rep = _db.Get<UserGameOwnership>();
            var found = rep.Query().FirstOrDefault( x => x.GameId == foundGame.Id && x.UserId == user.Id );
            if( found != null )
            {
                if( !string.IsNullOrEmpty( daysWeek ) )
                {
                    if( daysWeek.Length != 7 )
                    {
                        await ReplyAsync( $"Sorry {MentionUtils.MentionUser( user.Id )}, but there is only 7 days in a week. Use the format of '1111111' to specify days, starting with Monday and ending with Sunday." );
                        return;
                    }

                    var newArr = new System.Collections.BitArray( 7 );
                    string finalInterestedDays = "";
                    for( int i = 0; i < 7; i++ )
                    {
                        var interested = daysWeek[ i ] == '1';
                        newArr[ i ] = interested;
                        if( interested )
                            finalInterestedDays += Enum.GetName( typeof( DayOfWeek ), (DayOfWeek) ( ( i + 1 ) % 7 ) ) + ", ";
                    }

                    found.InterestedDays = newArr;
                    found.IsInterested = true;

                    await ReplyAsync( $"<@{user.Id}> is now interested in letsPlay notifications for '{gameName}' {( finalInterestedDays.Length > 0 ? "on the days of: " + finalInterestedDays.Substring( 0, finalInterestedDays.Length - 2 ) : "" )}" );
                }
                else if( !found.IsInterested )
                {
                    found.IsInterested = true;
                    await ReplyAsync( $"<@{user.Id}> is now interested in letsPlay notifications for '{gameName}'" );
                }
                else
                {
                    found.IsInterested = false;
                    await ReplyAsync( $"<@{user.Id}> is no longer interested in letsPlay notifications for '{gameName}'" );
                }

                await rep.ReplaceAsync( found );
            }
            else
                await ReplyAsync( $"Sorry <@{user.Id}>, but I didn't have you registered as owning '{gameName}' already" );
        }

        [Command( "list" ), Summary( "Gives the user a list of all unique games currently registered to any users" ), Priority( -10 )]
        //[Alias( "" )]
        public async Task GamesListAsync( [Summary( "An optional filter to use for searching names" )] [Remainder] string searchFilter = null )
        {
            searchFilter = this.FixGameName( searchFilter );

            var rep = _db.Get<UserGameOwnership>();
            var uniqueGamesDelegate = rep.Query();
            var gameRep = _db.Get<AddedGame>().Query();
            if( !string.IsNullOrEmpty( searchFilter ) )
            {
                gameRep = gameRep.Where( x => x.Name.ToLower().Contains( searchFilter.ToLower() ) );
            }

            var UGOQuery = rep.Query();
            var uniqueGames = gameRep.GroupJoin( UGOQuery, x => x.Id, y => y.GameId, ( x, y ) => new { Key = x.Name, Owners = y } ).OrderBy( x => x.Key );
            //var uniqueGames = uniqueGamesDelegate.GroupBy( x => x.GameId ).Select( x => new { Key = x.Key, Count = x.Count() } ).ToList();
            var builder = new EmbedBuilder();

            builder.WithTitle( "Curated list of games" );
            builder.WithDescription( "Game Name - IsInterested?\n" + String.Join( '\n', uniqueGames.Select( x => $"**{x.Key}** - _{x.Owners.Count()} owner{( x.Owners.Count() == 1 ? "" : "s" )}_\n{string.Join( '\n', x.Owners.Select( y => "\u200b  " + this.Context.Client.GetUser( y.UserId ).Username + " - " + ( y.IsInterested ? "Yes" : "No" ) ) )}" ) ) );

            await ReplyAsync( "", embed: builder.Build() );
        }

        [Command( "whoOwns" ), Summary( "Gives the user a list of all users who owns the given game" )]
        public async Task GamesWhoOwnsAsync( [Summary( "The game name to include owning users for" )] [Remainder] string gameName )
        {
            if( string.IsNullOrEmpty( gameName ) )
            {
                await this.DefaultError();
                return;
            }

            gameName = this.FixGameName( gameName );

            var user = Context.User;
            if( user == null )
                return;

            var gameRep = _db.Get<AddedGame>();
            var found = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
            if( found == null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, that game hasn't been added to the curated list" );
                return;
            }

            var owners = _db.Get<UserGameOwnership>().Query().Where( x => x.GameId == found.Id );
            if( owners == null || owners.Count() == 0 )
            {
                await ReplyAsync( $"Sorry <@{user.Id}>, but I didn't find any users owning '{gameName}'" );
                return;
            }

            var builder = new EmbedBuilder();

            builder.WithTitle( "Owners of " + gameName );
            builder.WithDescription( "Game Name - IsInterested?\n" + String.Join( '\n', owners.Select( x => this.Context.Client.GetUser( x.UserId ).Username + " - " + ( x.IsInterested ? "Yes" : "No" ) ) ) );

            await ReplyAsync( "", embed: builder.Build() );
        }

        [Command( "iOwn" ), Summary( "Gives the user a list of all the games that they own, including whether they're interestedIn the game or not" )]
        [Alias( "imInterestedIn", "thatIOwn", "thatImInterestedIn" )]
        public async Task GamesIOwnAsync()
        {
            var user = Context.User;
            if( user == null )
                return;

            /*var rep = _db.Get<UserGameOwnership>();
            var gameRep = _db.Get<AddedGame>().Query();
            var UGOQuery = rep.Query();
            var ownedGames = gameRep.GroupJoin( UGOQuery, x => x.Id, y => y.GameId, ( x, y ) => new { GameName = x.Name } ).OrderBy( x => x.GameName );*/


            var UGORep = _db.Get<UserGameOwnership>();
            var AGRep = _db.Get<AddedGame>();
            var ownedGames = UGORep.Query().Where( x => x.UserId == user.Id ).GroupJoin( AGRep.Query(), x => x.GameId, y => y.Id, ( x, y ) => new { GameName = y.First().Name, IsInterested = x.IsInterested } );
            if( ownedGames == null || ownedGames.Count() == 0 )
            {
                await ReplyAsync( $"Sorry <@{user.Id}>, but I didn't find any games that you own" );
                return;
            }

            var builder = new EmbedBuilder();

            builder.WithTitle( $"Games that {user.Username} owns" );
            builder.WithDescription( "Game Name - IsInterested?\n" + String.Join( '\n', ownedGames.Select( x => "**" + x.GameName + "** - " + ( x.IsInterested ? "Yes" : "No" ) ) ) );

            await ReplyAsync( "", embed: builder.Build() );
        }

        [Command( "letsPlay" ), Summary( "Sends a PM to all users who owns the given game and are marked as isInterested for that game" )]
        public async Task GamesLetsPlayAsync( [Summary( "The game name to send messages to of the owning users" )] [Remainder] string gameName )
        {
            if( string.IsNullOrEmpty( gameName ) )
            {
                await this.DefaultError();
                return;
            }

            gameName = this.FixGameName( gameName );

            var user = Context.User;
            if( user == null )
                return;

            var gameRep = _db.Get<AddedGame>();
            var found = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
            if( found == null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, that game hasn't been added to the curated list" );
                return;
            }

            var owners = _db.Get<UserGameOwnership>().Query().Where( x => x.GameId == found.Id && x.IsInterested == true );
            if( owners == null || owners.Count() == 0 )
            {
                await ReplyAsync( $"Sorry <@{user.Id}>, but I didn't find any users owning '{gameName}' or no one isInterested in notifications" );
                return;
            }

            foreach( var owningUser in owners )
            {
                if( owningUser.InterestedDays != null && !owningUser.InterestedDays[ ( (int) DateTime.UtcNow.DayOfWeek ) + 1 ] )
                {
                    continue;
                }

                var targetUser = this.Context.Client.GetUser( owningUser.UserId );
                if( targetUser == null )
                    continue;

                await targetUser.SendMessageAsync( $"Hi {targetUser.Username}!\nYou are hereby invited by '{user.Username}' from the Al-Khezam Discord to play a game of '{gameName}'. \nIf you wish to join, head on over to the {MentionUtils.MentionChannel( 503248406597206016 ) } channel.\n\nIf you no longer wish to be eligible for these PMs, go to {MentionUtils.MentionChannel( 503248406597206016 ) } and type in '{MentionUtils.MentionUser( this.Context.Client.CurrentUser.Id )} games interestedIn \"{gameName}\"' and you'll be removed from the list." );
            }
        }
    }
}
