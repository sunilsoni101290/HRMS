using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class ApiResponseLog : IEntity
    {
        // 🔹 Identification
        [Key]
        public string Id { get; set; }        // APL-2026-0001 or GUID
        public string RequestId { get; set; }         // Link with request
        public string CorrelationId { get; set; }

        // 🔹 API Info
        public string Endpoint { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Method { get; set; }

        // 🔹 Response Data
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }           // 200, 400, 500
        public bool IsSuccess { get; set; }

        // 🔹 Performance
        public long ExecutionTimeMs { get; set; }

        // 🔹 User Context
        public string UserId { get; set; }
        public string UserName { get; set; }

        // 🔹 Multi-Tenant
        public string CompanyId { get; set; }
        public string? BranchId { get; set; }

        // 🔹 Client Info
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }

        // 🔹 Time
        public DateTime ResponseTime { get; set; }

        // 🔹 Extra
        public string Remarks { get; set; }

        public virtual string GetSequencePrefix()
        {
            return "APRS"; // default
        }

        public string GetKeyPrefix()
        {
            return GetSequencePrefix();
        }
    }
}
