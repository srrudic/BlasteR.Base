using System;
using System.ComponentModel.DataAnnotations;

namespace BlasteR.Base
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        public BaseEntity()
        {
            Id = 0;
            CreatedAt = DateTime.Now;
            ModifiedAt = null;
        }
    }
    
    public abstract class SoftDeletableEntity : BaseEntity
    {
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
