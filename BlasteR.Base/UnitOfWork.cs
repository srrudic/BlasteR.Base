using System;
using System.Data;

namespace BlasteR.Base
{
    public interface IUnitOfWork : IDisposable
    {
        string User { get; }
        IDbConnection DbConnection { get; }
        IDbTransaction GetOrBeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
        void Commit();
        void Rollback();
    }

    public class UnitOfWork : IUnitOfWork
    {
        public string User { get; private set; }

        public IDbConnection DbConnection { get; private set; }

        private IDbTransaction transaction;

        public UnitOfWork(IDbConnection dbConnection, IDbTransaction transaction = null, string user = null)
        {
            if (dbConnection == null)
                throw new ArgumentNullException(nameof(dbConnection));

            this.DbConnection = dbConnection;
            this.transaction = transaction;
        }

        public IDbTransaction GetOrBeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            if (transaction != null)
                return transaction;

            transaction = DbConnection.BeginTransaction(isolationLevel);

            return transaction;
        }

        public void Commit()
        {
            try
            {
                transaction?.Commit();
            }
            finally
            {
                ClearTransaction();
            }
        }

        public void Rollback()
        {
            try
            {
                transaction?.Rollback();
            }
            finally
            {
                ClearTransaction();
            }
        }

        public void Dispose()
        {
            ClearTransaction();
            DbConnection?.Dispose();
        }

        private void ClearTransaction()
        {
            transaction?.Dispose();
            transaction = null;
        }
    }
}