using AlBot.Models;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Database.Mongo
{
    public class MongoDatabase : IDatabase
    {
        private IMongoDatabase database;

        private MongoClient _client;
        protected MongoClient Client
        {
            get
            {
                return _client;
            }
            private set
            {
                _client = value;
            }
        }

        public bool IsConnected
        {
            get
            {
                //TODO: Figure out a proper run-time check for mongodb. Maybe create a ping command to the test database since we know it's always there?
                return Client == null;
            }
        }

        static MongoDatabase()
        {
            MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<ModelBase>( cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty( c => c.Id )
                .SetIdGenerator( MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance )
                .SetSerializer( new MongoDB.Bson.Serialization.Serializers.StringSerializer( MongoDB.Bson.BsonType.ObjectId ) );
            } );

            ConventionRegistry.Register( "Ignore null values", new ConventionPack() { new IgnoreIfNullConvention( true ), new IgnoreExtraElementsConvention( true ) }, t => true );
        }

        public bool Connect( string databaseName, string ip, int port, string username, string password )
        {
            var settings = new MongoClientSettings()
            {
                Server = new MongoServerAddress( ip, port ),
                Credential = MongoCredential.CreateCredential( databaseName, username, password )
            };

            Client = new MongoClient( settings );
            database = Client.GetDatabase( databaseName );

            return true;
        }

        public void Disconnect()
        {
            Dispose();
        }

        public void Dispose()
        {
            database = null;
            Client = null;
        }

        public IRepository<T> Get<T>() where T : ModelBase
        {
            if( database == null )
                throw new InvalidOperationException( "ERROR! Cannot get repository when no database is set" );

            return new MongoRepository<T>( database.GetCollection<T>( typeof( T ).Name ) );
        }

        public async Task<object> Resolve( Type type, string id )
        {
            Task<ModelBase> res = (Task<ModelBase>) ( (Func<string, Task<ModelBase>>) Resolve<ModelBase> ).Method.GetGenericMethodDefinition().MakeGenericMethod( type ).Invoke( this, new[] { id } );
            return await res;
        }

        public async Task<T2> Resolve<T2>( string id ) where T2 : ModelBase
        {
            if( string.IsNullOrEmpty( id ) )
                throw new ArgumentException( "ERROR! id cannot be null or empty" );

            return await ( ( new MongoRepository<T2>( database.GetCollection<T2>( typeof( T2 ).Name ) ).Query() ) as IMongoQueryable<T2> ).FirstOrDefaultAsync( x => x.Id == id );
        }
    }
}
