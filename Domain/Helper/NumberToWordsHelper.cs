using System;
using System.Text;

namespace Domain.Helper
{
    // Converts a currency amount into Indian-numbering-system words for
    // payslip/finance documents, e.g. 73800 -> "Rupees Seventy Three
    // Thousand Eight Hundred Only." - matches the standard Indian payslip
    // convention (grouped by Thousand/Lakh/Crore, not the Western
    // Thousand/Million/Billion grouping). No equivalent existed anywhere in
    // this codebase before the Payslip redesign.
    public static class NumberToWordsHelper
    {
        private static readonly string[] Ones =
        {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
            "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

        // "Rupees <words> Only." - the exact framing used on the sample
        // payslip. Paise (if any) are appended as "and <N> Paise" before
        // "Only." Negative/zero amounts are handled defensively even though
        // a payslip's Net Pay should never realistically be negative.
        public static string ToRupeesInWords(decimal amount)
        {
            var rounded = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
            var rupees = (long)rounded;
            var paise = (int)Math.Round((rounded - rupees) * 100, MidpointRounding.AwayFromZero);

            var words = new StringBuilder("Rupees ");

            words.Append(rupees == 0 ? "Zero" : ConvertWholeNumber(rupees));

            if (paise > 0)
            {
                words.Append(" and ");
                words.Append(ConvertWholeNumber(paise));
                words.Append(" Paise");
            }

            words.Append(" Only.");

            return words.ToString();
        }

        // Splits into Crore / Lakh / Thousand / Hundred groups (Indian
        // grouping: after the first three digits, every subsequent group is
        // two digits - 1,00,00,000 not 10,000,000).
        private static string ConvertWholeNumber(long number)
        {
            if (number == 0)
                return "Zero";

            var crore = number / 10000000;
            number %= 10000000;

            var lakh = number / 100000;
            number %= 100000;

            var thousand = number / 1000;
            number %= 1000;

            var hundred = number / 100;
            var remainder = number % 100;

            var parts = new StringBuilder();

            if (crore > 0)
                parts.Append(ConvertUpToTwoDigits(crore)).Append(" Crore ");

            if (lakh > 0)
                parts.Append(ConvertUpToTwoDigits(lakh)).Append(" Lakh ");

            if (thousand > 0)
                parts.Append(ConvertUpToTwoDigits(thousand)).Append(" Thousand ");

            if (hundred > 0)
                parts.Append(Ones[hundred]).Append(" Hundred ");

            if (remainder > 0)
            {
                if (parts.Length > 0)
                    parts.Append("and ");

                parts.Append(ConvertUpToTwoDigits(remainder));
            }

            // Every word appended above already comes pre-capitalized from
            // the Ones/Tens arrays or literal strings, so no further case
            // conversion is needed - just collapse the trailing separators.
            return parts.ToString().Trim();
        }

        // Crore/Lakh/Thousand multipliers only ever carry a two-digit
        // remainder (0-99) by construction above.
        private static string ConvertUpToTwoDigits(long twoDigitNumber)
        {
            if (twoDigitNumber < 20)
                return Ones[twoDigitNumber];

            var tens = twoDigitNumber / 10;
            var ones = twoDigitNumber % 10;

            return ones == 0 ? Tens[tens] : $"{Tens[tens]} {Ones[ones]}";
        }
    }
}
