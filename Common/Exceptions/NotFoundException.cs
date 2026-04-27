using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public string ErrorCode { get; }

        public NotFoundException(string message, string errorCode = "NOT_FOUND") : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
