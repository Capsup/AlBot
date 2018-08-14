using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlBot.Database.Mongo.Extensions
{
    public static class IQueryableExtensions
    {
        public static Task<T> FirstOrDefaultAsync<T>( this IQueryable<T> query, Expression<Func<T, bool>> predicate )
        {
            var test = query as IMongoQueryable<T>;
            if( test != null )
            {
                return test.FirstOrDefaultAsync( predicate, default( CancellationToken ) );
            }
            else
                return Task.Run( () => query.FirstOrDefault( predicate ) );
        }

        public static Task ForEachAsync<T>( this IEnumerable<T> enumerable, Func<T, Task> action )
        {
            return Task.WhenAll( enumerable.Select( x => action( x ) ) );
        }
    }
}
