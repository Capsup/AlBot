using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AlBot.Utils
{
    public static class ReflectionUtils
    {
        public static string GetMethodName( this object type, [CallerMemberName] string caller = null )
        {
            return type.GetType().FullName + "." + caller + "()";
        }
    }
}
