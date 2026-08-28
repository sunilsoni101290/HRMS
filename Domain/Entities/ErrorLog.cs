using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class ErrorLog : IEntity
    {
        // 🔹 Identification
        [Key]
        public string Id { get; set; }           // ERR-2026-0001 or GUID
        public string RequestId { get; set; }
        public string CorrelationId { get; set; }

        // 🔹 Error Info
        public string ErrorMessage { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
        public string InnerException { get; set; }

        // 🔹 API Context
        public string Endpoint { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Method { get; set; }

        // 🔹 Categorization (added for the Error Log management screen -
        // lets a System Configurator filter/scan by area at a glance
        // without reading the stack trace, e.g. "Biometric Device
        // Integration" vs plain "HRMS"). Inferred automatically from
        // Controller in ErrorLogService.InferModuleAndFeature - never
        // required from the caller.
        public string? ModuleName { get; set; }
        public string? FeatureName { get; set; }

        // 🔹 Request Snapshot
        public string RequestBody { get; set; }
        public string QueryParams { get; set; }

        // 🔹 User Context
        public string UserId { get; set; }
        public string UserName { get; set; }

        // 🔹 Multi-Tenant
        public string? CompanyId { get; set; }
        //public string? BranchId { get; set; }

        // 🔹 Client Info
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }

        // 🔹 Severity
        public string LogLevel { get; set; }          // Info, Warning, Error, Critical

        // 🔹 Time
        public DateTime ErrorTime { get; set; }

        // 🔹 Resolution
        public bool IsResolved { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public string ResolvedBy { get; set; }

        // 🔹 Extra
        public string Remarks { get; set; }

        public virtual string GetSequencePrefix()
        {
            return "ERR"; // default
        }

        public string GetKeyPrefix()
        {
            return GetSequencePrefix();
        }
    }
}
