using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V4
{
    public class AtLeastOneRequiredAttribute : ValidationAttribute
    {
        private readonly string[] _propertyNames;

        public AtLeastOneRequiredAttribute(params string[] propertyNames)
        {
            _propertyNames = propertyNames;
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            foreach (var prop in _propertyNames)
            {
                var propertyInfo = validationContext.ObjectType.GetProperty(prop);
                if (propertyInfo != null)
                {
                    var propValue = propertyInfo.GetValue(validationContext.ObjectInstance, null) as string;
                    if (!string.IsNullOrWhiteSpace(propValue))
                        return ValidationResult.Success;
                }
            }
            return new ValidationResult($"At least one of the following must be supplied: {string.Join(", ", _propertyNames)}");
        }
    }
}
