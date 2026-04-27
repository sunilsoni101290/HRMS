using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public string ErrorCode { get; }

        public UnauthorizedException(string message, string errorCode = "UNAUTHORIZED") : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
