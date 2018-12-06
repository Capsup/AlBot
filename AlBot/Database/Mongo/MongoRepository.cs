using AlBot.Database.Mongo.Extensions;
using AlBot.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Database.Mongo
{
    public class MongoRepository<T> : IRepository<T> where T : ModelBase
    {
        private UpdateDefinition<T> _updateDeleted = Builders<T>.Update.Set( "Deleted", true ).CurrentDate( "DeletedDateTime" );

        public IMongoCollection<T> collection;

        public MongoRepository( IMongoCollection<T> collection )
        {
            this.collection = collection;
        }

        protected void mutateModelBaseInfo( ModelBase item )
        {

        }

        private IEnumerable<T> _query
        {
            get
            {
                return collection.AsQueryable().Where( x => x.Deleted != true );
            }
        }

        public T Delete( string id )
        {
            return collection.FindOneAndUpdate( x => x.Id.Equals( id ), _updateDeleted );
        }

        public async Task<T> DeleteAsync( string id )
        {
            return await collection.FindOneAndUpdateAsync( ( x => x.Id.Equals( id ) ), _updateDeleted );
        }

        public T Delete( T item )
        {
            return this.Delete( item.Id );
        }

        public async Task<T> DeleteAsync( T item )
        {
            return await this.DeleteAsync( item.Id );
        }

        public void DeleteMany( string[] itemIds )
        {
            var filter = Builders<T>.Filter.In( "Id", itemIds );
            collection.UpdateMany( filter, _updateDeleted );
        }

        public async Task DeleteManyAsync( string[] itemIds )
        {
            var filter = Builders<T>.Filter.In( "Id", itemIds );
            await collection.UpdateManyAsync( filter, _updateDeleted );
        }

        public void DeleteMany( T[] items )
        {
            this.DeleteMany( items.Select( x => x.Id ).ToArray() );
        }

        public async Task DeleteManyAsync( T[] items )
        {
            await this.DeleteManyAsync( items.Select( x => x.Id ).ToArray() );
        }

        public T Insert( T item )
        {
            collection.InsertOne( item );

            return item;
        }

        public async Task<T> InsertAsync( T item )
        {
            await collection.InsertOneAsync( item );

            return item;
        }

        public void InsertMany( T[] items )
        {
            collection.InsertMany( items );
        }

        public async Task InsertManyAsync( T[] items )
        {
            await collection.InsertManyAsync( items );
        }

        public T Replace( T item, bool upsert = false )
        {
            item.ModifiedDateTime = DateTime.UtcNow;
            return collection.FindOneAndReplace<T>( x => x.Id == item.Id, item, options: new FindOneAndReplaceOptions<T>() { ReturnDocument = ReturnDocument.After, IsUpsert = upsert } );
        }

        public async Task<T> ReplaceAsync( T item, Expression<Func<T, bool>> customPrimaryKeyPredicate = null, bool upsert = false )
        {
            item.ModifiedDateTime = DateTime.UtcNow;
            if( customPrimaryKeyPredicate == null )
                customPrimaryKeyPredicate = (x => x.Id == item.Id);
            return await collection.FindOneAndReplaceAsync<T>( customPrimaryKeyPredicate, item, options: new FindOneAndReplaceOptions<T>() { ReturnDocument = ReturnDocument.After, IsUpsert = upsert } );
        }

        public void ReplaceMany( T[] items, bool upsert = false )
        {
            foreach( var item in items )
            {
                this.Replace( item, upsert );
            }
        }

        public async Task ReplaceManyAsync( T[] items, bool upsert = false )
        {
            await items.ForEachAsync( async x => await this.ReplaceAsync( x, null, upsert ) );
        }

        public IEnumerable<T> Query()
        {
            return this._query;
        }
    }
}
