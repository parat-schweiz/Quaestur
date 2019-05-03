using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nancy;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Publicus
{
    public class PostStatus
    {
        private readonly IDatabase _db;
        private readonly List<JProperty> _messages;
        private readonly Translator _translator;
        private readonly Session _session;

        public bool IsSuccess { get; private set; }
        public string ErrorText { get; private set; }
        public string SuccessText { get; private set; }

        public bool ObjectNotNull(DatabaseObject obj)
        {
            if (ReferenceEquals(obj, null))
            {
                SetErrorNotFound();
                return false;
            }
            else
            {
                return true;
            }
        }

        public void SetErrorNotFound()
        {
            SetError("Error.Object.NotFound", "Error message when object not found", "Object not found.");
        }

        public void SetError(string key, string hint, string text, params object[] arguments)
        {
            ErrorText = _translator.Get(key, hint, text, arguments).EscapeHtml();
            IsSuccess = false;
        }

        public void SetSuccess(string key, string hint, string text, params object[] arguments)
        {
            SuccessText = _translator.Get(key, hint, text, arguments).EscapeHtml();
            IsSuccess = true;
        }

        public PostStatus(IDatabase db, Translator translator, Session session)
        {
            _db = db;
            _translator = translator;
            _session = session;
            _messages = new List<JProperty>();
            IsSuccess = true;
        }

        private bool VerifyAccess(Func<Session, bool> verify)
        {
            if (_session == null)
            {
                SetError("Error.Session.Required", "Error message when session required but not present", "You must log in to perform this action.");
                return false;
            }
            else if (verify(_session))
            {
                return true;
            }
            else
            {
                SetErrorAccessDenied();
                return false;
            }
        }

        public void SetErrorAccessDenied()
        {
            SetError("Error.Access.Denied", "Error message when access denied", "You do not have permission for this action.");
        }

        public bool HasAnyFeedAccess(PartAccess partAccess, AccessRight right)
        {
            return VerifyAccess(s => s.HasAnyFeedAccess(partAccess, right));
        }

        public bool HasSystemWideAccess(PartAccess partAccess, AccessRight right)
        {
            return VerifyAccess(s => s.HasSystemWideAccess(partAccess, right));
        }

        public bool HasAccess(Contact contact, PartAccess partAccess, AccessRight right)
        {
            return VerifyAccess(s => s.HasAccess(contact, partAccess, right));
        }

        public bool HasAccess(Feed feed, PartAccess partAccess, AccessRight right)
        {
            return VerifyAccess(s => s.HasAccess(feed, partAccess, right));
        }

        public bool HasAccess(Group group, PartAccess partAccess, AccessRight right)
        {
            return VerifyAccess(s => s.HasAccess(group, partAccess, right));
        }

        public string CreateJsonData()
        {
            var statusObject = new JObject(
                new JProperty("IsSuccess", IsSuccess));

            if (!string.IsNullOrEmpty(ErrorText))
            {
                statusObject.Add(new JProperty("MessageType", "warning"));
                statusObject.Add(new JProperty("MessageText", ErrorText.EscapeHtml()));
            }

            if (!string.IsNullOrEmpty(SuccessText))
            {
                statusObject.Add(new JProperty("MessageType", "success"));
                statusObject.Add(new JProperty("MessageText", SuccessText.EscapeHtml()));
            }

            foreach (var m in _messages)
            {
                statusObject.Add(m);
            }
            return statusObject.ToString();
        }

        public void SetValidationError(string fieldName, string key, string hint, string text)
        {
            Add(fieldName, key, hint, text);
            IsSuccess = false;
        }

        private void Add(string fieldName, string key, string hint, string text)
        {
            var message = _translator.Get(key, hint, text);
            _messages.Add(new JProperty(fieldName + "Validation", message));
        }

        public void AssingDataUrlString(string fieldName, FieldClass<byte[]> dataField, StringField contentTypeField, string stringValue, bool required)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                var parts = stringValue.Split(new string[] { "data:", ";base64," }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    try
                    {
                        if (!ReferenceEquals(contentTypeField, null))
                        {
                            contentTypeField.Value = parts[0];
                        }

                        dataField.Value = Convert.FromBase64String(parts[1]);
                    }
                    catch
                    {
                        Add(fieldName, 
                            "Validation.Upload.Failed", 
                            "Validation message on upload failed", 
                            "File upload failed");
                        IsSuccess = false;
                    }
                }
                else
                {
                    Add(fieldName,
                        "Validation.Upload.Failed",
                        "Validation message on upload failed",
                        "File upload failed");
                    IsSuccess = false;
                }
            }
            else if (required)
            {
                Add(fieldName,
                    "Validation.Upload.Required",
                    "Validation message on upload required",
                    "File upload required");
                IsSuccess = false;
            }
        }

        public void AssignEnumIntString<T>(string fieldName, EnumField<T> field, string stringValue) where T : struct, IConvertible
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (int.TryParse(stringValue, out int intValue))
                {
                    T value = (T)(object)intValue;
                    if (IsValue<T>(value))
                    {
                        field.Value = value;
                    }
                    else
                    {
                        IsSuccess = false;
                        Add(fieldName,
                            "Validation.Enum.Invalid",
                            "Validation message on enum invalid",
                            "Invalid value");
                    }
                }
                else
                {
                    IsSuccess = false;
                    Add(fieldName,
                        "Validation.Enum.Invalid",
                        "Validation message on enum invalid",
                        "Invalid value");
                }
            }
            else
            {
                Add(fieldName,
                    "Validation.Enum.Required",
                    "Validation message on enum required",
                    "Choice required");
                IsSuccess = false;
            }
        }

        public void AssignStringList(string fieldName, StringListField field, string[] stringValues)
        {
            field.Value = stringValues;
        }

        public void AssignEnumIntString<T>(string fieldName, EnumNullField<T> field, string stringValue) where T : struct, IConvertible
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (int.TryParse(stringValue, out int intValue))
                {
                    T value = (T)(object)intValue;
                    if (IsValue<T>(value))
                    {
                        field.Value = value;
                    }
                    else
                    {
                        IsSuccess = false;
                        Add(fieldName,
                            "Validation.Enum.Invalid",
                            "Validation message on enum invalid",
                            "Invalid value");
                    }
                }
                else
                {
                    IsSuccess = false;
                    Add(fieldName,
                        "Validation.Enum.Invalid",
                        "Validation message on enum invalid",
                        "Invalid value");
                }
            }
        }

        public void AssignFlagIntString<T>(string fieldName, EnumField<T> field, T flag, string stringValue) where T : struct, IConvertible
        {
            switch (stringValue)
            {
                case "0":
                    field.Value = (T)(object)((int)(object)field.Value & ~(int)(object)flag);
                    break;
                case "1":
                    field.Value = (T)(object)((int)(object)field.Value | (int)(object)flag);
                    break;
                default:
                    IsSuccess = false;
                    Add(fieldName,
                        "Validation.Flag.Invalid",
                        "Validation message on enum invalid",
                        "Invalid value");
                    break;
            }
        }

        public void AssignFlagIntsString<T>(string fieldName, EnumField<T> field, string[] stringValues) where T : struct, IConvertible
        {
            T newValue = (T)(object)(0);

            foreach (var stringValue in stringValues)
            {
                if (int.TryParse(stringValue, out int intValue))
                {
                    newValue = (T)(object)((int)(object)newValue | (int)(object)intValue);
                }
                else
                {
                    IsSuccess = false;
                    Add(fieldName,
                        "Validation.Flag.Invalid",
                        "Validation message on enum invalid",
                        "Invalid value");
                }
            }

            if (IsSuccess)
            {
                field.Value = newValue;
            }
        }

        public void AssignDateString(string fieldName, Field<DateTime> field, string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (DateTime.TryParseExact(stringValue,
                    new string[] { "yyyy-MM-dd", "dd.MM.yyyy", "MM/dd/yyyy" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTime value))
                {
                    field.Value = value;
                }
                else
                {
                    Add(fieldName,
                        "Validation.Date.Invalid",
                        "Validation message on date invalid",
                        "Date invalid");
                    IsSuccess = false;
                }
            }
            else
            {
                Add(fieldName,
                    "Validation.Date.Required",
                    "Validation message on date required",
                    "Date required");
                IsSuccess = false;
            }
        }

        public void AddAssignTimeString(string fieldName, FieldNull<DateTime> field, string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (field.Value.HasValue &&
                    TimeSpan.TryParseExact(stringValue,
                    new string[] {
                        @"hh\:mm",
                        @"h\:mm",
                    },
                    CultureInfo.InvariantCulture,
                    out TimeSpan value))
                {
                    field.Value = field.Value.Value.Add(value);
                }
                else
                {
                    Add(fieldName,
                        "Validation.Time.Invalid",
                        "Validation message on time invalid",
                        "Time invalid");
                    IsSuccess = false;
                }
            }
            else
            {
                Add(fieldName,
                    "Validation.Time.Required",
                    "Validation message on time required",
                    "Time required");
                IsSuccess = false;
            }
        }

        public void AssignDateString(string fieldName, FieldNull<DateTime> field, string stringValue, bool notNull = false)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (DateTime.TryParseExact(stringValue,
                    new string[] {
                    "yyyy-MM-dd",
                    "dd.MM.yyyy",
                    "MM/dd/yyyy",
                    },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime value))
                {
                    field.Value = value;
                }
                else
                {
                    Add(fieldName,
                        "Validation.Date.Invalid",
                        "Validation message on date invalid",
                        "Date invalid");
                    IsSuccess = false;
                }
            }
            else if (notNull)
            {
                Add(fieldName,
                    "Validation.Date.Required",
                    "Validation message on date required",
                    "Date required");
                IsSuccess = false;
            }
            else
            {
                field.Value = null;
            }
        }

        public void AssignStringIfNotEmpty(string fieldName, StringField field, string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                field.Value = stringValue;
            }
        }

        public void AssignStringFree(string fieldName, StringField field, string stringValue)
        {
            if (stringValue == null)
            {
                field.Value = string.Empty;
            }
            else
            {
                field.Value = stringValue;
            }
        }

        public void AssignStringRequired(string fieldName, StringField field, string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                field.Value = stringValue;
            }
            else
            {
                Add(fieldName,
                    "Validation.String.Required",
                    "Validation message on string required",
                    "Value required");
                IsSuccess = false;
            }
        }

        public void AssignMultiLanguageRequired(string fieldName, MultiLanguageStringField field, List<MultiItemViewModel> multiItems)
        {
            if (multiItems != null)
            {
                var newValue = new MultiLanguageString();

                foreach (var item in multiItems)
                {
                    if (int.TryParse(item.Key, out int intLanguage) &&
                        !string.IsNullOrEmpty(item.Value))
                    {
                        var language = (Language)intLanguage;

                        if (IsValue(language))
                        {
                            newValue[language] = item.Value;
                        }
                    }
                }

                if (string.IsNullOrEmpty(newValue.AnyValue))
                {
                    Add(fieldName,
                        "Validation.String.Required",
                        "Validation message on string required",
                        "Value required");
                    IsSuccess = false;
                }
                else
                {
                    field.Value = newValue; 
                }
            }
            else
            {
                Add(fieldName,
                    "Validation.String.Required",
                    "Validation message on string required",
                    "Value required");
                IsSuccess = false;
            }
        }

        public void AssignMultiLanguageFree(string fieldName, MultiLanguageStringField field, List<MultiItemViewModel> multiItems)
        {
            if (multiItems != null)
            {
                var newValue = new MultiLanguageString();

                foreach (var item in multiItems)
                {
                    if (int.TryParse(item.Key, out int intLanguage) &&
                        !string.IsNullOrEmpty(item.Value))
                    {
                        var language = (Language)intLanguage;

                        if (IsValue(language))
                        {
                            newValue[language] = item.Value;
                        }
                    }
                }

                field.Value = newValue;
            }
            else
            {
                field.Value = new MultiLanguageString();
            }
        }

        public void AssignDecimalString(string fieldName, DecimalField field, string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (decimal.TryParse(stringValue, out decimal value))
                {
                    field.Value = value;
                }
                else
                {
                    Add(fieldName,
                        "Validation.Decimal.Invalid",
                        "Validation message on decimal invalid",
                        "Value invalid");
                    IsSuccess = false;
                }
            }
            else
            {
                Add(fieldName,
                    "Validation.Decimal.Required",
                    "Validation message on decimal required",
                    "Value required");
                IsSuccess = false;
            }
        }

        public void AssignInt32String(string fieldName, Field<int> field, string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (int.TryParse(stringValue, out int value))
                {
                    field.Value = value;
                }
                else
                {
                    Add(fieldName,
                        "Validation.Int32.Invalid",
                        "Validation message on integer invalid",
                        "Value invalid");
                    IsSuccess = false;
                }
            }
            else
            {
                Add(fieldName,
                    "Validation.Int32.Required",
                    "Validation message on integer required",
                    "Value required");
                IsSuccess = false;
            }
        }

        public void AssignObjectIdString<T>(string fieldName, ProtoField<T> field, string stringIdValue) where T : DatabaseObject, new()
        {
            if (!string.IsNullOrEmpty(stringIdValue))
            {
                if (Guid.TryParse(stringIdValue, out Guid id))
                {
                    var o = _db.Query<T>(id);

                    if (o == null)
                    {
                        Add(fieldName,
                            "Validation.Object.Invalid",
                            "Validation message on object invalid",
                            "Selection invalid");
                        IsSuccess = false;
                    }
                    else
                    {
                        field.Value = o;
                    }
                }
                else
                {
                    Add(fieldName,
                        "Validation.Object.Invalid",
                        "Validation message on object invalid",
                        "Selection invalid");
                    IsSuccess = false;
                }
            }
            else
            {
                if (field.Nullable)
                {
                    field.Value = null;
                }
                else
                {
                    Add(fieldName,
                        "Validation.Object.Required",
                        "Validation message on object required",
                        "Selection required");
                    IsSuccess = false;
                }
            }
        }

        private bool IsValue<T>(T value) where T : struct, IConvertible
        {
            foreach (T x in (T[])Enum.GetValues(typeof(T)))
            {
                if (x.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
