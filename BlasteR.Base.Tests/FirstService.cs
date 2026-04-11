namespace BlasteR.Base.Tests
{
    public class FirstService : BaseService<FirstEntity>
    {
        public FirstService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}