using AlBot.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Database.Mongo
{
    public class MongoDatabaseFactory : IDatabaseFactory
    {
        private readonly ILogger _logger;

        public MongoDatabaseFactory( ILogger<MongoDatabaseFactory> logger )
        {
            this._logger = logger;
        }

        public IDatabase Create( string dbName, string dbUser, string dbPassword, string dbAddress, int dbPort )
        {
            try
            {
                _logger.LogTrace( $"{this.GetMethodName()}: entered" );

                if( string.IsNullOrEmpty( dbName ) )
                    throw new ArgumentException( "ERROR! dbName cannot be null or empty" );
                if( string.IsNullOrEmpty( dbUser ) )
                    throw new ArgumentException( "ERROR! dbUser cannot be null or empty" );
                if( string.IsNullOrEmpty( dbPassword ) )
                    throw new ArgumentException( "ERROR! dbPassword cannot be null or empty" );
                if( string.IsNullOrEmpty( dbAddress ) )
                    throw new ArgumentException( "ERROR! dbAddress cannot be null or empty" );
                if( dbPort <= 0 )
                    throw new ArgumentException( "ERROR! dbPort cannot be 0 or less" );

                var db = new MongoDatabase();
                _logger.LogDebug( $"{this.GetMethodName()}: connecting to database {dbName}" );
                if( !db.Connect( dbName, dbAddress, dbPort, dbUser, dbPassword ) )
                    return null;

                return db;
            }
            catch( Exception )
            {
                _logger.LogCritical( $"ERROR! Failed to connect to database" );
                return null;
            }
            finally
            {
                _logger.LogTrace( $"{this.GetMethodName()}: finished" );
            }
        }
    }
}
