using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Npgsql;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Publicus
{
    public abstract class Field : IEquatable<Field>
    {
        public bool Dirty { get; protected set; }

        public abstract void Validate();

        public abstract Type BaseType { get; }

        public string ColumnName { get; private set; }

        public virtual Type ReferencedType { get { return null; } }

        public bool Nullable { get; private set; }

        public virtual bool IsPrimaryKey { get { return false; } }

        public string VariableName { get { return "@" + ColumnName; } }

        public abstract void AddValue(NpgsqlCommand command);

        public abstract void Updated();

        public abstract void Read(NpgsqlDataReader reader);

        public virtual IEnumerable<SingleCascade> CascadeLoad() { return new SingleCascade[0]; }

        public virtual IEnumerable<DatabaseObject> CascadeUpdate() { return new DatabaseObject[0]; }

        protected Exception Except(string text, params object[] parameters)
        {
            return new Exception(string.Format(text, parameters));
        }

        public abstract bool Equals(Field other);

        protected Field(DatabaseObject obj, string columnName, bool nullable)
        {
            ColumnName = columnName;
            Nullable = nullable;
            obj.Fields.Add(this);
        }

        public abstract string GetText(Translator translator);
    }

    public abstract class ValueField<T> : Field
    {
        protected ValueField(DatabaseObject obj, string columnName, bool nullable)
            : base(obj, columnName, nullable)
        {
        }

        public abstract T Value { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ValueField<T> brother)
            {
                return brother.Value.Equals(Value);
            }
            else if (obj is T value)
            {
                return Value.Equals(value); 
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(Field other)
        {
            if (other is ValueField<T> brother)
            {
                return brother.Value.Equals(Value);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return 2342 + Value.GetHashCode();
        }

        public static bool operator ==(ValueField<T> a, T b)
        {
            if (ReferenceEquals(a, null))
            {
                return false;
            }
            else if (ReferenceEquals(a.Value, null))
            {
                return ReferenceEquals(b, null);
            }
            else
            {
                return a.Value.Equals(b);
            }
        }

        public static bool operator !=(ValueField<T> a, T b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            if (ReferenceEquals(Value, null))
            {
                return "<null>";
            }
            else
            {
                return Value.ToString();
            }
        }

        public override string GetText(Translator translator)
        {
            if (ReferenceEquals(Value, null))
            {
                return translator.Get("Value.Null", "Null value at generic text of some field", "Empty");
            }
            else
            {
                return Value.ToString();
            }
        }
    }

    public abstract class ProtoField<T> : ValueField<T>
    {
        protected T _value;

        public override T Value
        {
            get
            {
                return _value;
            }
            set
            {
                Dirty = true;
                _value = value;
            }
        }

        public override Type BaseType => throw new NotImplementedException();

        public override void AddValue(NpgsqlCommand command)
        {
            command.AddParam(VariableName, _value);
        }

        public override void Updated()
        {
            Dirty = false;
        }

        public override void Read(NpgsqlDataReader reader)
        {
            _value = (T)reader[ColumnName];
            Dirty = false;
        }

        public static implicit operator T(ProtoField<T> field)
        {
            return field.Value;
        }

        public ProtoField(DatabaseObject obj, string columnName, bool nullable)
            : base(obj, columnName, nullable)
        {
        }
    }

    public class Field<T> : ValueField<T> where T : struct
    {
        protected T _value;

        public override T Value
        {
            get
            {
                return _value;
            }
            set
            {
                Dirty = true;
                _value = value;
            }
        }

        public override Type BaseType
        {
            get { return typeof(T); } 
        }

        public override void AddValue(NpgsqlCommand command)
        {
            command.AddParam(VariableName, _value);
        }

        public override void Updated()
        {
            Dirty = false;
        }

        public override void Read(NpgsqlDataReader reader)
        {
            _value = (T)reader[ColumnName];
            Dirty = false;
        }

        public static implicit operator T(Field<T> field)
        {
            return field.Value;
        }

        public override void Validate()
        {
        }

        public Field(DatabaseObject obj, string columnName, T defaultValue)
            : base(obj, columnName, false)
        {
            _value = defaultValue;
        }
    }

    public class FieldNull<T> : ValueField<T?> where T : struct
    {
        protected T? _value;

        public override T? Value
        {
            get
            {
                return _value;
            }
            set
            {
                Dirty = true;
                _value = value;
            }
        }

        public override Type BaseType
        {
            get { return typeof(T); }
        }

        public override void AddValue(NpgsqlCommand command)
        {
            if (_value == null)
            {
                command.AddParam(VariableName, DBNull.Value);
            }
            else
            {
                command.AddParam(VariableName, _value);
            }
        }

        public override void Updated()
        {
            Dirty = false;
        }

        public override void Read(NpgsqlDataReader reader)
        {
            if (reader[ColumnName] is DBNull)
            {
                _value = null;
            }
            else
            {
                _value = (T)reader[ColumnName];
            }

            Dirty = false;
        }

        public static implicit operator T?(FieldNull<T> field)
        {
            return field.Value;
        }

        public override void Validate()
        {
        }

        public FieldNull(DatabaseObject obj, string columnName)
            : base(obj, columnName, true)
        {
            _value = null;
        }
    }

    public class FieldClass<T> : ValueField<T> where T : class
    {
        protected T _value;

        public override T Value
        {
            get
            {
                return _value;
            }
            set
            {
                Dirty = true;
                _value = value;
            }
        }

        public override Type BaseType
        {
            get { return typeof(T); }
        }

        public override void AddValue(NpgsqlCommand command)
        {
            if (_value == null)
            {
                command.AddParam(VariableName, DBNull.Value);
            }
            else
            {
                command.AddParam(VariableName, _value);
            }
        }

        public override void Updated()
        {
            Dirty = false;
        }

        public override void Read(NpgsqlDataReader reader)
        {
            if (reader[ColumnName] is DBNull)
            {
                _value = null;
            }
            else
            {
                _value = (T)reader[ColumnName];
            }

            Dirty = false;
        }

        public static implicit operator T (FieldClass<T> field)
        {
            return field.Value;
        }

        public override void Validate()
        {
            if ((_value == null) && (!Nullable))
            {
                throw Except("Field {0} must not be null.", ColumnName);
            }
        }

        public FieldClass(DatabaseObject obj, string columnName, bool nullable)
            : base(obj, columnName, nullable)
        {
            _value = null;
        }
    }

    public class ReadOnlyField<T> : ValueField<T>
    {
        protected T _value;

        public override T Value
        {
            get
            {
                return _value;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override Type BaseType
        {
            get { return typeof(T); }
        }

        public override void AddValue(NpgsqlCommand command)
        {
            command.AddParam(VariableName, _value);
        }

        public override void Updated()
        {
        }

        public override void Read(NpgsqlDataReader reader)
        {
            _value = (T)reader[ColumnName];
            Dirty = false;
        }

        public static implicit operator T(ReadOnlyField<T> field)
        {
            return field.Value;
        }

        public override void Validate()
        {
        }

        public ReadOnlyField(DatabaseObject obj, string columnName, T defaultValue)
            : base(obj, columnName, false)
        {
            _value = defaultValue;
        }
    }

    public enum AllowStringType
    {
        SimpleText,
        ParameterizedText,
        SafeLatex,
        SafeHtml,
        UnsecureText,
    }

    public class StringField : ProtoField<string>
    {
        public AllowStringType AllowType { get; private set; }

        public override void Validate()
        {
            if (_value == null)
            {
                throw Except("String {0} must not be null.", ColumnName);
            }
            else if (_value.Length > Size)
            {
                throw Except("String {0} must not be longer than {1}.", ColumnName, Size);
            }
        }

        public override Type BaseType
        {
            get { return typeof(string); }
        }

        public override string Value
        {
            get { return base.Value; }
            set
            {
                if (value == null)
                {
                    throw new NotSupportedException(); 
                }

                switch (AllowType)
                {
                    case AllowStringType.SimpleText:
                        base.Value = value.RemoveHtml().RemoveParameters();
                        break;
                    case AllowStringType.ParameterizedText:
                        base.Value = value.RemoveHtml();
                        break;
                    case AllowStringType.SafeHtml:
                        base.Value = value.SafeHtml();
                        break;
                    case AllowStringType.SafeLatex:
                        base.Value = value.SafeLatex();
                        break;
                    case AllowStringType.UnsecureText:
                        base.Value = value;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public int Size { get; private set; }

        public StringField(DatabaseObject obj, string columnName, int size, AllowStringType allowType = AllowStringType.SimpleText)
            : base(obj, columnName, false)
        {
            _value = string.Empty;
            AllowType = allowType;
            Size = size;
        }
    }

    public class StringNullField : ProtoField<string>
    {
        public AllowStringType AllowType { get; private set; }

        public int Size { get; private set; }

        public override string Value
        {
            get { return base.Value; }
            set
            {
                if (value == null)
                {
                    base.Value = null;
                }
                else
                {
                    switch (AllowType)
                    {
                        case AllowStringType.SimpleText:
                            base.Value = value.RemoveHtml().RemoveParameters();
                            break;
                        case AllowStringType.ParameterizedText:
                            base.Value = value.RemoveHtml();
                            break;
                        case AllowStringType.SafeHtml:
                            base.Value = value.SafeHtml();
                            break;
                        case AllowStringType.SafeLatex:
                            base.Value = value.SafeLatex();
                            break;
                        case AllowStringType.UnsecureText:
                            base.Value = value;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }

        public override void Validate()
        {
            if (_value != null &&
                _value.Length > Size)
            {
                throw Except("String {0} must not be longer than {1}.", ColumnName, Size);
            }
        }

        public override void AddValue(NpgsqlCommand command)
        {
            if (_value == null)
            {
                command.AddParam(VariableName, DBNull.Value);
            }
            else
            {
                command.AddParam(VariableName, _value);
            }
        }

        public override void Read(NpgsqlDataReader reader)
        {
            if (reader[ColumnName] is DBNull)
            {
                _value = null;
            }
            else
            {
                _value = (string)reader[ColumnName];
            }

            Dirty = false;
        }

        public override Type BaseType
        {
            get { return typeof(string); }
        }

        public StringNullField(DatabaseObject obj, string columnName, int size, AllowStringType allowType = AllowStringType.SimpleText)
            : base(obj, columnName, true)
        {
            AllowType = allowType;
            _value = null;
            Size = size;
        }
    }

    public class DecimalField : ProtoField<decimal>
    {
        public int Precision { get; private set; }
        public int Scale { get; private set; }

        public override Type BaseType
        {
            get { return typeof(decimal); }
        }

        public override void Validate()
        {
        }

        public DecimalField(DatabaseObject obj, string columnName, int precision, int scale)
            : base(obj, columnName, false)
        {
            _value = 0m;
            Precision = precision;
            Scale = scale;
        }
    }

    public class DecimalNullField : ProtoField<decimal?>
    {
        public int Precision { get; private set; }
        public int Scale { get; private set; }

        public override Type BaseType
        {
            get { return typeof(decimal); }
        }

        public override void Validate()
        {
        }

        public DecimalNullField(DatabaseObject obj, string columnName, int precision, int scale)
            : base(obj, columnName, true)
        {
            _value = null;
            Precision = precision;
            Scale = scale;
        }
    }
    
    public class GuidIdPrimaryKeyField : ReadOnlyField<Guid>
    {
        public GuidIdPrimaryKeyField(DatabaseObject obj, Guid id)
            : base(obj, "id", id)
        {
        }
    }

    public class EnumField<T> : ValueField<T> where T : struct, IConvertible
    {
        private T _value;
        private Func<T, Translator, string> _translate;

        public override T Value
        {
            get 
            {
                return _value;
            }
            set
            {
                _value = value;
                Dirty = true;
            }
        }

        public override Type BaseType
        {
            get { return typeof(int); }
        }

        public override void Validate()
        {
        }

        public EnumField(DatabaseObject obj, string columnName, T defaultValue, Func<T, Translator, string> translate)
            : base(obj, columnName, false)
        {
            _value = defaultValue;
            _translate = translate;
        }

        public override string GetText(Translator translator)
        {
            return _translate(Value, translator);
        }

        public override void AddValue(NpgsqlCommand command)
        {
            command.AddParam(VariableName, (int)(object)_value);
        }

        public override void Read(NpgsqlDataReader reader)
        {
            _value = (T)(object)(int)reader[ColumnName];
        }

        public override void Updated()
        {
            Dirty = false;
        }
    }

    public class EnumNullField<T> : ValueField<T?> where T : struct, IConvertible
    {
        private T? _value;
        private Func<T, Translator, string> _translate;

        public override T? Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                Dirty = true;
            }
        }

        public override Type BaseType
        {
            get { return typeof(int); }
        }

        public override void Validate()
        {
        }

        public override string GetText(Translator translator)
        {
            if (!Value.HasValue)
            {
                return translator.Get("Value.Null", "Null value at generic text of some field", "Empty");
            }
            else
            {
                return _translate(Value.Value, translator);
            }
        }

        public EnumNullField(DatabaseObject obj, string columnName, Func<T, Translator, string> translate)
            : base(obj, columnName, true)
        {
            _value = null;
            _translate = translate;
        }

        public override void AddValue(NpgsqlCommand command)
        {
            if (_value.HasValue)
            {
                command.AddParam(VariableName, (int)(object)_value);
            }
            else
            {
                command.AddParam(VariableName, DBNull.Value);
            }
        }

        public override void Read(NpgsqlDataReader reader)
        {
            if (reader[ColumnName] is DBNull)
            {
                _value = null;
            }
            else
            {
                _value = (T)(object)(int)reader[ColumnName];
            }
        }

        public override void Updated()
        {
            Dirty = false;
        }
    }

    public class ForeignKeyField<T, P> : ProtoField<T>
        where T : DatabaseObject, new()
        where P : DatabaseObject, new()
    {
        private Guid? _preLoadId;
        private Func<T, List<P>> _getList;
        private P _parent;

        public ForeignKeyField(P obj, string columnName, bool nullable, Func<T, List<P>> getList)
            : base(obj, columnName, nullable)
        {
            _parent = obj;
            _getList = getList;
        }

        public override string GetText(Translator translator)
        {
            if (Value == null)
            {
                return translator.Get("Value.Null", "Null value at generic text of some field", "None");
            }
            else
            {
                return Value.GetText(translator);
            }
        }

        public override T Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_getList != null && _value != null && _getList(_value).Contains(_parent))
                {
                    _getList(_value).Remove(_parent);
                }

                _value = value;
                Dirty = true;

                if (_getList != null && _value != null && !_getList(_value).Contains(_parent))
                {
                    _getList(_value).Add(_parent); 
                }
            }
        }

        public override Type ReferencedType
        {
            get { return typeof(T); }
        }

        public override Type BaseType
        {
            get { return typeof(Guid); }
        }

        public override void Validate()
        {
            if ((_value == null) && (!Nullable))
            {
                throw Except("Reference {0} must not be null.", ColumnName);
            }
        }

        public override void AddValue(NpgsqlCommand command)
        {
            if (_value == null)
            {
                command.AddParam(VariableName, DBNull.Value);
            }
            else
            {
                command.AddParam(VariableName, _value.Id.Value); 
            }
        }

        public override void Read(NpgsqlDataReader reader)
        {
            if (reader[ColumnName] is DBNull)
            {
                _preLoadId = null;
            }
            else
            {
                _preLoadId = (Guid)reader[ColumnName];
            }
        }

        public override IEnumerable<SingleCascade> CascadeLoad()
        {
            if (_preLoadId.HasValue)
            {
                yield return new SingleCascade<T>(_preLoadId.Value, o =>
                {
                    _value = o;
                    _preLoadId = null;
                    Dirty = false;
                });
            }
        }

        public override IEnumerable<DatabaseObject> CascadeUpdate()
        {
            if (_value != null)
            {
                yield return _value; 
            }
        }
    }

    public class MultiLanguageStringField : FieldClass<MultiLanguageString>
    {
        public MultiLanguageStringField(DatabaseObject obj, string columnName)
            : base(obj, columnName, false)
        {
            _value = new MultiLanguageString();
        }

        public override void AddValue(NpgsqlCommand command)
        {
            command.AddParam(VariableName, _value.ToJson().ToString());
        }

        public override void Read(NpgsqlDataReader reader)
        {
            _value = new MultiLanguageString((string)reader[ColumnName]);
        }

        public override Type BaseType
        {
            get { return typeof(string); }
        }
    }

    public class StringListField : FieldClass<IEnumerable<string>>
    {
        public AllowStringType AllowType { get; private set; }

        public StringListField(DatabaseObject obj, string columnName, AllowStringType allowType = AllowStringType.SimpleText)
            : base(obj, columnName, false)
        {
            AllowType = allowType;
            _value = new List<string>();
        }

        private string Sanatize(string value)
        {
            switch (AllowType)
            {
                case AllowStringType.SimpleText:
                    return value.RemoveHtml().RemoveParameters();
                case AllowStringType.ParameterizedText:
                    return value.RemoveHtml();
                case AllowStringType.SafeHtml:
                    return value.SafeHtml();
                case AllowStringType.SafeLatex:
                    return value.SafeLatex();
                default:
                    throw new NotSupportedException();
            } 
        }

        public override IEnumerable<string> Value
        {
            get { return base.Value; }
            set
            {
                var list = new List<string>();

                foreach (var v in value)
                {
                    list.Add(Sanatize(v)); 
                }

                base.Value = list;
            }
        }

        public override void AddValue(NpgsqlCommand command)
        {
            var array = new JArray(_value.ToArray());
            command.AddParam(VariableName, array.ToString());
        }

        public override void Read(NpgsqlDataReader reader)
        {
            var array = JArray.Parse((string)reader[ColumnName]);
            _value = array.Values<string>().ToList();
        }

        public override Type BaseType
        {
            get { return typeof(string); }
        }
    }

    public class MultiLanguageString
    {
        private Dictionary<Language, string> _values;

        public MultiLanguageString()
        {
            _values = new Dictionary<Language, string>();
        }

        public MultiLanguageString(string jsonData)
        {
            _values = new Dictionary<Language, string>();

            try
            {
                Assign(JArray.Parse(jsonData));
            }
            catch
            {
                _values.Add(Language.German, jsonData);
            }
        }

        public MultiLanguageString(JArray array)
        {
            Assign(array);
        }

        public void Assign(JArray array)
        {
            _values = new Dictionary<Language, string>();

            foreach (JObject obj in array)
            {
                var language = (Language)(int)obj["language"];
                var text = (string)obj["text"];
                _values.Add(language, text);
            }
        }

        public JArray ToJson()
        {
            return new JArray(
                _values.Select(v => new JObject(
                    new JProperty("language", (int)v.Key),
                    new JProperty("text", v.Value))));
        }

        public string AnyValue
        {
            get
            {
                return _values
                    .OrderBy(x => (int)x.Key)
                    .Select(x => x.Value)
                    .FirstOrDefault() ?? string.Empty;
            }
        }

        public string GetValueOrEmpty(Language language)
        {
            if (_values.ContainsKey(language))
            {
                return _values[language] ?? string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        public static MultiLanguageString operator +(MultiLanguageString a, string b)
        {
            return a.Concat(b);
        }

        public static MultiLanguageString operator +(MultiLanguageString a, MultiLanguageString b)
        {
            return a.Concat(b);
        }

        public MultiLanguageString Concat(string suffix)
        {
            var result = new MultiLanguageString();

            foreach (var v in _values)
            {
                result._values.Add(v.Key, v.Value + suffix);
            }

            return result;
        }

        public MultiLanguageString Concat(MultiLanguageString suffix)
        {
            var result = new MultiLanguageString();
            var keys = _values.Keys.Concat(suffix._values.Keys).Distinct().ToList();

            foreach (var k in keys)
            {
                result[k] = this[k] + suffix[k];
            }

            return result;
        }

        public string this[Language language]
        {
            get
            {
                if (_values.ContainsKey(language))
                {
                    return _values[language];
                }
                else
                {
                    return AnyValue;
                }
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (_values.ContainsKey(language))
                    {
                        _values[language] = value.RemoveHtml().RemoveParameters();
                    }
                    else
                    {
                        _values.Add(language, value.RemoveHtml().RemoveParameters());
                    }
                }
                else if (_values.ContainsKey(language))
                {
                    _values.Remove(language);
                }
            }
        }
    }
}
