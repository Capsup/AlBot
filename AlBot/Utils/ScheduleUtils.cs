using AlBot.Database;
using AlBot.Models.GamesModule;
using AlBot.Models.GamesModule.AdminModule;
using Discord;
using Discord.WebSocket;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Utils
{
    public static class ScheduleUtils
    {
        public class BackgroundFuncJob
        {
            public static async Task ScheduleGame( string scheduledId )
            {
                if( string.IsNullOrEmpty( scheduledId ) )
                    throw new ArgumentException( "ERROR! scheduledId cannot be null or empty" );

                var services = Program.Services;
                var _db = services.GetRequiredService<IDatabase>();
                var found = _db.Get<ScheduledGame>().Query().FirstOrDefault( x => x.Id == scheduledId );
                if( found == null )
                    throw new ArgumentException( "ERROR! Could not find ScheduledGame with the given id" );

                var addedGame = _db.Get<AddedGame>().Query().FirstOrDefault( x => x.Id == found.GameId );
                if( addedGame == null )
                    throw new Exception( "ERROR! Could not find AddedGame with the given id" );

                var discord = services.GetRequiredService<DiscordSocketClient>();
                var timeString = found.StartTime.ToString( "U", System.Globalization.CultureInfo.InvariantCulture ) + " UTC";
                var builder = new EmbedBuilder();

#if DEBUG
                var channel = discord.GetGuild( 209247707951267840 ).TextChannels.First( x => x.Id == 378275172966465536 );
#else
                var channel = discord.GetGuild( 188763107584114688 ).TextChannels.First( x => x.Id == 503248406597206016 );
#endif

                if( ( found.StartTime - DateTime.UtcNow ).TotalHours > 23 )
                {
                    builder = new EmbedBuilder();

                    builder.WithTitle( $"Scheduled '{addedGame.Name}' game starting at _{timeString}_" );
                    builder.WithDescription( $"{MentionUtils.MentionUser( found.UserId )} has scheduled a game of '{addedGame.Name}' to be started at _{timeString}_\nThis is your 1-day reminder that the game is happening.\n\nI will automatically remind everyone who owns the game again on the start time.\n\nNo further action is required from here." );

                    await channel.SendMessageAsync( "", embed: builder.Build() );

#if DEBUG
                    var scheduleTime = TimeSpan.FromSeconds( 5 );
#else
                    var scheduleTime = found.StartTime;
#endif
                    var newJobId = BackgroundJob.Schedule( () => ScheduleGame( found.Id ), scheduleTime );
                    found.JobId = newJobId;

                    await _db.Get<ScheduledGame>().ReplaceAsync( found );

                    return;
                }

                builder = new EmbedBuilder();

                builder.WithTitle( $"Scheduled '{addedGame.Name}' game starting at _{timeString}_ - **which is now!**" );
                builder.WithDescription( $"{MentionUtils.MentionUser( found.UserId )} has scheduled a game of '{addedGame.Name}' to be started now\nThis is your reminder that the game is happening and it's time to go play! Enjoy." );

                var finalMessage = "";
                foreach( var owner in _db.Get<UserGameOwnership>().Query().Where( x => x.GameId == addedGame.Id ).ToArray() )
                {
                    finalMessage += MentionUtils.MentionUser( owner.UserId ) + " ";
                }

                await channel.SendMessageAsync( finalMessage, embed: builder.Build() );

                await _db.Get<ScheduledGame>().DeleteAsync( found );
            }
        }
    }
}
