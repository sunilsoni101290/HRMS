using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Common
{
    namespace APP.Helpers
    {
        // Lightweight, dependency-free User-Agent parser - just enough to fill
        // LoginHistory.Browser/OS/DeviceInfo without pulling in a full UA
        // parsing package. Good-enough classification, not forensic-grade.
        public static class UserAgentHelper
        {
            public static (string Browser, string OS, string DeviceInfo) Parse(string? userAgent)
            {
                if (string.IsNullOrWhiteSpace(userAgent))
                    return ("Unknown", "Unknown", "Unknown");

                string browser = "Unknown";
                string os = "Unknown";
                string deviceInfo = "Desktop";

                // ---- Browser ----
                // Order matters: Edge/Chrome/Opera UAs all contain "Safari" and
                // "Chrome" tokens, so check the more specific ones first.
                if (Regex.IsMatch(userAgent, "Edg/", RegexOptions.IgnoreCase))
                    browser = "Edge";
                else if (Regex.IsMatch(userAgent, "OPR/|Opera", RegexOptions.IgnoreCase))
                    browser = "Opera";
                else if (Regex.IsMatch(userAgent, "Firefox/", RegexOptions.IgnoreCase))
                    browser = "Firefox";
                else if (Regex.IsMatch(userAgent, "Chrome/", RegexOptions.IgnoreCase))
                    browser = "Chrome";
                else if (Regex.IsMatch(userAgent, "Safari/", RegexOptions.IgnoreCase))
                    browser = "Safari";
                else if (Regex.IsMatch(userAgent, "MSIE|Trident/", RegexOptions.IgnoreCase))
                    browser = "Internet Explorer";

                // ---- OS ----
                if (Regex.IsMatch(userAgent, "Windows NT", RegexOptions.IgnoreCase))
                    os = "Windows";
                else if (Regex.IsMatch(userAgent, "Mac OS X", RegexOptions.IgnoreCase))
                    os = "macOS";
                else if (Regex.IsMatch(userAgent, "Android", RegexOptions.IgnoreCase))
                    os = "Android";
                else if (Regex.IsMatch(userAgent, "iPhone|iPad|iOS", RegexOptions.IgnoreCase))
                    os = "iOS";
                else if (Regex.IsMatch(userAgent, "Linux", RegexOptions.IgnoreCase))
                    os = "Linux";

                // ---- Device type ----
                if (Regex.IsMatch(userAgent, "Mobi|Android.*Mobile|iPhone", RegexOptions.IgnoreCase))
                    deviceInfo = "Mobile";
                else if (Regex.IsMatch(userAgent, "iPad|Tablet", RegexOptions.IgnoreCase))
                    deviceInfo = "Tablet";
                else
                    deviceInfo = "Desktop";

                return (browser, os, deviceInfo);
            }
        }
    }
}
