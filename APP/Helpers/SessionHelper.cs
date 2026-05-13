namespace APP.Helpers
{
    public static class SessionHelper
    {
        public static string GetActiveTenantId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("TenantId");

        public static string GetActiveUserId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("UserId");
    }
}
