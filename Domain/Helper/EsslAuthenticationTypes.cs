namespace Domain.Helper
{
    /// <summary>Allowed values for EsslIntegrationSetting.AuthenticationType - kept as plain string constants (not an enum) so the value round-trips through JSON/forms without a converter, same convention as BiometricDevice.DeviceType.</summary>
    public static class EsslAuthenticationTypes
    {
        public const string Sql = "Sql";
        public const string Windows = "Windows";
    }
}
