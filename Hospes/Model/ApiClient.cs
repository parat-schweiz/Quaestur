using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class ApiClient : DatabaseObject
    {
        public MultiLanguageStringField Name { get; private set; }
        public ForeignKeyField<Group, ApiClient> Group { get; set; }
        public ByteArrayField SecureSecret { get; set; }
        public List<ApiPermission> Permissions { get; set; }

        public ApiClient() : this(Guid.Empty)
        {
        }

		public ApiClient(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Group = new ForeignKeyField<Group, ApiClient>(this, "groupid", false, null);
            SecureSecret = new ByteArrayField(this, "securesecret", false);
            Permissions = new List<ApiPermission>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<ApiPermission>("apiclientid", Id.Value, () => Permissions); 
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var permission in database.Query<ApiPermission>(DC.Equal("apiclientid", Id.Value)))
            {
                permission.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Group.ToString() + " / " + Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Group.GetText(translator) + " / " + Name.Value[translator.Language];
        }
    }
}
