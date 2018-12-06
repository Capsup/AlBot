using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Utils
{
    public static class Utils
    {
        public static string DefaultError( Discord.WebSocket.SocketUser user )
        {
            return $"Sorry <@{user.Id}>, but I don't understand that command.";
        }

        public static class ProjectionEqualityComparer
        {
            /// <summary>
            /// Creates an instance of ProjectionEqualityComparer using the specified projection.
            /// </summary>
            /// <typeparam name="TSource">Type parameter for the elements to be compared</typeparam>
            /// <typeparam name="TKey">Type parameter for the keys to be compared,
            /// after being projected from the elements</typeparam>
            /// <param name="projection">Projection to use when determining the key of an element</param>
            /// <returns>A comparer which will compare elements by projecting 
            /// each element to its key, and comparing keys</returns>
            public static ProjectionEqualityComparer<TSource, TKey> Create<TSource, TKey>( Func<TSource, TKey> projection )
            {
                return new ProjectionEqualityComparer<TSource, TKey>( projection );
            }

            /// <summary>
            /// Creates an instance of ProjectionEqualityComparer using the specified projection.
            /// The ignored parameter is solely present to aid type inference.
            /// </summary>
            /// <typeparam name="TSource">Type parameter for the elements to be compared</typeparam>
            /// <typeparam name="TKey">Type parameter for the keys to be compared,
            /// after being projected from the elements</typeparam>
            /// <param name="ignored">Value is ignored - type may be used by type inference</param>
            /// <param name="projection">Projection to use when determining the key of an element</param>
            /// <returns>A comparer which will compare elements by projecting
            /// each element to its key, and comparing keys</returns>
            public static ProjectionEqualityComparer<TSource, TKey> Create<TSource, TKey>
                ( TSource ignored,
                 Func<TSource, TKey> projection )
            {
                return new ProjectionEqualityComparer<TSource, TKey>( projection );
            }

        }

        public static class ProjectionEqualityComparer<TSource>
        {
            /// <summary>
            /// Creates an instance of ProjectionEqualityComparer using the specified projection.
            /// </summary>
            /// <typeparam name="TKey">Type parameter for the keys to be compared,
            /// after being projected from the elements</typeparam>
            /// <param name="projection">Projection to use when determining the key of an element</param>
            /// <returns>A comparer which will compare elements by projecting each element to its key,
            /// and comparing keys</returns>        
            public static ProjectionEqualityComparer<TSource, TKey> Create<TKey>( Func<TSource, TKey> projection )
            {
                return new ProjectionEqualityComparer<TSource, TKey>( projection );
            }
        }

        public class ProjectionEqualityComparer<TSource, TKey> : IEqualityComparer<TSource>
        {
            readonly Func<TSource, TKey> projection;
            readonly IEqualityComparer<TKey> comparer;

            /// <summary>
            /// Creates a new instance using the specified projection, which must not be null.
            /// The default comparer for the projected type is used.
            /// </summary>
            /// <param name="projection">Projection to use during comparisons</param>
            public ProjectionEqualityComparer( Func<TSource, TKey> projection )
                : this( projection, null )
            {
            }

            /// <summary>
            /// Creates a new instance using the specified projection, which must not be null.
            /// </summary>
            /// <param name="projection">Projection to use during comparisons</param>
            /// <param name="comparer">The comparer to use on the keys. May be null, in
            /// which case the default comparer will be used.</param>
            public ProjectionEqualityComparer( Func<TSource, TKey> projection, IEqualityComparer<TKey> comparer )
            {
                if( projection == null )
                {
                    throw new ArgumentNullException( "projection" );
                }
                this.comparer = comparer ?? EqualityComparer<TKey>.Default;
                this.projection = projection;
            }

            /// <summary>
            /// Compares the two specified values for equality by applying the projection
            /// to each value and then using the equality comparer on the resulting keys. Null
            /// references are never passed to the projection.
            /// </summary>
            public bool Equals( TSource x, TSource y )
            {
                if( x == null && y == null )
                {
                    return true;
                }
                if( x == null || y == null )
                {
                    return false;
                }
                return comparer.Equals( projection( x ), projection( y ) );
            }

            /// <summary>
            /// Produces a hash code for the given value by projecting it and
            /// then asking the equality comparer to find the hash code of
            /// the resulting key.
            /// </summary>
            public int GetHashCode( TSource obj )
            {
                if( obj == null )
                {
                    throw new ArgumentNullException( "obj" );
                }
                return comparer.GetHashCode( projection( obj ) );
            }
        }
    }
}
