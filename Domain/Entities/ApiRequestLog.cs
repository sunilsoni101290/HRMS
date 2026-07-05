using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class ApiRequestLog : IEntity
    {
        // 🔹 Identification
        [Key]
        public string Id { get; set; }          // Unique ID (GUID or AL-2026-0001)
        public string CorrelationId { get; set; }      // For tracing multiple calls

        // 🔹 API Info
        public string Endpoint { get; set; }           // /api/employee/create
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Method { get; set; }             // GET, POST, PUT, DELETE

        // 🔹 Request Data
        public string RequestBody { get; set; }
        public string QueryParams { get; set; }
        public string Headers { get; set; }

        // 🔹 Response Data
        public string ResponseBody { get; set; }
        public int? StatusCode { get; set; }           // 200, 400, 500

        // 🔹 Performance
        public long? ExecutionTimeMs { get; set; }     // Time taken in milliseconds

        // 🔹 User Context
        public string UserId { get; set; }
        public string UserName { get; set; }

        // 🔹 Multi-Tenant Context
        public string CompanyId { get; set; }
        public string? BranchId { get; set; }

        // 🔹 Client Info
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }          // Browser / Device
        public string Device { get; set; }             // Mobile/Web/API

        // 🔹 Error Handling
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ExceptionStackTrace { get; set; }

        // 🔹 Security / Tracking
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }              // (optional - masked)

        // 🔹 Time Info
        public DateTime RequestTime { get; set; }
        public DateTime? ResponseTime { get; set; }

        // 🔹 Extra
        public string Remarks { get; set; }

        public virtual string GetSequencePrefix()
        {
            return "APRQ"; // default
        }

        public string GetKeyPrefix()
        {
            return GetSequencePrefix();
        }


    }
}
