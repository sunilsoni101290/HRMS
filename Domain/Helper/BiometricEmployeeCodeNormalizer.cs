using System;

namespace Domain.Helper
{
    /// <summary>
    /// Single, shared normalization rule for matching a raw biometric-device
    /// employee code (eTimeTrackLite1's DeviceLogs.UserId, or an agent-pushed
    /// BiometricAttendanceLog.EmployeeCode) against
    /// EmployeeBiometricMapping.BiometricEmployeeCode.
    ///
    /// ROOT-CAUSE FIX (2671 eSSL source rows -&gt; only 303 rows ever reaching
    /// Attendance): every mapping lookup in this codebase
    /// (EsslAttendanceSyncService.SyncAsync's isMapped check,
    /// AttendanceProcessorService.ProcessAttendanceAsync's mappingByCode
    /// lookup, and EsslAttendanceSyncService.GetUnmappedEmployeesCoreAsync's
    /// activeCodeSet check) used a plain C# Dictionary/HashSet&lt;string&gt;,
    /// which compares with ordinal, case-sensitive, whitespace-sensitive
    /// equality. EmployeeBiometricMappingService never trimmed or
    /// case-normalized BiometricEmployeeCode on save either. Real eSSL/HRMS
    /// deployments routinely have a device-side UserId of "0260123" or
    /// "260123 " or "essl260123" while the admin-typed mapping is "260123" -
    /// every one of those previously failed the exact-match lookup silently,
    /// leaving the punch permanently IsProcessed=false (retried every run,
    /// never fixed by retrying, since the comparison never changes) and
    /// invisible anywhere except a LogWarning line.
    ///
    /// This normalization (trim + invariant-culture uppercase) must be
    /// applied at BOTH ends of every comparison - when
    /// EmployeeBiometricMappingService saves BiometricEmployeeCode AND when
    /// any sync/processing code looks a raw code up - or it has no effect.
    /// It intentionally does NOT strip leading zeros or other punctuation:
    /// eSSL UserId values are sometimes genuinely alphanumeric (not purely
    /// numeric device codes), so guessing at a stronger transformation risks
    /// silently merging two different real employee codes. Trim+uppercase
    /// only removes accidental formatting noise, never changes which digits/
    /// letters are considered part of the code.
    /// </summary>
    public static class BiometricEmployeeCodeNormalizer
    {
        public static string Normalize(string? code)
            => string.IsNullOrWhiteSpace(code) ? "" : code.Trim().ToUpperInvariant();
    }
}
