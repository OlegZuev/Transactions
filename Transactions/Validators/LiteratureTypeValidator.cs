using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Transactions.Models;

namespace Transactions.Validators {
    public class LiteratureTypeValidator : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (!(value is BindingGroup bindingGroup) || bindingGroup.Items.Count == 0)
                return ValidationResult.ValidResult;

            if (!(bindingGroup.Items[0] is LiteratureType literatureType))
                return new ValidationResult(false, "Row doesn't exist");

            return new ValidationResult(literatureType.Validator.IsValid, null);
        }
    }
}