using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.JWT_TOKEN
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user, IList<string> roles);
    }
}
