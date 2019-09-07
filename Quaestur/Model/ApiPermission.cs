using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class ApiPermission : DatabaseObject
    {
		public ForeignKeyField<ApiClient, ApiPermission> ApiClient { get; set; }
        public EnumField<PartAccess> Part { get; set; }
        public EnumField<SubjectAccess> Subject { get; set; }
        public EnumField<AccessRight> Right { get; set; }

        public ApiPermission() : this(Guid.Empty)
        {
        }

        public ApiPermission(Guid id) : base(id)
        {
            ApiClient = new ForeignKeyField<ApiClient, ApiPermission>(this, "apiclientid", false, r => r.Permissions);
            Part = new EnumField<PartAccess>(this, "part", PartAccess.None, PartAccessExtensions.Translate);
            Subject = new EnumField<SubjectAccess>(this, "subject", SubjectAccess.None, SubjectAccessExtensions.Translate);
            Right = new EnumField<AccessRight>(this, "accessright", AccessRight.None, AccessRightExtensions.Translate);
        }

        public override string ToString()
        {
            return string.Format("API Permission {0} {1} {2}", Part.Value, Subject.Value, Right.Value);
        }

        public override string GetText(Translator translator)
        {
            return translator.Get(
                "ApiPermission.Text",
                "Textual representation of API permission",
                "{0} access to {1} of {2}",
                Right.GetText(translator),
                Part.GetText(translator), 
                Subject.GetText(translator));
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}
