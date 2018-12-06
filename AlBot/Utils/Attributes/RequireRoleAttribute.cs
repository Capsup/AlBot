using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Utils.Attributes
{
    public class RequireRoleAttribute : PreconditionAttribute
    {
        protected ulong roleId;

        public RequireRoleAttribute( ulong roleId = 266611875708534785 )
        {
            this.roleId = roleId;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync( ICommandContext context, CommandInfo command, IServiceProvider services )
        {
#if DEBUG
            return PreconditionResult.FromSuccess();
#else

            var role = context.Guild.Roles.FirstOrDefault( x => x.Id == this.roleId );
            if( role == null )
                return PreconditionResult.FromError( "Role doesn't exist" );

            return ( await context.Guild.GetUserAsync( context.User.Id ) ).RoleIds.Contains( this.roleId ) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError( "User doesn't have required role" );
#endif
        }
    }
}
