using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Domain.Helper
{
    
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }

    public static class PasswordGenerator
    {
        private const string UPPERCASE = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LOWERCASE = "abcdefghijklmnopqrstuvwxyz";
        private const string DIGITS = "0123456789";
        private const string SPECIAL_CHARACTERS = "!@#$%^&*()-_=+?";

        private const string ALL_CHARACTERS =
            UPPERCASE + LOWERCASE + DIGITS + SPECIAL_CHARACTERS;

        public static string GeneratePassword(int passwordLength)
        {
            if (passwordLength < 4)
                throw new ArgumentException(
                    "Password length must be at least 4.");

            StringBuilder password = new StringBuilder(passwordLength);

            // Ensure at least one character from each category
            password.Append(RandomChar(UPPERCASE));
            password.Append(RandomChar(LOWERCASE));
            password.Append(RandomChar(DIGITS));
            password.Append(RandomChar(SPECIAL_CHARACTERS));

            // Fill remaining length
            for (int i = 4; i < passwordLength; i++)
            {
                password.Append(RandomChar(ALL_CHARACTERS));
            }

            // Shuffle password
            return ShuffleString(password.ToString());
        }

        private static char RandomChar(string characters)
        {
            int index = RandomNumberGenerator.GetInt32(characters.Length);

            return characters[index];
        }

        private static string ShuffleString(string input)
        {
            char[] array = input.ToCharArray();

            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);

                // Swap
                (array[i], array[j]) = (array[j], array[i]);
            }

            return new string(array);
        }

        // Generate Random Number with specific digits
        public static int GenerateRandomNumber(int digits)
        {
            if (digits <= 0)
                throw new ArgumentException(
                    "Number of digits must be positive.");

            int min = (int)Math.Pow(10, digits - 1);
            int max = (int)Math.Pow(10, digits) - 1;

            return RandomNumberGenerator.GetInt32(min, max + 1);
        }
    }
}
