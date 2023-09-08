using System;
using SiteLibrary;

namespace Quaestur
{
    public class DeviceSession : DatabaseObject
    {
        public ForeignKeyField<Person, DeviceSession> User { get; private set; }
        public StringField Name { get; private set; }
        public Field<DateTime> Created { get; private set; }
        public Field<DateTime> LastAccess { get; private set; }
        public Field<bool> TwoFactorAuth { get; set; }

        public DeviceSession() : this(Guid.Empty)
        {
        }

        public DeviceSession(Guid id) : base(id)
        {
            User = new ForeignKeyField<Person, DeviceSession>(this, "userid", false, null);
            Name = new StringField(this, "name", 256, AllowStringType.SimpleText);
            Created = new Field<DateTime>(this, "created", new DateTime(1850, 1, 1));
            LastAccess = new Field<DateTime>(this, "lastaccess", new DateTime(1850, 1, 1));
            TwoFactorAuth = new Field<bool>(this, "twofactorauth", false);

            Created.Value = DateTime.UtcNow;
            LastAccess.Value = DateTime.UtcNow;
            TwoFactorAuth.Value = false;
        }

        public override string GetText(Translator translator)
        {
            throw new NotImplementedException();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public bool Expired
        {
            get
            {
                return 
                    (DateTime.UtcNow > Created.Value.AddSeconds(Global.Config.SessionExpiryAbsoluteSeconds) ||
                     DateTime.UtcNow > LastAccess.Value.AddSeconds(Global.Config.SessionExpiryRelativeSeconds));
            }
        }
    }
}
