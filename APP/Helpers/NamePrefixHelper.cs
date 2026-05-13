namespace APP.Helpers
{
    public static class NameHelper
    {
        /// <summary>
        /// Get first 2 letters from name
        /// Example:
        /// Sunil => SU
        /// Amit Kumar => AK
        /// </summary>
        public static string GetNamePrefix(string firstName, string lastName = "")
        {
            string prefix = "";

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                prefix += firstName.Trim()[0];
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                prefix += lastName.Trim()[0];
            }

            // If only one letter exists then take second letter from first name
            if (prefix.Length == 1 && firstName.Length > 1)
            {
                prefix += firstName.Trim()[1];
            }

            return prefix.ToUpper();
        }
    }
}
