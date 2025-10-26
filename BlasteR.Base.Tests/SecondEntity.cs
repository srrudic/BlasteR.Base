namespace BlasteR.Base.Tests
{
    public class SecondEntity : BaseEntity
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }

        public int? FirstEntityId { get; set; }
        public virtual FirstEntity FirstEntity { get; set; }
    }
}