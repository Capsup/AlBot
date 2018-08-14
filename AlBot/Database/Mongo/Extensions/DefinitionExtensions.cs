using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Database.Mongo.Extensions
{
    public static class DefinitionExtensions
    {
        public static BsonDocument RenderToBsonDocument<T>( this FilterDefinition<T> filter )
        {
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            return filter.Render( documentSerializer, serializerRegistry );
        }

        public static BsonDocument RenderToBsonDocument<T>( this UpdateDefinition<T> update )
        {
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            return update.Render( documentSerializer, serializerRegistry );
        }
    }
}
