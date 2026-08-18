using System;

namespace BlasteR.Base.Tests
{
    public class TestFixture : IDisposable
    {
        public IUnitOfWork UnitOfWork { get; set; }

        private static object lockObject = new object();

        public TestFixture()
        {
            lock (lockObject)
            {
                UnitOfWork = DbConnectionFactory.GetInMemoryUnitOfWork("TestUser");
                TestDatabase.Initialize(UnitOfWork.DB);
            }
        }

        public void Dispose()
        {
            UnitOfWork.Dispose();
        }
    }
}
