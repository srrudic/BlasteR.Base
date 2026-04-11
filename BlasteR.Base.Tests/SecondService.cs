namespace BlasteR.Base.Tests
{
    public class SecondService : BaseService<SecondEntity>
    {
        public SecondService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}