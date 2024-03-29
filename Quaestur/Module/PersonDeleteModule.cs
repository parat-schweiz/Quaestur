﻿using System;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDeleteModule : QuaesturModule
    {
        public PersonDeleteModule()
        {
            RequireCompleteLogin();

            Get("/person/delete/mark/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Contact, AccessRight.Write) &&
                        (person != CurrentSession.User))
                    {
                        person.Deleted.Value = true;
                        Database.Save(person);
                        Journal(person,
                            "Person.Delete.Mark",
                            "Mark a person as delete",
                            "Marked as delete");
                    }
                }

                return string.Empty;
            });
            Get("/person/delete/unmark/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Deleted, AccessRight.Write) &&
                        (person != CurrentSession.User))
                    {
                        person.Deleted.Value = false;
                        Database.Save(person);
                        Journal(person,
                            "Person.Delete.Unmark",
                            "Unmark as deleted",
                            "Undeleted");
                    }
                }

                return string.Empty;
            });
            Get("/person/delete/hard/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Deleted, AccessRight.Write) &&
                        (person != CurrentSession.User))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            foreach (var mailing in Database.Query<Mailing>(DC.Equal("creatorid", person.Id.Value)))
                            {
                                mailing.Creator.Value = CurrentSession.User;
                            }

                            foreach (var document in Database.Query<Document>(DC.Equal("verifierid", person.Id.Value)))
                            {
                                document.Verifier.Value = CurrentSession.User;
                            }

                            person.Delete(Database);
                            transaction.Commit();
                            Global.Log.Notice(
                                "User {0} deleted {1} from the database", 
                                CurrentSession.User.ShortHand, 
                                person.ShortHand);
                        }
                    }
                }

                return string.Empty;
            });
        }
    }
}
