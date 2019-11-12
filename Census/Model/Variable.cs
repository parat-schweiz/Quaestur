using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public enum VariableModification
    {
        None = 0,
        Set = 1,
        And = 2,
        Or = 3,
        Xor = 4,
        Add = 5,
        Subtract = 6,
        Multiply = 7,
        Divide = 8,
        Append = 9,
        AddToList = 10,
        RemoveFromList = 11,
    }

    public static class VariableModificationExtensions
    {
        public static string Translate(this VariableModification modification, Translator translator)
        {
            switch (modification)
            {
                case VariableModification.None:
                    return translator.Get("Enum.VariableModification.None", "None value in the variable modification enum", "None");
                case VariableModification.Set:
                    return translator.Get("Enum.VariableModification.Set", "Set value in the variable modification enum", "Set");
                case VariableModification.And:
                    return translator.Get("Enum.VariableModification.And", "And value in the variable modification enum", "And");
                case VariableModification.Or:
                    return translator.Get("Enum.VariableModification.Or", "Or value in the variable modification enum", "Or");
                case VariableModification.Xor:
                    return translator.Get("Enum.VariableModification.Xor", "Xor value in the variable modification enum", "Xor");
                case VariableModification.Add:
                    return translator.Get("Enum.VariableModification.Add", "Add value in the variable modification enum", "Add");
                case VariableModification.Subtract:
                    return translator.Get("Enum.VariableModification.Subtract", "Subtract value in the variable modification enum", "Subtract");
                case VariableModification.Multiply:
                    return translator.Get("Enum.VariableModification.Multiply", "Multiply value in the variable modification enum", "Multiply");
                case VariableModification.Divide:
                    return translator.Get("Enum.VariableModification.Divide", "Divide value in the variable modification enum", "Divide");
                case VariableModification.Append:
                    return translator.Get("Enum.VariableModification.Append", "Append value in the variable modification enum", "Append");
                case VariableModification.AddToList:
                    return translator.Get("Enum.VariableModification.AddToList", "Add to list value in the variable modification enum", "Add to list");
                case VariableModification.RemoveFromList:
                    return translator.Get("Enum.VariableModification.RemoveFromList", "Remove from list value in the variable modification enum", "Remove from list");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum VariableType
    {
        Boolean = 0,
        Integer = 1,
        Double = 2,
        String = 3,
        ListOfBooleans = 4,
        ListOfIntegers = 5,
        ListOfDouble = 6,
        ListOfStrings = 7,
    }

    public static class VariableTypeExtensions
    {
        public static string Translate(this VariableType type, Translator translator)
        {
            switch (type)
            {
                case VariableType.Boolean:
                    return translator.Get("Enum.VariableType.Boolean", "Boolean value in the variable type enum", "Boolean");
                case VariableType.Integer:
                    return translator.Get("Enum.VariableType.Integer", "Integer value in the variable type enum", "Integer");
                case VariableType.Double:
                    return translator.Get("Enum.VariableType.Double", "Double value in the variable type enum", "Double");
                case VariableType.String:
                    return translator.Get("Enum.VariableType.String", "String value in the variable type enum", "String");
                case VariableType.ListOfBooleans:
                    return translator.Get("Enum.VariableType.ListOfBooleans", "List of booleans value in the variable type enum", "List of booleans");
                case VariableType.ListOfIntegers:
                    return translator.Get("Enum.VariableType.ListOfIntegers", "List of integers value in the variable type enum", "List of integers");
                case VariableType.ListOfStrings:
                    return translator.Get("Enum.VariableType.ListOfStrings", "List of strings value in the variable type enum", "List of strings");
                default:
                    throw new NotSupportedException(); 
            }
        }
    }

    public class Variable : DatabaseObject
    {
        public ForeignKeyField<Questionaire, Variable> Questionaire { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public EnumField<VariableType> Type { get; private set; }

        public Variable() : this(Guid.Empty)
        {
        }

		public Variable(Guid id) : base(id)
        {
            Questionaire = new ForeignKeyField<Questionaire, Variable>(this, "questionaireid", false, q => q.Variables);
            Name = new MultiLanguageStringField(this, "text");
            Type = new EnumField<VariableType>(this, "type", VariableType.Boolean, VariableTypeExtensions.Translate);
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string ToString()
        {
            return Questionaire.Value.ToString() + " / " + Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Questionaire.Value.GetText(translator) + " / " + Name.Value[translator.Language];
        }

        public Group Owner
        {
            get
            {
                return Questionaire.Value.Owner;
            }
        }

    }
}
