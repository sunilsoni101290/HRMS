using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Exceptions
{
    public class ForbiddenException : Exception
    {
        public string ErrorCode { get; }

        public ForbiddenException(string message, string errorCode = "FORBIDDEN") : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
