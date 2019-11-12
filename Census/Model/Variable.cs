using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public enum VariableType
    {
        Boolean = 0,
        Integer = 1,
        String = 2,
        ListOfBooleans = 3,
        ListOfIntegers = 4,
        ListOfStrings = 5,
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
