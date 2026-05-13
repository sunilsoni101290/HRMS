using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.ErrorLog
{
    public interface IErrorLogService
    {
        Task LogExceptionAsync(Exception ex, HttpContext context, string requestId = null);
    }
}
