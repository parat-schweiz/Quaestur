using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public enum SendingTemplateParentType
    { 
        BallotTemplate = 0,
    }

    public static class SendingTemplateParentTypeExtensions
    {
        public static string Translate(this SendingTemplateParentType parentType, Translator translator)
        {
            switch (parentType)
            {
                case SendingTemplateParentType.BallotTemplate:
                    return translator.Get("Enum.SendingTemplateParentType.BallotTemplate", "Value 'Ballot template' in BallotStatus enum", "Ballot template");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class SendingTemplate : DatabaseObject
    {
        public EnumField<SendingTemplateParentType> ParentType { get; private set; }
        public Field<Guid> ParentId { get; private set; }
        public StringField FieldName { get; private set; }
        public List<SendingTemplateLanguage> Languages { get; private set; }

        public SendingTemplate() : this(Guid.Empty)
        {
        }

        public SendingTemplate(Guid id) : base(id)
        {
            ParentType = new EnumField<SendingTemplateParentType>(this, "parenttype", SendingTemplateParentType.BallotTemplate, SendingTemplateParentTypeExtensions.Translate);
            ParentId = new Field<Guid>(this, "parentid", Guid.Empty);
            FieldName = new StringField(this, "fieldname", 256, AllowStringType.SimpleText);
            Languages = new List<SendingTemplateLanguage>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get 
            {
                yield return new MultiCascade<SendingTemplateLanguage>("templateid", Id, () => Languages);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Delete(IDatabase database)
        {
            foreach (var sendingTemplateLanguage in database.Query<SendingTemplateLanguage>(DC.Equal("templateid", Id.Value)))
            {
                sendingTemplateLanguage.Delete(database); 
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return ToString();
        }

        public DatabaseObject Parent(IDatabase database)
        {
            switch (ParentType.Value)
            {
                case SendingTemplateParentType.BallotTemplate:
                    return database.Query<BallotTemplate>(ParentId.Value);
                default:
                    throw new NotSupportedException();
            }
        }

        public string TranslateFieldName(IDatabase database, Translator translator)
        {
            switch (ParentType.Value)
            {
                case SendingTemplateParentType.BallotTemplate:
                    return BallotTemplate.GetFieldNameTranslation(translator, FieldName.Value);
                default:
                    throw new NotSupportedException();
            }
        }

        public PartAccess ParentPartAccess
        {
            get
            {
                switch (ParentType.Value)
                {
                    case SendingTemplateParentType.BallotTemplate:
                        return PartAccess.Ballot;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public bool HasAccess(IDatabase database, Session session, AccessRight right)
        {
            switch (ParentType.Value)
            {
                case SendingTemplateParentType.BallotTemplate:
                    var organization = (Parent(database) as BallotTemplate).Organizer.Value.Organization.Value;
                    return session.HasAccess(organization, ParentPartAccess, right);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
