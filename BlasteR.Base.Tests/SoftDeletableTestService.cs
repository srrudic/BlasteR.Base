namespace BlasteR.Base.Tests
{
    public class SoftDeletableTestService : BaseService<SoftDeletableTestEntity>
    {
        public SoftDeletableTestService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}