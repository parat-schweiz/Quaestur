using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class NamedIdViewModel
    {
        public string Id;
        public string Name;
        public bool Disabled;
        public bool Selected;

        public string Options
        {
            get
            {
                var options = string.Empty;

                if (Disabled)
                {
                    options += " disabled"; 
                }

                if (Selected)
                {
                    options += " selected";
                }

                return options;
            }
        }

        public NamedIdViewModel(string name, bool disabled, bool selected)
        {
            Id = string.Empty;
            Name = name.EscapeHtml();
            Disabled = disabled;
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Country country, bool selected)
        {
            Id = country.Id.ToString();
            Name = country.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, State state, bool selected)
        {
            Id = state.Id.ToString();
            Name = state.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Feed feed, bool selected)
        {
            Id = feed.Id.ToString();
            Name = feed.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Tag tag, bool selected)
        {
            Id = tag.Id.ToString();
            Name = tag.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Session session, Contact contact, bool selected)
        {
            Id = contact.Id.ToString();
            Name = session.HasAccess(contact, PartAccess.Demography, AccessRight.Read) ?
                contact.ShortHand.EscapeHtml() : contact.Organization.Value.EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(MailingElement element, bool selected)
        {
            Id = element.Id.ToString();
            Name = element.Name.Value.EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Subscription subscription, bool selected)
        {
            Id = subscription.Id.ToString();
            Name = subscription.Feed.Value.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Group group, bool selected)
        {
            Id = group.Id.ToString();
            Name = group.Feed.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   group.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, MasterRole masterRole, bool selected)
        {
            Id = masterRole.Id.ToString();
            Name = masterRole.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }

        public NamedIdViewModel(Translator translator, Role role, bool selected)
        {
            Id = role.Id.ToString();
            Name = role.Group.Value.Feed.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   role.Group.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   role.Name.Value[translator.Language].EscapeHtml();
            Selected = selected;
        }
    }
}
