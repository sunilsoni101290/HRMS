using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using System.Text;

namespace Domain.Entities
{
    public abstract class BaseEntity : IEntity
    {
        [Key]
        public string Id { get; set; }

        public string? TenantId { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;

        // 🔥 Override this in child classes
        public virtual string GetSequencePrefix()
        {
            return "GEN"; // default
        }

        public string GetKeyPrefix()
        {
            return GetSequencePrefix();
        }
    }
}
