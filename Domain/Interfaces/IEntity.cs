using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IEntity
    {
        string Id { get; set; }

        string GetKeyPrefix();
    }
}
