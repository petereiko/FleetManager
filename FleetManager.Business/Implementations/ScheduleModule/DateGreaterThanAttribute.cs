using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.ScheduleModule
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DateGreaterThanAttribute : ValidationAttribute, IClientModelValidator
    {
        public string OtherPropertyName { get; }

        public DateGreaterThanAttribute(string otherPropertyName)
        {
            OtherPropertyName = otherPropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var otherProp = validationContext.ObjectType.GetProperty(OtherPropertyName);
            if (otherProp == null)
            {
                return new ValidationResult($"Unknown property: {OtherPropertyName}");
            }

            var otherValue = otherProp.GetValue(validationContext.ObjectInstance);
            if (value == null || otherValue == null)
            {
                // Let [Required] handle nulls
                return ValidationResult.Success;
            }

            if (value is DateTime thisDate && otherValue is DateTime otherDate)
            {
                if (thisDate < otherDate)
                {
                    // use either your custom ErrorMessage or a default
                    var msg = FormatErrorMessage(validationContext.DisplayName);
                    return new ValidationResult(msg);
                }
            }

            return ValidationResult.Success;
        }

        // For client‑side (unobtrusive) validation
        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-dategreaterthan", ErrorMessage ?? $"{context.ModelMetadata.GetDisplayName()} must be on or after {OtherPropertyName}.");
            MergeAttribute(context.Attributes, "data-val-dategreaterthan-other", OtherPropertyName);
        }

        private bool MergeAttribute(IDictionary<string, string> attrs, string key, string value)
        {
            if (attrs.ContainsKey(key)) return false;
            attrs.Add(key, value);
            return true;
        }
    }
}
