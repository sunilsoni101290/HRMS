using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Exceptions
{
    public class BadRequestException : Exception
    {
        public string ErrorCode { get; }

        public BadRequestException(string message, string errorCode = "BAD_REQUEST") : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
