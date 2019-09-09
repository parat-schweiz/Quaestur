using System;

namespace Census
{
    public static class Variables
    {
        public static bool IsBoolValue(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "yes":
                case "no":
                case "true":
                case "false":
                case "1":
                case "0":
                    return true;
                default:
                    return false;
            }
        }

        public static void CheckValue(PostStatus status, VariableType type, string value, string field)
        {
            switch (type)
            {
                case VariableType.Boolean:
                case VariableType.ListOfBooleans:
                    if (!IsBoolValue(value))
                    {
                        status.SetValidationError(field, "Variable.Edit.Validation.ValueMustBeBoolean", "Value must represent a boolean when modification involves boolean variable is set in the variable or option edit dialog", "Value must represent a boolean");
                    }
                    break;
                case VariableType.Integer:
                case VariableType.ListOfIntegers:
                    if (!int.TryParse(value, out int dummy))
                    {
                        status.SetValidationError(field, "Variable.Edit.Validation.ValueMustBeInteger", "Value must represent an integer when modification involves boolean variable is set in the variable or option edit dialog", "Value must represent an integer");
                    }
                    break;
                case VariableType.Double:
                case VariableType.ListOfDouble:
                    if (!double.TryParse(value, out double dummy2))
                    {
                        status.SetValidationError(field, "Variable.Edit.Validation.ValueMustBeDouble", "Value must represent a floating point number when modification involves boolean variable is set in the variable or option edit dialog", "Value must represent a floating point number");
                    }
                    break;
                case VariableType.String:
                case VariableType.ListOfStrings:
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
