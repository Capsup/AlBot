using AlBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AlBot.Database
{
    public interface IDatabase : IDisposable
    {
        bool IsConnected
        {
            get;
        }

        bool Connect( string databaseName, string ip, int port, string username, string password );
        void Disconnect();

        IRepository<T> Get<T>() where T : ModelBase;

        Task<object> Resolve( Type type, string id );
        Task<T2> Resolve<T2>( string id ) where T2 : ModelBase;

        //TODO:
        //Must be stateful but _also_ allow stateless where the programmer just gets IDatabaseConnection that has a UseDb and IRepository Get
    }
}
