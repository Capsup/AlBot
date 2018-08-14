using System;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Database
{
    public interface IDatabaseFactory
    {
        //IDatabase Create( string dbName, string dbUser, string dbPassword );
        IDatabase Create( string dbName, string dbUser, string dbPassword, string dbAddress, int dbPort );
    }
}
