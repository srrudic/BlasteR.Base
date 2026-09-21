using System;
using System.Data;

namespace BlasteR.Base
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection DbConnection { get; }
        IDbTransaction Transaction { get; }
        string User { get; }
        IDbTransaction GetOrBeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
        void Commit();
        void Rollback();
    }

    public class UnitOfWork : IUnitOfWork
    {
        public IDbConnection DbConnection { get; }

        public IDbTransaction Transaction { get; private set; }
        public string User { get; }

        public UnitOfWork(IDbConnection dbConnection, IDbTransaction transaction = null, string user = null)
        {
            if (dbConnection == null)
                throw new ArgumentNullException(nameof(dbConnection));

            this.DbConnection = dbConnection;
            this.Transaction = transaction;
            this.User = user;
        }

        public IDbTransaction GetOrBeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            if (Transaction != null)
                return Transaction;

            if (DbConnection.State != ConnectionState.Open)
                DbConnection.Open();

            Transaction = DbConnection.BeginTransaction(isolationLevel);

            return Transaction;
        }

        public void Commit()
        {
            if (Transaction == null)
                return;

            try
            {
                Transaction.Commit();
            }
            catch
            {
                try
                {
                    Transaction.Rollback();
                }
                catch
                {
                    // Must not mask the original exception.
                }

                throw;
            }
            finally
            {
                ClearTransaction();
            }
        }

        public void Rollback()
        {
            if (Transaction == null)
                return;

            try
            {
                Transaction.Rollback();
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
            try
            {
                Transaction?.Dispose();
            }
            catch
            {
                // Must not mask the original exception.
            }

            Transaction = null;
        }
    }
}