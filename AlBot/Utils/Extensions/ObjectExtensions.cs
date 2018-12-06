using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AlBot.Utils.Extensions
{
    public static class ObjectExtensions
    {
        public static string FixGameName( this object type, string gameName, [CallerMemberName] string caller = null )
        {
            if( string.IsNullOrEmpty( gameName ) )
                return "";

            var finalStr = gameName;

            AliasAttribute attrib = null;
            string firstParam = null;
            if( !string.IsNullOrEmpty( ( firstParam = gameName.Split( " " )[ 0 ] ) ) 
                && ( ( attrib = type.GetType().GetMethod( caller )?.GetCustomAttribute<AliasAttribute>() )?.Aliases.Contains( firstParam ) ?? false ) )
                finalStr = finalStr.Substring( firstParam.Length + 1 );

            return finalStr.Trim('"');
        }
    }
}
