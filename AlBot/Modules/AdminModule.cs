using AlBot.Database;
using AlBot.Models.GamesModule;
using AlBot.Models.VotingModule;
using AlBot.Utils.Attributes;
using AlBot.Utils.Extensions;
using static AlBot.Utils.DateTimeUtils;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlBot.Models.GamesModule.AdminModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlBot.Modules
{
    [Group( "admin" )]
    public class AdminModule : InteractiveBase<SocketCommandContext>
    {
        private IDatabase _db;

        public AdminModule( IDatabase db )
        {
            this._db = db;
        }

        [RequireRole()]
        [Command( "addGame" ), Summary( "Allows a moderator to add a game to the curated list of games. Will automatically remove the equivalent game from the user voting list" )]
        public async Task AddGameAsync( [Summary( "The name that the moderator wishes to add to the curated list of games." )] [Remainder] string gameName )
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

            var voteRep = _db.Get<GameVote>();
            var foundVotes = voteRep.Query().Where( x => x.GameName.ToLower() == gameName.Trim('"').ToLower() ).Select( x => x.Id ).ToArray();
            await voteRep.DeleteManyAsync( foundVotes );

            var gameRep = _db.Get<AddedGame>();
            var found = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
            if( found != null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, that game is already on the curated list" );
                return;
            }

            await gameRep.InsertAsync( new AddedGame() { Name = gameName } );
            await ReplyAsync( $"{MentionUtils.MentionUser( Context.User.Id )} just added the game '{gameName}' to the curated list!" );
        }

        [RequireRole()]
        [Command( "removeGame" ), Summary( "Allows a moderator to remove the given game from the curated list of games. The matched game will be removed from everyone's owned list" )]
        public async Task RemoveGameAsync( [Summary( "The name that the moderator wishes to remove from the curated list of games." )] [Remainder] string gameName )
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
            var found = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
            if( found == null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, that game doesn't exist on the curated list" );
                return;
            }

            var ownerRep = _db.Get<UserGameOwnership>();
            await ownerRep.DeleteManyAsync( ownerRep.Query().Where( x => x.GameId == found.Id ).Select( x => x.Id ).ToArray() );

            await gameRep.DeleteAsync( found );
            await ReplyAsync( $"{MentionUtils.MentionUser( Context.User.Id )} just removed the game of '{gameName}' from the curated list!" );
        }

        [RequireRole()]
        [Command( "removeVote" ), Summary( "Allows a moderator to remove the given game from the voted list." )]
        public async Task VoteRemoveGameAsync( [Summary( "The name that the moderator wishes to remove from the voted list" )] [Remainder] string gameName )
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

            var gameRep = _db.Get<GameVote>();
            var found = gameRep.Query().Where( x => x.GameName.ToLower() == gameName.ToLower() );
            if( found == null )
            {
                await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, that game doesn't exist on voter list" );
                return;
            }

            var ownerRep = _db.Get<GameVote>();
            await ownerRep.DeleteManyAsync( found.Select( x => x.Id ).ToArray() );

            await ReplyAsync( $"{MentionUtils.MentionUser( Context.User.Id )} just removed the game of '{gameName}' from the voter list!" );
        }

        [Group( "schedule" )]
        public class AdminScheduleModule : InteractiveBase<SocketCommandContext>
        {
            private IDatabase _db;

            public AdminScheduleModule( IDatabase db )
            {
                this._db = db;
            }

            [RequireRole()]
            [Command( "add", RunMode = RunMode.Async ), Summary( "Allows a moderator to schedule a game ahead of time. Will notify all players who own a specified game in PMs" )]
            public async Task ScheduleAddGameAsync( [Summary( "The name of the game that the moderator wishes to schedule ahead of time" )] string gameName, [Remainder] string time )
            {
                var user = Context.User;
                if( user == null )
                    return;

                if( string.IsNullOrEmpty( gameName ) )
                {
                    await this.DefaultError();
                    return;
                }

                var gameRep = _db.Get<AddedGame>();
                var found = gameRep.Query().FirstOrDefault( x => x.Name.ToLower() == gameName.ToLower() );
                if( found == null )
                {
                    await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, that game doesn't exist on the curated list" );
                    return;
                }

                var builder = new EmbedBuilder();

                if( !time.TryParseDateTime( DateTimeFormat.UK_DATE, out ParsedDateTime parsedTime ) )
                {
                    await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, but I couldn't understand that date format" );
                    return;
                }

                DateTime finalTime;
                if( parsedTime.IsUtcOffsetFound )
                    finalTime = parsedTime.UtcDateTime;
                else
                {
                    finalTime = new DateTime( parsedTime.DateTime.Ticks, DateTimeKind.Utc );
                }

                var timeString = finalTime.ToString( "U", System.Globalization.CultureInfo.InvariantCulture ) + " UTC";

                builder.WithTitle( "About to schedule game for " + gameName );
                builder.WithDescription( $"{MentionUtils.MentionUser( Context.User.Id )}, you're about to schedule a game for '{gameName}' ahead of time.\nThe scheduled time is _{timeString}_.\n\n**If this time is not correct, reply with 'no' within the next 10 seconds and this game will be deleted.**" );

                var gameMessage = await ReplyAsync( "", embed: builder.Build() );
                var response = await NextMessageAsync( timeout: TimeSpan.FromSeconds( 10 ) );
                if( response != null )
                {
                    if( !string.IsNullOrEmpty( response.Content ) && response.Content.ToLower() == "no" )
                    {
                        await gameMessage.DeleteAsync();
                        await ReplyAsync( $"{MentionUtils.MentionUser( Context.User.Id )} has deleted the scheduled game" );
                        return;
                    }
                }

                var newSchedule = new ScheduledGame() { GameId = found.Id, StartTime = parsedTime.DateTime, UserId = Context.User.Id };
                await _db.Get<ScheduledGame>().InsertAsync( newSchedule );

                //Context.Channel.SendMessageAsync( "Hi!", default( bool ), default( Embed ), default( RequestOptions ) )
#if DEBUG
                var scheduleTime = TimeSpan.FromSeconds( 5 );
#else
                var scheduleTime = finalTime;
#endif
                var jobId = BackgroundJob.Schedule( () => Utils.ScheduleUtils.BackgroundFuncJob.ScheduleGame( newSchedule.Id ), scheduleTime );
                newSchedule.JobId = jobId;
                await _db.Get<ScheduledGame>().ReplaceAsync( newSchedule );


                //await ReplyAsync( $"{MentionUtils.MentionUser( Context.User.Id )} just scheduled a game of '{gameName}'!" );
                await gameMessage.ModifyAsync( x =>
                {
                    builder = new EmbedBuilder();

                    builder.WithTitle( $"Scheduled '{gameName}' game at {timeString}" );
                    builder.WithDescription( $"{MentionUtils.MentionUser( Context.User.Id )}, you have scheduled a game of '{gameName}' to be started at _{timeString}_\nI will automatically remind everyone who owns the game exactly 1 day ahead of time and on the start time.\n\nNo further action is required from you from here." );
                    x.Embed = builder.Build();
                } );
                //BackgroundJob.Enqueue( () => Console.WriteLine( "hai" ) );
            }

            [RequireRole()]
            [Command( "remove" ), Summary( "Allows a moderator to remove a scheduled game." )]
            public async Task ScheduleRemoveGameAsync( [Summary( "The id of the scheduled game that the moderator wishes to remove. Can be found using the 'games schedule' command" )] string scheduledGameId )
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

                var gameRep = _db.Get<ScheduledGame>();
                var found = gameRep.Query().FirstOrDefault( x => x.Id == scheduledGameId );
                if( found == null )
                {
                    await ReplyAsync( $"Sorry {MentionUtils.MentionUser( Context.User.Id )}, I couldn't find that a scheduledGame by that id" );
                    return;
                }

                var foundGame = _db.Get<AddedGame>().Query().FirstOrDefault( x => x.Id == found.GameId );
                if( foundGame == null )
                    throw new Exception( "ERROR! Failed to find game associated with scheduledGame" );

                try
                {
                    BackgroundJob.Delete( found.JobId );
                }
                catch( Exception e )
                {
                    Program.Services.GetRequiredService<ILogger<AdminModule>>().LogError( e, $"ERROR! Failed to remove backgroundJob with ID of '{found.JobId}'" );
                }

                await gameRep.DeleteAsync( found );
                await ReplyAsync( $"Has deleted the scheduled game of '{found.GameId}' that should have started at {found.StartTime.ToString( "U", System.Globalization.CultureInfo.InvariantCulture ) + " UTC"}" );
            }
        }
    }
}
