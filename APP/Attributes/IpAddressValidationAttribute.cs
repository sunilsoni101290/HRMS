using System.ComponentModel.DataAnnotations;
using System.Net;

namespace APP.Attributes
{
    public class IpAddressValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            string ip = value.ToString();

            if (IPAddress.TryParse(ip, out _))
                return ValidationResult.Success;

            return new ValidationResult("Invalid IP Address.");
        }
    }
}
