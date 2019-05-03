using System;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDeleteModule : PublicusModule
    {
        public ContactDeleteModule()
        {
            this.RequiresAuthentication();

            Get["/contact/delete/mark/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Write) &&
                        (contact != CurrentSession.User))
                    {
                        contact.Deleted.Value = true;
                        Database.Save(contact);
                        Journal(contact,
                            "Contact.Delete.Mark",
                            "Mark a contact as delete",
                            "Marked as delete");
                    }
                }

                return null;
            };
            Get["/contact/delete/unmark/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Deleted, AccessRight.Write) &&
                        (contact != CurrentSession.User))
                    {
                        contact.Deleted.Value = false;
                        Database.Save(contact);
                        Journal(contact,
                            "Contact.Delete.Unmark",
                            "Unmark as deleted",
                            "Undeleted");
                    }
                }

                return null;
            };
            Get["/contact/delete/hard/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Deleted, AccessRight.Write) &&
                        (contact != CurrentSession.User))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            contact.Delete(Database);
                            transaction.Commit();
                            Global.Log.Notice(
                                "User {0} deleted {1} from the database", 
                                CurrentSession.User.UserName.Value, 
                                contact.ShortHand);
                        }
                    }
                }

                return null;
            };
        }
    }
}
